namespace System.Text.Json.Serialization

#nowarn "44" // JsonSerializerOptions.IgnoreNullValues is obsolete for users but still relevant for converters.

open System
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type private Field =
    {
        Type: Type
        Name: string
        MustBeNonNull: bool
        MustBePresent: bool
        IsSkip: obj -> bool
    }

type private Case =
    {
        Fields: Field[]
        DefaultFields: obj[]
        FieldsByName: Dictionary<string, struct (int * Field)> voption
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
        Name: string
        UnwrappedSingleField: bool
        UnwrappedRecordField: ValueOption<IRecordConverter>
        MinExpectedFieldCount: int
    }

type JsonUnionConverter<'T>
    (
        options: JsonSerializerOptions,
        fsOptions: JsonFSharpOptions,
        cases: UnionCaseInfo[],
        overrides: IDictionary<Type, JsonFSharpOptions>
    ) =
    inherit JsonConverter<'T>()

    let [<Literal>] UntaggedBit = enum<JsonUnionEncoding> 0x00_08
    let baseFormat =
        let given = fsOptions.UnionEncoding &&& enum<JsonUnionEncoding> 0x00_fe
        if given = enum 0 then JsonUnionEncoding.AdjacentTag else given
    let namedFields = fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.NamedFields
    let unwrapFieldlessTags = fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapFieldlessTags

    static let unionType = typeof<'T>

    static let hasOnSerializing = unionType.IsAssignableFrom(typeof<IJsonOnSerializing>)
    static let hasOnSerialized = unionType.IsAssignableFrom(typeof<IJsonOnSerialized>)
    static let hasOnDeserializing = unionType.IsAssignableFrom(typeof<IJsonOnDeserializing>)
    static let hasOnDeserialized = unionType.IsAssignableFrom(typeof<IJsonOnDeserialized>)

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
                        MustBePresent = not (isSkippableFieldType fsOptions p.PropertyType)
                        IsSkip = isSkip p.PropertyType
                    })
            let fieldsByName =
                if options.PropertyNameCaseInsensitive then
                    let d = Dictionary(StringComparer.OrdinalIgnoreCase)
                    fields |> Array.iteri (fun i f ->
                        d[f.Name] <- struct (i, f))
                    ValueSome d
                else
                    ValueNone
            let unwrappedRecordField =
                if namedFields
                    && fields.Length = 1
                    && FSharpType.IsRecord(fields[0].Type, true)
                    && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapRecordCases
                then
                    JsonRecordConverter.CreateConverter(fields[0].Type, options, fsOptions, overrides)
                    |> box
                    :?> IRecordConverter
                    |> ValueSome
                else
                    ValueNone
            let unwrappedSingleField =
                ValueOption.isNone unwrappedRecordField
                && fields.Length = 1
                && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleFieldCases
            let defaultFields =
                let arr = Array.zeroCreate fields.Length
                fields
                |> Array.iteri (fun i field ->
                    if isSkippableType field.Type || isValueOptionType field.Type then
                        let case = FSharpType.GetUnionCases(field.Type)[0]
                        arr[i] <- FSharpValue.MakeUnion(case, [||])
                )
                arr
            {
                Fields = fields
                DefaultFields = defaultFields
                FieldsByName = fieldsByName
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
                Name = name
                UnwrappedSingleField = unwrappedSingleField
                UnwrappedRecordField = unwrappedRecordField
                MinExpectedFieldCount = fields |> Seq.filter (fun f -> f.MustBePresent) |> Seq.length
            })

    let tagReader = FSharpValue.PreComputeUnionTagReader(unionType, true)

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
                match case.UnwrappedRecordField with
                | ValueNone -> case.Fields |> Array.map (fun f -> f.Name, case)
                | ValueSome r -> r.FieldNames |> Array.map (fun n -> n, case))
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
                        dict[n] <- c
            ValueSome dict
        else
            ValueNone

    let casesByName =
        if fsOptions.UnionTagCaseInsensitive then
            let dict = Dictionary(StringComparer.OrdinalIgnoreCase)
            for c in cases do
                dict[c.Name] <- c
            ValueSome dict
        else
            ValueNone

    let getCaseByTagReader (reader: byref<Utf8JsonReader>) =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases[i]
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
            raise (JsonException("Unknown case for union type " + unionType.FullName + ": " + reader.GetString()))
        | ValueSome case ->
            case

    let getCaseByTagString tag =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases[i]
                    if case.Name.Equals(tag, StringComparison.OrdinalIgnoreCase) then
                        found <- ValueSome case
                    else
                        i <- i + 1
                found
            | ValueSome d ->
                match d.TryGetValue(tag) with
                | true, c -> ValueSome c
                | false, _ -> ValueNone
        match found with
        | ValueNone ->
            raise (JsonException("Unknown case for union type " + unionType.FullName + ": " + tag))
        | ValueSome case ->
            case

    let getCaseByFieldName (reader: byref<Utf8JsonReader>) =
        let found =
            match allFieldsByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < allFields.Length do
                    let struct (fieldName, case) = allFields[i]
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
            raise (JsonException("Unknown case for union type " + unionType.FullName + " due to unknown field: " + reader.GetString()))
        | ValueSome case ->
            case

    let fieldIndexByName (reader: byref<Utf8JsonReader>) (case: Case) =
        match case.FieldsByName with
        | ValueNone ->
            let mutable found = ValueNone
            let mutable i = 0
            while found.IsNone && i < case.Fields.Length do
                let field = case.Fields[i]
                if reader.ValueTextEquals(field.Name) then
                    found <- ValueSome (struct (i, field))
                else
                    i <- i + 1
            found
        | ValueSome d ->
            match d.TryGetValue(reader.GetString()) with
            | true, p -> ValueSome p
            | false, _ -> ValueNone

    let readField (reader: byref<Utf8JsonReader>) (case: Case) (f: Field) (options: JsonSerializerOptions) =
        reader.Read() |> ignore
        if f.MustBeNonNull && reader.TokenType = JsonTokenType.Null then
            let msg = sprintf "%s.%s(%s) was expected to be of type %s, but was null." unionType.Name case.Name f.Name f.Type.Name
            raise (JsonException msg)
        else
            JsonSerializer.Deserialize(&reader, f.Type, options)

    let readFieldsAsRestOfArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.copy case.DefaultFields
        for i in 0..fieldCount-1 do
            fields[i] <- readField &reader case case.Fields[i] options
        readExpecting JsonTokenType.EndArray "end of array" &reader unionType
        case.Ctor fields :?> 'T

    let readFieldsAsArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartArray "array" &reader unionType
        readFieldsAsRestOfArray &reader case options

    let coreReadFieldsAsRestOfObject (reader: byref<Utf8JsonReader>) (case: Case) (skipFirstRead: bool) (options: JsonSerializerOptions) =
        let fields = Array.copy case.DefaultFields
        let mutable cont = true
        let mutable fieldsFound = 0
        let mutable skipRead = skipFirstRead
        while cont && (skipRead || reader.Read()) do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                skipRead <- false
                match fieldIndexByName &reader case with
                | ValueSome (i, f) ->
                    fieldsFound <- fieldsFound + 1
                    fields[i] <- readField &reader case f options
                | _ ->
                    reader.Skip()
            | _ -> ()

        if fieldsFound < case.MinExpectedFieldCount && not options.IgnoreNullValues then
            raise (JsonException("Missing field for union type " + unionType.FullName))
        case.Ctor fields :?> 'T

    let readFieldsAsRestOfObject (reader: byref<Utf8JsonReader>) (case: Case) (skipFirstRead: bool) (options: JsonSerializerOptions) =
        match case.UnwrappedRecordField with
        | ValueSome conv ->
            let field = conv.ReadRestOfObject(&reader, options, skipFirstRead)
            case.Ctor [| field |] :?> 'T
        | ValueNone ->
            coreReadFieldsAsRestOfObject &reader case skipFirstRead options

    let readFieldsAsObject (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartObject "object" &reader unionType
        readFieldsAsRestOfObject &reader case false options

    let readFields (reader: byref<Utf8JsonReader>) case options =
        match case.UnwrappedRecordField with
        | ValueSome conv ->
            let field = conv.ReadRestOfObject(&reader, options, false)
            case.Ctor [| field |] :?> 'T
        | ValueNone ->
        if case.UnwrappedSingleField then
            let field = readField &reader case case.Fields[0] options
            case.Ctor [| field |] :?> 'T
        elif namedFields then
            readFieldsAsObject &reader case options
        else
            readFieldsAsArray &reader case options

    let getCaseFromDocument (reader: Utf8JsonReader) =
        let mutable reader = reader
        let document = JsonDocument.ParseValue(&reader)
        match document.RootElement.TryGetProperty fsOptions.UnionTagName with
        | true, element -> getCaseByTagString (element.GetString())
        | false, _ ->
            sprintf "Failed to find union case field for %s: expected %s" unionType.FullName fsOptions.UnionTagName
            |> JsonException
            |> raise

    let getCase (reader: byref<Utf8JsonReader>) =
        let mutable snapshot = reader
        if readIsExpectingPropertyNamed fsOptions.UnionTagName &snapshot unionType then
            readExpectingPropertyNamed fsOptions.UnionTagName &reader unionType
            readExpecting JsonTokenType.String "case name" &reader unionType
            struct (getCaseByTagReader &reader, false)
        elif fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.AllowUnorderedTag then
            struct (getCaseFromDocument reader, true)
        else
            sprintf "Failed to find union case field for %s: expected %s" unionType.FullName fsOptions.UnionTagName
            |> JsonException
            |> raise

    let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader unionType
        let struct (case, usedDocument) = getCase &reader
        let res =
            if case.Fields.Length > 0 then
                readExpectingPropertyNamed fsOptions.UnionFieldsName &reader unionType
                readFields &reader case options
            else
                case.Ctor [||] :?> 'T
        if usedDocument then
            reader.Read() |> ignore
            reader.Skip()
        readExpecting JsonTokenType.EndObject "end of object" &reader unionType
        res

    let readExternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader unionType
        readExpecting JsonTokenType.PropertyName "case name" &reader unionType
        let case = getCaseByTagReader &reader
        let res = readFields &reader case options
        readExpecting JsonTokenType.EndObject "end of object" &reader unionType
        res

    let readInternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if namedFields then
            expectAlreadyRead JsonTokenType.StartObject "object" &reader unionType
            let mutable snapshot = reader
            let struct (case, _usedDocument) = getCase &snapshot
            readFieldsAsRestOfObject &reader case false options
        else
            expectAlreadyRead JsonTokenType.StartArray "array" &reader unionType
            readExpecting JsonTokenType.String "case name" &reader unionType
            let case = getCaseByTagReader &reader
            readFieldsAsRestOfArray &reader case options

    let readUntagged (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader unionType
        reader.Read() |> ignore
        match reader.TokenType with
        | JsonTokenType.PropertyName ->
            let case = getCaseByFieldName &reader
            readFieldsAsRestOfObject &reader case true options
        | JsonTokenType.EndObject ->
            match fieldlessCase with
            | ValueSome case -> case.Ctor [||] :?> 'T
            | ValueNone -> fail "case field" &reader unionType
        | _ ->
            fail "case field" &reader unionType

    let writeFieldsAsRestOfArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        let fields = case.Fields
        let values = case.Dector value
        for i in 0..fields.Length-1 do
            JsonSerializer.Serialize(writer, values[i], fields[i].Type, options)
        writer.WriteEndArray()

    let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        writeFieldsAsRestOfArray writer case value options

    let coreWriteFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        let fields = case.Fields
        let values = case.Dector value
        for i in 0..fields.Length-1 do
            let f = fields[i]
            let v = values[i]
            if not (options.IgnoreNullValues && isNull v) && not (f.IsSkip v) then
                writer.WritePropertyName(f.Name)
                JsonSerializer.Serialize(writer, v, f.Type, options)
        writer.WriteEndObject()

    let writeFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        match case.UnwrappedRecordField with
        | ValueSome conv ->
            conv.WriteRestOfObject(writer, (case.Dector value)[0], options)
        | ValueNone ->
            coreWriteFieldsAsRestOfObject writer case value options

    let writeFieldsAsObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writeFieldsAsRestOfObject writer case value options

    let writeFields (writer: Utf8JsonWriter) case value (options: JsonSerializerOptions) =
        if case.UnwrappedSingleField then
            JsonSerializer.Serialize(writer, (case.Dector value)[0], case.Fields[0].Type, options)
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
        let v =
            match reader.TokenType with
            | JsonTokenType.Null when isNullableUnion unionType ->
                (null : obj) :?> 'T
            | JsonTokenType.String when unwrapFieldlessTags ->
                let case = getCaseByTagReader &reader
                case.Ctor [||] :?> 'T
            | _ ->
                match baseFormat with
                | JsonUnionEncoding.AdjacentTag -> readAdjacentTag &reader options
                | JsonUnionEncoding.ExternalTag -> readExternalTag &reader options
                | JsonUnionEncoding.InternalTag -> readInternalTag &reader options
                | UntaggedBit ->
                    if not hasDistinctFieldNames then
                        raise (JsonException(sprintf "Union %s can't be deserialized as Untagged because it has duplicate field names across unions" unionType.FullName))
                    readUntagged &reader options
                | _ -> raise (JsonException("Invalid union encoding: " + string fsOptions.UnionEncoding))
        if hasOnDeserializing then (box v :?> IJsonOnDeserializing).OnDeserializing()
        if hasOnDeserialized then (box v :?> IJsonOnDeserialized).OnDeserialized()
        v

    override _.Write(writer, value, options) =
        if hasOnSerializing then (box value :?> IJsonOnSerializing).OnSerializing()
        let value = box value
        if isNull value then
            writer.WriteNullValue()
        else
            let tag = tagReader value
            let case = cases[tag]
            if unwrapFieldlessTags && case.Fields.Length = 0 then
                writer.WriteStringValue(case.Name)
            else
            match baseFormat with
            | JsonUnionEncoding.AdjacentTag -> writeAdjacentTag writer case value options
            | JsonUnionEncoding.ExternalTag -> writeExternalTag writer case value options
            | JsonUnionEncoding.InternalTag -> writeInternalTag writer case value options
            | UntaggedBit -> writeUntagged writer case value options
            | _ -> raise (JsonException("Invalid union encoding: " + string fsOptions.UnionEncoding))
        if hasOnSerialized then (box value :?> IJsonOnSerialized).OnSerialized()

type JsonSkippableConverter<'T>() =
    inherit JsonConverter<Skippable<'T>>()

    override _.Read(reader, _typeToConvert, options) =
        Include <| JsonSerializer.Deserialize<'T>(&reader, options)

    override _.Write(writer, value, options) =
        match value with
        | Skip -> writer.WriteNullValue()
        | Include x -> JsonSerializer.Serialize<'T>(writer, x, options)

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
        JsonSerializer.Serialize<'FieldT>(writer, (getter value)[0] :?> 'FieldT, options)

type JsonUnionConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    static let jsonUnionConverterTy = typedefof<JsonUnionConverter<_>>
    static let optionTy = typedefof<option<_>>
    static let voptionTy = typedefof<voption<_>>
    static let skippableTy = typedefof<Skippable<_>>
    static let jsonUnwrapOptionConverterTy = typedefof<JsonUnwrapOptionConverter<_>>
    static let jsonUnwrapValueOptionConverterTy = typedefof<JsonUnwrapValueOptionConverter<_>>
    static let jsonUnwrappedUnionConverterTy = typedefof<JsonUnwrappedUnionConverter<_, _>>
    static let jsonSkippableConverterTy = typedefof<JsonSkippableConverter<_>>
    static let optionsTy = typeof<JsonSerializerOptions>
    static let fsOptionsTy = typeof<JsonFSharpOptions>
    static let caseTy = typeof<UnionCaseInfo>
    static let casesTy = typeof<UnionCaseInfo[]>
    static let overridesTy = typeof<IDictionary<Type, JsonFSharpOptions>>

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter
        (
            typeToConvert: Type,
            options: JsonSerializerOptions,
            fsOptions: JsonFSharpOptions,
            overrides: IDictionary<Type, JsonFSharpOptions>
        ) =
        let fsOptions = overrideOptions typeToConvert fsOptions overrides
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
        elif typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() = skippableTy then
            jsonSkippableConverterTy
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
                && (fields <- cases[0].GetFields(); fields.Length = 1)
            if isUnwrappedSingleCase then
                let case = cases[0]
                jsonUnwrappedUnionConverterTy
                    .MakeGenericType([|typeToConvert; fields[0].PropertyType|])
                    .GetConstructor([|caseTy|])
                    .Invoke([|case|])
                :?> JsonConverter
            else
                jsonUnionConverterTy
                    .MakeGenericType([|typeToConvert|])
                    .GetConstructor([|optionsTy; fsOptionsTy; casesTy; overridesTy|])
                    .Invoke([|options; fsOptions; cases; overrides|])
                :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions, null)
