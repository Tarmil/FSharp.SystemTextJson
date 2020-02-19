namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Text.Json
open FSharp.Reflection
open System.Text.Json.Serialization.Helpers

type private Field =
    {
        Type: Type
        Name: string
        MustBeNonNull: bool
    }

type private Case =
    {
        Fields: Field[]
        FieldsByName: Dictionary<string, struct (int * Field)> voption
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
        Name: string
        UnwrappedSingleField: bool
    }

type JsonUnionConverter<'T>(options: JsonSerializerOptions, fsOptions: JsonFSharpOptions, cases: UnionCaseInfo[]) =
    inherit JsonConverter<'T>()

    let [<Literal>] UntaggedBit = enum<JsonUnionEncoding> 0x00_08
    let baseFormat =
        let given = fsOptions.UnionEncoding &&& enum<JsonUnionEncoding> 0x00_ff
        if given = enum 0 then JsonUnionEncoding.AdjacentTag else given
    let namedFields = fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.NamedFields
    let unwrapFieldlessTags = fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapFieldlessTags

    let ty = typeof<'T>

    let cases =
        cases
        |> Array.map (fun uci ->
            let name =
                match uci.GetCustomAttributes(typeof<JsonPropertyNameAttribute>) with
                | [| :? JsonPropertyNameAttribute as name |] -> name.Name
                | _ ->
                    match fsOptions.UnionTagNamingPolicy with
                    | null -> uci.Name
                    | policy -> policy.ConvertName uci.Name
            let fields =
                uci.GetFields()
                |> Array.map (fun p ->
                    {
                        Type = p.PropertyType
                        Name =
                            match options.PropertyNamingPolicy with
                            | null -> p.Name
                            | policy -> policy.ConvertName p.Name
                        MustBeNonNull = not (isNullableFieldType fsOptions p.PropertyType)
                    })
            let fieldsByName =
                if options.PropertyNameCaseInsensitive then
                    let d = Dictionary(StringComparer.OrdinalIgnoreCase)
                    fields |> Array.iteri (fun i f ->
                        d.[f.Name] <- struct (i, f))
                    ValueSome d
                else
                    ValueNone
            {
                Fields = fields
                FieldsByName = fieldsByName
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
                Name = name
                UnwrappedSingleField = fields.Length = 1 && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleFieldCases
            })

    let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)

    let hasDistinctFieldNames, fieldlessCase, allFields =
        let mutable fieldlessCase = ValueNone
        let mutable hasDuplicateFieldNames = false
        let cases =
            cases
            |> Array.collect (fun case ->
                // TODO: UnwrapFieldlessTags should allow multiple fieldless cases
                if not unwrapFieldlessTags && Array.isEmpty case.Fields then
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

    let allFieldsByName =
        if hasDistinctFieldNames && options.PropertyNameCaseInsensitive then
            let dict = Dictionary(StringComparer.OrdinalIgnoreCase)
            for _, c in allFields do
                match c.FieldsByName with
                | ValueNone -> ()
                | ValueSome fields ->
                    for KeyValue(n, _) in fields do
                        dict.[n] <- c
            ValueSome dict
        else
            ValueNone

    let casesByName =
        if fsOptions.UnionTagCaseInsensitive then
            let dict = Dictionary(StringComparer.OrdinalIgnoreCase)
            for c in cases do
                dict.[c.Name] <- c
            ValueSome dict
        else
            ValueNone

    let getCaseByTag (reader: byref<Utf8JsonReader>) =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases.[i]
                    if reader.ValueTextEquals(case.Name) then
                        found <- ValueSome case
                    else
                        i <- i + 1
                found
            | ValueSome d ->
                match d.TryGetValue(reader.GetString()) with
                | true, c -> ValueSome c
                | false, _ -> ValueNone
        match found with
        | ValueNone ->
            raise (JsonException("Unknow case for union type " + ty.FullName + ": " + reader.GetString()))
        | ValueSome case ->
            case

    let getCaseByFieldName (reader: byref<Utf8JsonReader>) =
        let found =
            match allFieldsByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < allFields.Length do
                    let struct (fieldName, case) = allFields.[i]
                    if reader.ValueTextEquals(fieldName) then
                        found <- ValueSome case
                    else
                        i <- i + 1
                found
            | ValueSome d ->
                match d.TryGetValue(reader.GetString()) with
                | true, p -> ValueSome p
                | false, _ -> ValueNone
        match found with
        | ValueNone ->
            raise (JsonException("Unknow case for union type " + ty.FullName + " due to unknown field: " + reader.GetString()))
        | ValueSome case ->
            case

    let fieldIndexByName (reader: byref<Utf8JsonReader>) (case: Case) =
        match case.FieldsByName with
        | ValueNone ->
            let mutable found = ValueNone
            let mutable i = 0
            while found.IsNone && i < case.Fields.Length do
                let field = case.Fields.[i]
                if reader.ValueTextEquals(field.Name) then
                    found <- ValueSome (struct (i, field))
                else
                    i <- i + 1
            found
        | ValueSome d ->
            match d.TryGetValue(reader.GetString()) with
            | true, p -> ValueSome p
            | false, _ -> ValueNone

    let readField (reader: byref<Utf8JsonReader>) (case: Case) (f: Field) options =
        let v = JsonSerializer.Deserialize(&reader, f.Type, options)
        if isNull v && f.MustBeNonNull then
            let msg = sprintf "%s.%s(%s) was expected to be of type %s, but was null." ty.Name case.Name f.Name f.Type.Name
            raise (JsonException msg)
        v

    let readFieldsAsRestOfArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.zeroCreate fieldCount
        for i in 0..fieldCount-1 do
            reader.Read() |> ignore
            fields.[i] <- readField &reader case case.Fields.[i] options
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
                    fields.[i] <- readField &reader case f options
                | _ ->
                    reader.Skip()
            | _ -> ()

        if fieldsFound < expectedFieldCount && not options.IgnoreNullValues then
            raise (JsonException("Missing field for union type " + ty.FullName))
        case.Ctor fields :?> 'T

    let readFieldsAsObject (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartObject "object" &reader ty
        readFieldsAsRestOfObject &reader case false options

    let readFields (reader: byref<Utf8JsonReader>) case options =
        if case.UnwrappedSingleField then
            let field = readField &reader case case.Fields.[0] options
            case.Ctor [| field |] :?> 'T
        elif namedFields then
            readFieldsAsObject &reader case options
        else
            readFieldsAsArray &reader case options

    let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        readExpectingPropertyNamed fsOptions.UnionTagName &reader ty
        readExpecting JsonTokenType.String "case name" &reader ty
        let case = getCaseByTag &reader
        let res =
            if case.Fields.Length > 0 then
                readExpectingPropertyNamed fsOptions.UnionFieldsName &reader ty
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
            readExpectingPropertyNamed fsOptions.UnionTagName &reader ty
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
        let fields = case.Fields
        let values = case.Dector value
        for i in 0..fields.Length-1 do
            JsonSerializer.Serialize(writer, values.[i], fields.[i].Type, options)
        writer.WriteEndArray()

    let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        writeFieldsAsRestOfArray writer case value options

    let writeFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        let fields = case.Fields
        let values = case.Dector value
        for i in 0..fields.Length-1 do
            if not (options.IgnoreNullValues && isNull values.[i]) then
                writer.WritePropertyName(fields.[i].Name)
                JsonSerializer.Serialize(writer, values.[i], fields.[i].Type, options)
        writer.WriteEndObject()

    let writeFieldsAsObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writeFieldsAsRestOfObject writer case value options

    let writeFields writer case value options =
        if case.UnwrappedSingleField then
            JsonSerializer.Serialize(writer, (case.Dector value).[0], case.Fields.[0].Type, options)
        elif namedFields then
            writeFieldsAsObject writer case value options
        else
            writeFieldsAsArray writer case value options

    let writeAdjacentTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WriteString(fsOptions.UnionTagName, case.Name)
        if case.Fields.Length > 0 then
            writer.WritePropertyName(fsOptions.UnionFieldsName)
            writeFields writer case value options
        writer.WriteEndObject()

    let writeExternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WritePropertyName(case.Name)
        writeFields writer case value options
        writer.WriteEndObject()

    let writeInternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        if namedFields then
            writer.WriteStartObject()
            writer.WriteString(fsOptions.UnionTagName, case.Name)
            writeFieldsAsRestOfObject writer case value options
        else
            writer.WriteStartArray()
            writer.WriteStringValue(case.Name)
            writeFieldsAsRestOfArray writer case value options

    let writeUntagged (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writeFieldsAsObject writer case value options

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null when Helpers.isNullableUnion ty ->
            (null : obj) :?> 'T
        | JsonTokenType.String when unwrapFieldlessTags ->
            let case = getCaseByTag &reader
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
            | _ -> raise (JsonException("Invalid union encoding: " + string fsOptions.UnionEncoding))

    override _.Write(writer, value, options) =
        let value = box value
        if isNull value then writer.WriteNullValue() else

        let tag = tagReader value
        let case = cases.[tag]
        if unwrapFieldlessTags && case.Fields.Length = 0 then
            writer.WriteStringValue(case.Name)
        else
        match baseFormat with
        | JsonUnionEncoding.AdjacentTag -> writeAdjacentTag writer case value options
        | JsonUnionEncoding.ExternalTag -> writeExternalTag writer case value options
        | JsonUnionEncoding.InternalTag -> writeInternalTag writer case value options
        | UntaggedBit -> writeUntagged writer case value options
        | _ -> raise (JsonException("Invalid union encoding: " + string fsOptions.UnionEncoding))

type JsonUnwrapOptionConverter<'T>() =
    inherit JsonConverter<option<'T>>()

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null -> None
        | _ -> Some <| JsonSerializer.Deserialize<'T>(&reader, options)

    override _.Write(writer, value, options) =
        match value with
        | None -> writer.WriteNullValue()
        | Some x -> JsonSerializer.Serialize<'T>(writer, x, options)

type JsonUnwrapValueOptionConverter<'T>() =
    inherit JsonConverter<voption<'T>>()

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null -> ValueNone
        | _ -> ValueSome <| JsonSerializer.Deserialize<'T>(&reader, options)

    override _.Write(writer, value, options) =
        match value with
        | ValueNone -> writer.WriteNullValue()
        | ValueSome x -> JsonSerializer.Serialize<'T>(writer, x, options)

type JsonUnwrappedUnionConverter<'T, 'FieldT>(case: UnionCaseInfo) =
    inherit JsonConverter<'T>()

    let ctor = FSharpValue.PreComputeUnionConstructor(case, true)
    let getter = FSharpValue.PreComputeUnionReader(case, true)

    override _.Read(reader, _typeToConvert, options) =
        ctor [| box (JsonSerializer.Deserialize<'FieldT>(&reader, options)) |]
        :?> 'T

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize<'FieldT>(writer, (getter value).[0] :?> 'FieldT, options)

type JsonUnionConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    static let jsonUnionConverterTy = typedefof<JsonUnionConverter<_>>
    static let optionTy = typedefof<option<_>>
    static let voptionTy = typedefof<voption<_>>
    static let jsonUnwrapOptionConverterTy = typedefof<JsonUnwrapOptionConverter<_>>
    static let jsonUnwrapValueOptionConverterTy = typedefof<JsonUnwrapValueOptionConverter<_>>
    static let jsonUnwrappedUnionConverterTy = typedefof<JsonUnwrappedUnionConverter<_, _>>
    static let optionsTy = typeof<JsonSerializerOptions>
    static let fsOptionsTy = typeof<JsonFSharpOptions>
    static let caseTy = typeof<UnionCaseInfo>
    static let casesTy = typeof<UnionCaseInfo[]>

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter(typeToConvert: Type, options: JsonSerializerOptions, fsOptions: JsonFSharpOptions) =
        if fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapOption
            && typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() = optionTy then
            jsonUnwrapOptionConverterTy
                .MakeGenericType(typeToConvert.GetGenericArguments())
                .GetConstructor([||])
                .Invoke([||])
            :?> JsonConverter
        elif fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapOption
            && typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() = voptionTy then
            jsonUnwrapValueOptionConverterTy
                .MakeGenericType(typeToConvert.GetGenericArguments())
                .GetConstructor([||])
                .Invoke([||])
            :?> JsonConverter
        else
            let cases = FSharpType.GetUnionCases(typeToConvert, true)
            let mutable fields = Unchecked.defaultof<_>
            let isUnwrappedSingleCase =
                fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleCaseUnions
                && cases.Length = 1
                && (fields <- cases.[0].GetFields(); fields.Length = 1)
            if isUnwrappedSingleCase then
                let case = cases.[0]
                jsonUnwrappedUnionConverterTy
                    .MakeGenericType([|typeToConvert; fields.[0].PropertyType|])
                    .GetConstructor([|caseTy|])
                    .Invoke([|case|])
                :?> JsonConverter
            else
                jsonUnionConverterTy
                    .MakeGenericType([|typeToConvert|])
                    .GetConstructor([|optionsTy; fsOptionsTy; casesTy|])
                    .Invoke([|options; fsOptions; cases|])
                :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions)
