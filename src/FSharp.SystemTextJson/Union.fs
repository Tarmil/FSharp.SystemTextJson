namespace System.Text.Json.Serialization

open System
open System.Reflection
open System.Text.Json
open FSharp.Reflection
open System.Runtime.InteropServices
open System.Text.Json.Serialization.Helpers

type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// `unionTagName` (defaults to "Case") contains the union tag, and "Fields" contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    | AdjacentTag       = 0x00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag       = 0x00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named `unionTagName`, defaulting to "Case", if NamedFields is set)
    /// is the union tag, the rest are the union fields.
    | InternalTag       = 0x00_04

    /// Encode unions as a n-valued object:
    /// the union tag is not encoded, only the union fields are.
    /// Deserialization is only possible if the fields of all cases have different names.
    | Untagged          = 0x01_08


    //// Additional options

    /// If unset, union fields are encoded as an array.
    /// If set, union fields are encoded as an object using their field names.
    | NamedFields       = 0x01_00

    /// If set, union cases that don't have fields are encoded as a bare string.
    | BareFieldlessTags = 0x02_00

    /// If set, `None` is represented as null,
    /// and `Some x`  is represented the same as `x`.
    | SuccintOption     = 0x04_00


    //// Specific formats

    | Default           = 0x04_01
    | NewtonsoftLike    = 0x00_01
    | ThothLike         = 0x02_04
    | FSharpLuLike      = 0x06_02


type JsonUnionTagName = string
type JsonUnionFieldsName = string

type private Case =
    {
        Info: UnionCaseInfo
        Fields: PropertyInfo[]
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
    }

type JsonUnionConverter<'T>(encoding: JsonUnionEncoding, unionTagName: JsonUnionTagName, unionFieldsName: JsonUnionFieldsName) =
    inherit JsonConverter<'T>()

    let [<Literal>] UntaggedBit = enum<JsonUnionEncoding> 0x00_08
    let baseFormat =
        let given = encoding &&& enum<JsonUnionEncoding> 0x00_ff
        if given = enum 0 then JsonUnionEncoding.AdjacentTag else given
    let namedFields = encoding.HasFlag JsonUnionEncoding.NamedFields
    let bareFieldlessTags = encoding.HasFlag JsonUnionEncoding.BareFieldlessTags

    let ty = typeof<'T>

    let cases =
        FSharpType.GetUnionCases(ty, true)
        |> Array.map (fun uci ->
            {
                Info = uci
                Fields = uci.GetFields()
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
            })

    let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)

    let usesNull =
        ty.GetCustomAttributes(typeof<CompilationRepresentationAttribute>, false)
        |> Array.exists (fun x ->
            let x = (x :?> CompilationRepresentationAttribute)
            x.Flags.HasFlag(CompilationRepresentationFlags.UseNullAsTrueValue))

    let hasDistinctFieldNames, fieldlessCase, allFields =
        let mutable fieldlessCase = ValueNone
        let mutable hasDuplicateFieldNames = false
        let cases =
            cases
            |> Array.collect (fun case ->
                // TODO: BareFieldlessTags should allow multiple fieldless cases
                if not bareFieldlessTags && Array.isEmpty case.Fields then
                    match fieldlessCase with
                    | ValueSome _ -> hasDuplicateFieldNames <- true
                    | ValueNone -> fieldlessCase <- ValueSome case
                case.Fields |> Array.map (fun f -> f.Name, case))
        let fields =
            (Map.empty, cases)
            ||> Array.fold (fun foundFieldNames (fieldName, case) ->
                if hasDuplicateFieldNames then
                    foundFieldNames
                else
                if foundFieldNames |> Map.containsKey fieldName then
                    hasDuplicateFieldNames <- true
                Map.add fieldName case foundFieldNames)
        let fields = [| for KeyValue(k, v) in fields -> struct (k, v) |]
        not hasDuplicateFieldNames, fieldlessCase, fields

    let getCaseByTag (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < cases.Length do
            let case = cases.[i]
            if reader.ValueTextEquals(case.Info.Name.AsSpan()) then
                found <- ValueSome case
            else
                i <- i + 1
        match found with
        | ValueNone ->
            raise (JsonException("Unknow case for union type " + ty.FullName + ": " + reader.GetString()))
        | ValueSome case ->
            case

    let getCaseByFieldName (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < allFields.Length do
            let struct (fieldName, case) = allFields.[i]
            if reader.ValueTextEquals(fieldName) then
                found <- ValueSome case
            else
                i <- i + 1
        match found with
        | ValueNone ->
            raise (JsonException("Unknow case for union type " + ty.FullName + " due to unknown field: " + reader.GetString()))
        | ValueSome case ->
            case

    let fieldIndexByName (reader: byref<Utf8JsonReader>) (case: Case) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < cases.Length do
            let field = case.Fields.[i]
            if reader.ValueTextEquals(field.Name.AsSpan()) then
                found <- ValueSome (struct (i, field))
            else
                i <- i + 1
        found

    let readFieldsAsRestOfArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.zeroCreate fieldCount
        for i in 0..fieldCount-1 do
            reader.Read() |> ignore
            fields.[i] <- JsonSerializer.Deserialize(&reader, case.Fields.[i].PropertyType, options)
        readExpecting JsonTokenType.EndArray "end of array" &reader ty
        case.Ctor fields :?> 'T

    let readFieldsAsArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartArray "array" &reader ty
        readFieldsAsRestOfArray &reader case options

    let readFieldsAsRestOfObject (reader: byref<Utf8JsonReader>) (case: Case) (skipFirstRead: bool) (options: JsonSerializerOptions) =
        let expectedFieldCount = case.Fields.Length
        let fields = Array.zeroCreate expectedFieldCount
        let mutable cont = true
        let mutable fieldsFound = 0
        let mutable skipFirstRead = skipFirstRead
        while cont && (skipFirstRead || reader.Read()) do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                skipFirstRead <- false
                match fieldIndexByName &reader case with
                | ValueSome (i, f) ->
                    fieldsFound <- fieldsFound + 1
                    fields.[i] <- JsonSerializer.Deserialize(&reader, f.PropertyType, options)
                | _ ->
                    reader.Skip()
            | _ -> ()

        if fieldsFound < expectedFieldCount then
            raise (JsonException("Missing field for record type " + ty.FullName))
        case.Ctor fields :?> 'T

    let readFieldsAsObject (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartObject "object" &reader ty
        readFieldsAsRestOfObject &reader case false options

    let readFields (reader: byref<Utf8JsonReader>) case options =
        if namedFields then
            readFieldsAsObject &reader case options
        else
            readFieldsAsArray &reader case options

    let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        readExpectingPropertyNamed unionTagName &reader ty
        readExpecting JsonTokenType.String "case name" &reader ty
        let case = getCaseByTag &reader
        let res =
            if case.Fields.Length > 0 then
                readExpectingPropertyNamed unionFieldsName &reader ty
                readFields &reader case options
            else
                case.Ctor [||] :?> 'T
        readExpecting JsonTokenType.EndObject "end of object" &reader ty
        res

    let readExternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        readExpecting JsonTokenType.PropertyName "case name" &reader ty
        let case = getCaseByTag &reader
        let res = readFields &reader case options
        readExpecting JsonTokenType.EndObject "end of object" &reader ty
        res

    let readInternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if namedFields then
            expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
            readExpectingPropertyNamed unionTagName &reader ty
            readExpecting JsonTokenType.String "case name" &reader ty
            let case = getCaseByTag &reader
            readFieldsAsRestOfObject &reader case false options
        else
            expectAlreadyRead JsonTokenType.StartArray "array" &reader ty
            readExpecting JsonTokenType.String "case name" &reader ty
            let case = getCaseByTag &reader
            readFieldsAsRestOfArray &reader case options

    let readUntagged (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        reader.Read() |> ignore
        match reader.TokenType with
        | JsonTokenType.PropertyName ->
            let case = getCaseByFieldName &reader
            readFieldsAsRestOfObject &reader case true options
        | JsonTokenType.EndObject ->
            match fieldlessCase with
            | ValueSome case -> case.Ctor [||] :?> 'T
            | ValueNone -> fail "case field" &reader ty
        | _ ->
            fail "case field" &reader ty

    let writeFieldsAsRestOfArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        for field in case.Dector value do
            JsonSerializer.Serialize(writer, field, options)
        writer.WriteEndArray()

    let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        writeFieldsAsRestOfArray writer case value options

    let writeFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        (case.Fields, case.Dector value)
        ||> Array.iter2 (fun field value ->
            writer.WritePropertyName(field.Name)
            JsonSerializer.Serialize(writer, value, options)
        )
        writer.WriteEndObject()

    let writeFieldsAsObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writeFieldsAsRestOfObject writer case value options

    let writeFields writer case value options =
        if namedFields then
            writeFieldsAsObject writer case value options
        else
            writeFieldsAsArray writer case value options

    let writeAdjacentTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WriteString(unionTagName, case.Info.Name)
        if case.Fields.Length > 0 then
            writer.WritePropertyName(unionFieldsName)
            writeFields writer case value options
        writer.WriteEndObject()

    let writeExternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WritePropertyName(case.Info.Name)
        writeFields writer case value options
        writer.WriteEndObject()

    let writeInternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        if namedFields then
            writer.WriteStartObject()
            writer.WriteString(unionTagName, case.Info.Name)
            writeFieldsAsRestOfObject writer case value options
        else
            writer.WriteStartArray()
            writer.WriteStringValue(case.Info.Name)
            writeFieldsAsRestOfArray writer case value options

    let writeUntagged (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writeFieldsAsObject writer case value options

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null when usesNull ->
            (null : obj) :?> 'T
        | JsonTokenType.String when bareFieldlessTags ->
            let case = getCaseByTag &reader
            reader.Read() |> ignore
            case.Ctor [||] :?> 'T
        | _ ->
            match baseFormat with
            | JsonUnionEncoding.AdjacentTag -> readAdjacentTag &reader options
            | JsonUnionEncoding.ExternalTag -> readExternalTag &reader options
            | JsonUnionEncoding.InternalTag -> readInternalTag &reader options
            | UntaggedBit ->
                if not hasDistinctFieldNames then
                    raise (JsonException(sprintf "Union %s can't be deserialized as Untagged because it has duplicate field names across unions" ty.FullName))
                readUntagged &reader options
            | _ -> raise (JsonException("Invalid union encoding: " + string encoding))

    override _.Write(writer, value, options) =
        let value = box value
        if isNull value then writer.WriteNullValue() else

        let tag = tagReader value
        let case = cases.[tag]
        if bareFieldlessTags && case.Fields.Length = 0 then
            writer.WriteStringValue(case.Info.Name)
        else
        match baseFormat with
        | JsonUnionEncoding.AdjacentTag -> writeAdjacentTag writer case value options
        | JsonUnionEncoding.ExternalTag -> writeExternalTag writer case value options
        | JsonUnionEncoding.InternalTag -> writeInternalTag writer case value options
        | UntaggedBit -> writeUntagged writer case value options
        | _ -> raise (JsonException("Invalid union encoding: " + string encoding))

type JsonSuccintOptionConverter<'T>() =
    inherit JsonConverter<option<'T>>()

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null -> None
        | _ -> Some <| JsonSerializer.Deserialize<'T>(&reader, options)

    override _.Write(writer, value, options) =
        match value with
        | None -> writer.WriteNullValue()
        | Some x -> JsonSerializer.Serialize<'T>(writer, x, options)

type JsonUnionConverter
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        encoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue("Case")>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue("Fields")>]
        unionFieldsName: JsonUnionFieldsName
    ) =
    inherit JsonConverterFactory()

    static let jsonUnionConverterTy = typedefof<JsonUnionConverter<_>>
    static let jsonUnionEncodingTy = typeof<JsonUnionEncoding>
    static let unionTagNameTy = typeof<JsonUnionTagName>
    static let unionFieldsNameTy = typeof<JsonUnionFieldsName>
    static let optionTy = typedefof<option<_>>
    static let jsonSuccintOptionConverterTy = typedefof<JsonSuccintOptionConverter<_>>

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter(typeToConvert: Type, encoding: JsonUnionEncoding, unionTagName: JsonUnionTagName, unionFieldsName: JsonUnionFieldsName) =
        if encoding.HasFlag JsonUnionEncoding.SuccintOption
            && typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() = optionTy then
            jsonSuccintOptionConverterTy
                .MakeGenericType(typeToConvert.GetGenericArguments())
                .GetConstructor([||])
                .Invoke([||])
            :?> JsonConverter
        else
            jsonUnionConverterTy
                .MakeGenericType([|typeToConvert|])
                .GetConstructor([|jsonUnionEncodingTy; unionTagNameTy; unionFieldsNameTy|])
                .Invoke([|encoding; unionTagName; unionFieldsName|])
            :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonUnionConverter.CreateConverter(typeToConvert, encoding, unionTagName, unionFieldsName)
