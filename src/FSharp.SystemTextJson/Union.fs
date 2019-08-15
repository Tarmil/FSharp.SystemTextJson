namespace System.Text.Json.Serialization

open System
open System.Reflection
open System.Text.Json
open FSharp.Reflection
open System.Runtime.InteropServices

type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// "Case" contains the union tag, and "Fields" contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    | AdjacentTag       = 0x00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag       = 0x00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named "Case" if NamedFields is set) is the union tag, the rest are the union fields.
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


    //// Specific formats

    | Default           = 0x00_01
    | NewtonsoftLike    = 0x00_01
    | ThothLike         = 0x02_04


type private Case =
    {
        Info: UnionCaseInfo
        Fields: PropertyInfo[]
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
    }

type JsonUnionConverter<'T>(encoding: JsonUnionEncoding) =
    inherit JsonConverter<'T>()

    let [<Literal>] UntaggedBit = enum<JsonUnionEncoding> 0x00_08
    let baseFormat = encoding &&& enum<JsonUnionEncoding> 0x00_ff
    let namedFields = encoding.HasFlag JsonUnionEncoding.NamedFields

    static let ty = typeof<'T>

    static let cases =
        FSharpType.GetUnionCases(ty, true)
        |> Array.map (fun uci ->
            {
                Info = uci
                Fields = uci.GetFields()
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
            })

    static let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)

    static let usesNull =
        ty.GetCustomAttributes(typeof<CompilationRepresentationAttribute>, false)
        |> Array.exists (fun x ->
            let x = (x :?> CompilationRepresentationAttribute)
            x.Flags.HasFlag(CompilationRepresentationFlags.UseNullAsTrueValue))

    static let hasDistinctFieldNames, fieldlessCase, allFields =
        let mutable fieldlessCase = ValueNone
        let mutable hasDuplicateFieldNames = false
        let cases =
            cases
            |> Array.collect (fun case ->
                // TODO: BareFieldlessTags should allow multiple fieldless cases
                if Array.isEmpty case.Fields then
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

    static let getCaseByTag (reader: byref<Utf8JsonReader>) =
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

    static let getCaseByFieldName (reader: byref<Utf8JsonReader>) =
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

    static let fieldIndexByName (reader: byref<Utf8JsonReader>) (case: Case) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < cases.Length do
            let field = case.Fields.[i]
            if reader.ValueTextEquals(field.Name.AsSpan()) then
                found <- ValueSome (struct (i, field))
            else
                i <- i + 1
        found

    static let fail expected (reader: byref<Utf8JsonReader>) =
        sprintf "Failed to parse union type %s: expected %s, found %A"
            ty.FullName expected reader.TokenType
        |> JsonException
        |> raise

    static let expectAlreadyRead expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) =
        if reader.TokenType <> expectedTokenType then
            fail expectedLabel &reader

    static let readExpecting expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> expectedTokenType then
            fail expectedLabel &reader

    static let readExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> JsonTokenType.PropertyName || not (reader.ValueTextEquals expectedPropertyName) then
            fail ("\"" + expectedPropertyName + "\"") &reader

    static let readFieldsAsRestOfArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.zeroCreate fieldCount
        for i in 0..fieldCount-1 do
            reader.Read() |> ignore
            fields.[i] <- JsonSerializer.Deserialize(&reader, case.Fields.[i].PropertyType, options)
        readExpecting JsonTokenType.EndArray "end of array" &reader
        case.Ctor fields :?> 'T
    
    static let readFieldsAsArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartArray "array" &reader
        readFieldsAsRestOfArray &reader case options

    static let readFieldsAsRestOfObject (reader: byref<Utf8JsonReader>) (case: Case) (skipFirstRead: bool) (options: JsonSerializerOptions) =
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

    static let readFieldsAsObject (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartObject "object" &reader
        readFieldsAsRestOfObject &reader case false options

    let readFields (reader: byref<Utf8JsonReader>) case options =
        if namedFields then
            readFieldsAsObject &reader case options
        else
            readFieldsAsArray &reader case options

    let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader
        readExpectingPropertyNamed "Case" &reader
        readExpecting JsonTokenType.String "case name" &reader
        let case = getCaseByTag &reader
        let res =
            if case.Fields.Length > 0 then
                readExpectingPropertyNamed "Fields" &reader
                readFields &reader case options
            else
                case.Ctor [||] :?> 'T
        readExpecting JsonTokenType.EndObject "end of object" &reader
        res

    let readExternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader
        readExpecting JsonTokenType.PropertyName "case name" &reader
        let case = getCaseByTag &reader
        let res = readFields &reader case options
        readExpecting JsonTokenType.EndObject "end of object" &reader
        res

    let readInternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if namedFields then
            expectAlreadyRead JsonTokenType.StartObject "object" &reader
            readExpectingPropertyNamed "Case" &reader
            readExpecting JsonTokenType.String "case name" &reader
            let case = getCaseByTag &reader
            readFieldsAsRestOfObject &reader case false options
        else
            expectAlreadyRead JsonTokenType.StartArray "array" &reader
            readExpecting JsonTokenType.String "case name" &reader
            let case = getCaseByTag &reader
            readFieldsAsRestOfArray &reader case options

    static let readUntagged (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader
        reader.Read() |> ignore
        match reader.TokenType with
        | JsonTokenType.PropertyName ->
            let case = getCaseByFieldName &reader
            readFieldsAsRestOfObject &reader case true options
        | JsonTokenType.EndObject ->
            match fieldlessCase with
            | ValueSome case -> case.Ctor [||] :?> 'T
            | ValueNone -> fail "case field" &reader
        | _ ->
            fail "case field" &reader

    static let writeFieldsAsRestOfArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        for field in case.Dector value do
            JsonSerializer.Serialize(writer, field, options)
        writer.WriteEndArray()

    static let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        writeFieldsAsRestOfArray writer case value options

    static let writeFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        (case.Fields, case.Dector value)
        ||> Array.iter2 (fun field value ->
            writer.WritePropertyName(field.Name)
            JsonSerializer.Serialize(writer, value, options)
        )
        writer.WriteEndObject()

    static let writeFieldsAsObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writeFieldsAsRestOfObject writer case value options

    let writeFields writer case value options =
        if namedFields then
            writeFieldsAsObject writer case value options
        else
            writeFieldsAsArray writer case value options

    let writeAdjacentTag (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        writer.WriteStartObject()
        writer.WriteString("Case", case.Info.Name)
        if case.Fields.Length > 0 then
            writer.WritePropertyName("Fields")
            writeFields writer case value options
        writer.WriteEndObject()

    let writeExternalTag (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        writer.WriteStartObject()
        writer.WritePropertyName(case.Info.Name)
        writeFields writer case value options
        writer.WriteEndObject()

    let writeInternalTag (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        if namedFields then
            writer.WriteStartObject()
            writer.WriteString("Case", case.Info.Name)
            writeFieldsAsRestOfObject writer case value options
        else
            writer.WriteStartArray()
            writer.WriteStringValue(case.Info.Name)
            writeFieldsAsRestOfArray writer case value options

    let writeUntagged (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        writeFieldsAsObject writer case value options

    override __.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null when usesNull ->
            (null : obj) :?> 'T
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

    override __.Write(writer, value, options) =
        let value = box value
        if isNull value then writer.WriteNullValue() else

        match baseFormat with
        | JsonUnionEncoding.AdjacentTag -> writeAdjacentTag writer value options
        | JsonUnionEncoding.ExternalTag -> writeExternalTag writer value options
        | JsonUnionEncoding.InternalTag -> writeInternalTag writer value options
        | UntaggedBit -> writeUntagged writer value options
        | _ -> raise (JsonException("Invalid union encoding: " + string encoding))

type JsonUnionConverter
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        encoding: JsonUnionEncoding
    ) =
    inherit JsonConverterFactory()

    static let jsonUnionConverterTy = typedefof<JsonUnionConverter<_>>
    static let jsonUnionEncodingTy = typeof<JsonUnionEncoding>

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter(typeToConvert, encoding: JsonUnionEncoding) =
        jsonUnionConverterTy
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([|jsonUnionEncodingTy|])
            .Invoke([|encoding|])
        :?> JsonConverter

    override this.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override this.CreateConverter(typeToConvert) =
        JsonUnionConverter.CreateConverter(typeToConvert, encoding)
