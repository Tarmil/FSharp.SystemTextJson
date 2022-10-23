namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type private Field =
    { Type: Type
      Names: string[]
      NullValue: obj voption
      MustBePresent: bool
      IsSkip: obj -> bool }

type private Case =
    { Fields: Field[]
      DefaultFields: obj[]
      FieldsByName: Dictionary<string, struct (int * Field)> voption
      Ctor: obj[] -> obj
      Dector: obj -> obj[]
      Names: JsonName[]
      NamesAsString: string[]
      UnwrappedSingleField: bool
      UnwrappedRecordField: ValueOption<IRecordConverter>
      MinExpectedFieldCount: int }

type JsonUnionConverter<'T>
    (
        options: JsonSerializerOptions,
        fsOptions: JsonFSharpOptions,
        cases: UnionCaseInfo[],
        overrides: IDictionary<Type, JsonFSharpOptions>
    ) =
    inherit JsonConverter<'T>()

    [<Literal>]
    let UntaggedBit = enum<JsonUnionEncoding> 0x00_08
    let baseFormat =
        let given = fsOptions.UnionEncoding &&& enum<JsonUnionEncoding> 0x00_fe
        if given = enum 0 then JsonUnionEncoding.AdjacentTag else given
    let namedFields = fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.NamedFields
    let unwrapFieldlessTags =
        fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapFieldlessTags

    let ty = typeof<'T>

    let nullValue = tryGetNullValue fsOptions ty |> ValueOption.map (fun x -> x :?> 'T)

    let cases =
        cases
        |> Array.map (fun uci ->
            let names =
                match getJsonNames "case" uci.GetCustomAttributes with
                | ValueSome name -> name
                | ValueNone -> [| JsonName.String(convertName fsOptions.UnionTagNamingPolicy uci.Name) |]
            let fieldNames = getJsonFieldNames uci.GetCustomAttributes
            let fields =
                let fields = uci.GetFields()
                let usedFieldNames = Dictionary()
                let fieldsAndNames =
                    if fsOptions.UnionEncoding.HasFlag(JsonUnionEncoding.UnionFieldNamesFromTypes) then
                        fields
                        |> Array.mapi (fun i p ->
                            let useTypeName =
                                if i = 0 && fields.Length = 1 then
                                    p.Name = "Item"
                                else
                                    p.Name = "Item" + string (i + 1)
                            let name = if useTypeName then p.PropertyType.Name else p.Name
                            let nameIndex =
                                match usedFieldNames.TryGetValue(name) with
                                | true, ix -> ix + 1
                                | false, _ -> 1
                            usedFieldNames[name] <- nameIndex
                            p, name, nameIndex
                        )
                    else
                        fields |> Array.map (fun p -> p, p.Name, 1)
                fieldsAndNames
                |> Array.map (fun (p, name, nameIndex) ->
                    let name =
                        let mutable nameCount = 1
                        if
                            nameIndex = 1
                            && not (usedFieldNames.TryGetValue(name, &nameCount) && nameCount > 1)
                        then
                            name
                        else
                            name + string nameIndex
                    let canBeSkipped = ignoreNullValues options || isSkippableType p.PropertyType
                    let names =
                        match fieldNames.TryGetValue(name) with
                        | true, names -> names |> Array.map (fun n -> n.AsString())
                        | false, _ ->
                            let policy =
                                match fsOptions.UnionFieldNamingPolicy with
                                | null -> options.PropertyNamingPolicy
                                | policy -> policy
                            [| convertName policy name |]
                    { Type = p.PropertyType
                      Names = names
                      NullValue = tryGetNullValue fsOptions p.PropertyType
                      MustBePresent = not canBeSkipped
                      IsSkip = isSkip p.PropertyType }
                )
            let fieldsByName =
                if options.PropertyNameCaseInsensitive then
                    let d = Dictionary(StringComparer.OrdinalIgnoreCase)
                    fields
                    |> Array.iteri (fun i f ->
                        for name in f.Names do
                            d[name] <- struct (i, f)
                    )
                    ValueSome d
                else
                    ValueNone
            let unwrappedRecordField =
                if namedFields
                   && fields.Length = 1
                   && FSharpType.IsRecord(fields[0].Type, true)
                   && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapRecordCases then
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
            { Fields = fields
              DefaultFields = defaultFields
              FieldsByName = fieldsByName
              Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
              Dector = FSharpValue.PreComputeUnionReader(uci, true)
              Names = names
              NamesAsString = names |> Array.map (fun n -> n.AsString())
              UnwrappedSingleField = unwrappedSingleField
              UnwrappedRecordField = unwrappedRecordField
              MinExpectedFieldCount = fields |> Seq.filter (fun f -> f.MustBePresent) |> Seq.length }
        )

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
                match case.UnwrappedRecordField with
                | ValueNone -> case.Fields |> Array.collect (fun f -> f.Names |> Array.map (fun n -> n, case))
                | ValueSome r -> r.FieldNames |> Array.map (fun n -> n, case)
            )
        let fields =
            (Map.empty, cases)
            ||> Array.fold (fun foundFieldNames (fieldName, case) ->
                if hasDuplicateFieldNames then
                    foundFieldNames
                else
                    if foundFieldNames |> Map.containsKey fieldName then
                        hasDuplicateFieldNames <- true
                    Map.add fieldName case foundFieldNames
            )
        let fields = [| for KeyValue (k, v) in fields -> struct (k, v) |]
        not hasDuplicateFieldNames, fieldlessCase, fields

    let allFieldsByName =
        if hasDistinctFieldNames && options.PropertyNameCaseInsensitive then
            let dict = Dictionary(StringComparer.OrdinalIgnoreCase)
            for _, c in allFields do
                match c.FieldsByName with
                | ValueNone -> ()
                | ValueSome fields ->
                    for KeyValue (n, _) in fields do
                        dict[n] <- c
            ValueSome dict
        else
            ValueNone

    let casesByName =
        if fsOptions.UnionTagCaseInsensitive then
            let dict = Dictionary(JsonNameComparer(StringComparer.OrdinalIgnoreCase))
            for c in cases do
                for name in c.Names do
                    dict[name] <- c
                    match name with
                    | JsonName.String _ -> ()
                    | name ->
                        let stringName = JsonName.String(name.AsString())
                        if not (dict.ContainsKey(stringName)) then dict[stringName] <- c
            ValueSome dict
        else
            ValueNone

    let casesByJsonType =
        if fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.Untagged
           && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleFieldCases then
            let dict = Dictionary<JsonTokenType, Case>()
            for c in cases do
                let clrType = c.Fields[0].Type
                let typeCode = Type.GetTypeCode(c.Fields[0].Type)
                match typeCode with
                | TypeCode.Byte
                | TypeCode.SByte
                | TypeCode.UInt16
                | TypeCode.UInt32
                | TypeCode.UInt64
                | TypeCode.Int16
                | TypeCode.Int32
                | TypeCode.Int64
                | TypeCode.Decimal
                | TypeCode.Double
                | TypeCode.Single -> dict[JsonTokenType.Number] <- c
                | TypeCode.Boolean ->
                    dict[JsonTokenType.True] <- c
                    dict[JsonTokenType.False] <- c
                | TypeCode.DateTime
                | TypeCode.String -> dict[JsonTokenType.String] <- c
                | TypeCode.Object when typeof<System.Collections.IEnumerable>.IsAssignableFrom (clrType) ->
                    dict[JsonTokenType.StartArray] <- c
                | TypeCode.Object -> dict[JsonTokenType.StartObject] <- c
                | _ -> ()
            ValueSome dict
        else
            ValueNone

    let getJsonName (reader: byref<Utf8JsonReader>) =
        match reader.TokenType with
        | JsonTokenType.True -> JsonName.Bool true
        | JsonTokenType.False -> JsonName.Bool false
        | JsonTokenType.Number ->
            match reader.TryGetInt32() with
            | true, intName -> JsonName.Int intName
            | false, _ -> failExpecting "union tag" &reader ty
        | JsonTokenType.String -> JsonName.String(reader.GetString())
        | _ -> failExpecting "union tag" &reader ty

    let caseIsNamedFromReader (case: Case) (reader: byref<Utf8JsonReader>) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        while found.IsNone && i < case.NamesAsString.Length do
            if reader.ValueTextEquals(case.NamesAsString[i]) then
                found <- ValueSome case
            else
                i <- i + 1

    let getCaseByPropertyName (reader: byref<Utf8JsonReader>) =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases[i]
                    caseIsNamedFromReader case &reader &found
                    i <- i + 1
                found
            | ValueSome d ->
                let key = reader.GetString()
                match d.TryGetValue(JsonName.String key) with
                | true, c -> ValueSome c
                | false, _ -> ValueNone
        match found with
        | ValueNone -> failf "Unknown case for union type %s: %s" ty.FullName (reader.GetString())
        | ValueSome case -> case

    let caseIsNamedFromReaderString (case: Case) (reader: byref<Utf8JsonReader>) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        while found.IsNone && i < case.Names.Length do
            match case.Names[i] with
            | JsonName.String name when reader.ValueTextEquals(name) -> found <- ValueSome case
            | _ -> i <- i + 1

    let caseIsNamedFromReaderInt (case: Case) (reader: byref<Utf8JsonReader>) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        let mutable intName = 0
        while found.IsNone && i < case.Names.Length do
            match case.Names[i] with
            | JsonName.Int name when reader.TryGetInt32(&intName) && intName = name -> found <- ValueSome case
            | _ -> i <- i + 1

    let caseIsNamedFromBool (case: Case) (expected: bool) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        while found.IsNone && i < case.Names.Length do
            match case.Names[i] with
            | JsonName.Bool name when name = expected -> found <- ValueSome case
            | _ -> i <- i + 1

    let getCaseByTagReader (reader: byref<Utf8JsonReader>) =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases[i]
                    match reader.TokenType with
                    | JsonTokenType.String -> caseIsNamedFromReaderString case &reader &found
                    | JsonTokenType.Number -> caseIsNamedFromReaderInt case &reader &found
                    | JsonTokenType.True -> caseIsNamedFromBool case true &found
                    | JsonTokenType.False -> caseIsNamedFromBool case false &found
                    | _ -> ()
                    i <- i + 1
                found
            | ValueSome d ->
                match d.TryGetValue(getJsonName &reader) with
                | true, c -> ValueSome c
                | false, _ -> ValueNone
        match found with
        | ValueNone -> failf "Unknown case for union type %s: %s" ty.FullName (reader.GetString())
        | ValueSome case -> case

    let caseIsNamedFromElementString (case: Case) (element: JsonElement) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        while found.IsNone && i < case.Names.Length do
            match case.Names[i] with
            | JsonName.String name when element.ValueEquals(name) -> found <- ValueSome case
            | _ -> i <- i + 1

    let caseIsNamedFromElementInt (case: Case) (element: JsonElement) (found: byref<ValueOption<_>>) =
        let mutable i = 0
        let mutable intName = 0
        while found.IsNone && i < case.Names.Length do
            match case.Names[i] with
            | JsonName.Int name when element.TryGetInt32(&intName) && intName = name -> found <- ValueSome case
            | _ -> i <- i + 1

    let getCaseByTagElement (element: JsonElement) =
        let found =
            match casesByName with
            | ValueNone ->
                let mutable found = ValueNone
                let mutable i = 0
                while found.IsNone && i < cases.Length do
                    let case = cases[i]
                    match element.ValueKind with
                    | JsonValueKind.String -> caseIsNamedFromElementString case element &found
                    | JsonValueKind.Number -> caseIsNamedFromElementInt case element &found
                    | JsonValueKind.True -> caseIsNamedFromBool case true &found
                    | JsonValueKind.False -> caseIsNamedFromBool case false &found
                    | _ -> ()
                    i <- i + 1
                found
            | ValueSome d ->
                let mutable intName = 0
                let name =
                    match element.ValueKind with
                    | JsonValueKind.String -> JsonName.String(element.GetString())
                    | JsonValueKind.Number when element.TryGetInt32(&intName) -> JsonName.Int intName
                    | JsonValueKind.True -> JsonName.Bool true
                    | JsonValueKind.False -> JsonName.Bool false
                    | _ -> failf "Unknown case for union type %s: %s" ty.FullName (element.ToString())
                match d.TryGetValue(name) with
                | true, c -> ValueSome c
                | false, _ -> ValueNone
        match found with
        | ValueNone -> failf "Unknown case for union type %s: %s" ty.FullName (element.ToString())
        | ValueSome case -> case


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
        | ValueNone -> failf "Unknown case for union type %s due to unknown field: %s" ty.FullName (reader.GetString())
        | ValueSome case -> case

    let fieldIndexByName (reader: byref<Utf8JsonReader>) (case: Case) =
        match case.FieldsByName with
        | ValueNone ->
            let mutable found = ValueNone
            let mutable i = 0
            while found.IsNone && i < case.Fields.Length do
                let field = case.Fields[i]
                let mutable j = 0
                while found.IsNone && j < field.Names.Length do
                    if reader.ValueTextEquals(field.Names[j]) then
                        found <- ValueSome(struct (i, field))
                    else
                        j <- j + 1
                i <- i + 1
            found
        | ValueSome d ->
            match d.TryGetValue(reader.GetString()) with
            | true, p -> ValueSome p
            | false, _ -> ValueNone

    let readField (reader: byref<Utf8JsonReader>) (case: Case) (f: Field) (options: JsonSerializerOptions) =
        reader.Read() |> ignore
        if reader.TokenType = JsonTokenType.Null then
            match f.NullValue with
            | ValueSome v -> v
            | ValueNone ->
                failf
                    "%s.%s(%s) was expected to be of type %s, but was null."
                    ty.Name
                    (case.Names[ 0 ].AsString())
                    f.Names[0]
                    f.Type.Name
        else
            JsonSerializer.Deserialize(&reader, f.Type, options)

    let readFieldsAsRestOfArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.copy case.DefaultFields
        for i in 0 .. fieldCount - 1 do
            fields[i] <- readField &reader case case.Fields[i] options
        readExpecting JsonTokenType.EndArray "end of array" &reader ty
        case.Ctor fields :?> 'T

    let readFieldsAsArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartArray "array" &reader ty
        readFieldsAsRestOfArray &reader case options

    let coreReadFieldsAsRestOfObject
        (reader: byref<Utf8JsonReader>)
        (case: Case)
        (skipFirstRead: bool)
        (options: JsonSerializerOptions)
        =
        let fields = Array.copy case.DefaultFields
        let mutable cont = true
        let mutable fieldsFound = 0
        let mutable skipRead = skipFirstRead
        while cont && (skipRead || reader.Read()) do
            match reader.TokenType with
            | JsonTokenType.EndObject -> cont <- false
            | JsonTokenType.PropertyName ->
                skipRead <- false
                match fieldIndexByName &reader case with
                | ValueSome (i, f) ->
                    fieldsFound <- fieldsFound + 1
                    fields[i] <- readField &reader case f options
                | _ -> reader.Skip()
            | _ -> ()

        if fieldsFound < case.MinExpectedFieldCount && not (ignoreNullValues options) then
            failf "Missing field for union type %s" ty.FullName
        case.Ctor fields :?> 'T

    let readFieldsAsRestOfObject
        (reader: byref<Utf8JsonReader>)
        (case: Case)
        (skipFirstRead: bool)
        (options: JsonSerializerOptions)
        =
        match case.UnwrappedRecordField with
        | ValueSome conv ->
            let field = conv.ReadRestOfObject(&reader, options, skipFirstRead)
            case.Ctor [| field |] :?> 'T
        | ValueNone -> coreReadFieldsAsRestOfObject &reader case skipFirstRead options

    let readFieldsAsObject (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        readExpecting JsonTokenType.StartObject "object" &reader ty
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
        | true, element -> getCaseByTagElement element
        | false, _ -> failf "Failed to find union case field for %s: expected %s" ty.FullName fsOptions.UnionTagName

    let getCase (reader: byref<Utf8JsonReader>) =
        let mutable snapshot = reader
        if readIsExpectingPropertyNamed fsOptions.UnionTagName &snapshot ty then
            readExpectingPropertyNamed fsOptions.UnionTagName &reader ty
            reader.Read() |> ignore
            struct (getCaseByTagReader &reader, false)
        elif fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.AllowUnorderedTag then
            struct (getCaseFromDocument reader, true)
        else
            failf "Failed to find union case field for %s: expected %s" ty.FullName fsOptions.UnionTagName

    let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        let struct (case, usedDocument) = getCase &reader
        let res =
            if case.Fields.Length > 0 then
                readExpectingPropertyNamed fsOptions.UnionFieldsName &reader ty
                readFields &reader case options
            else
                case.Ctor [||] :?> 'T
        if usedDocument then
            reader.Read() |> ignore
            reader.Skip()
        readExpecting JsonTokenType.EndObject "end of object" &reader ty
        res

    let readExternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
        readExpecting JsonTokenType.PropertyName "case name" &reader ty
        let case = getCaseByPropertyName &reader
        let res = readFields &reader case options
        readExpecting JsonTokenType.EndObject "end of object" &reader ty
        res

    let readInternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if namedFields then
            expectAlreadyRead JsonTokenType.StartObject "object" &reader ty
            let mutable snapshot = reader
            let struct (case, _usedDocument) = getCase &snapshot
            readFieldsAsRestOfObject &reader case false options
        else
            expectAlreadyRead JsonTokenType.StartArray "array" &reader ty
            reader.Read() |> ignore
            let case = getCaseByTagReader &reader
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
            | ValueNone -> failExpecting "case field" &reader ty
        | _ -> failExpecting "case field" &reader ty

    let getCaseByElementType (reader: byref<Utf8JsonReader>) =
        let found =
            match casesByJsonType with
            | ValueNone -> ValueNone
            | ValueSome d ->
                match d.TryGetValue(reader.TokenType) with
                | true, p -> ValueSome p
                | false, _ -> ValueNone
        match found with
        | ValueNone ->
            failf "Unknown case for union type %s due to unmatched field type: %s" ty.FullName (reader.GetString())
        | ValueSome case -> case

    let readUnwrapedUntagged (reader: byref<Utf8JsonReader>) =
        let case = getCaseByElementType &reader
        let field = JsonSerializer.Deserialize(&reader, case.Fields[0].Type, options)
        case.Ctor [| field |] :?> 'T

    let writeFieldsAsRestOfArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        let fields = case.Fields
        let values = case.Dector value
        for i in 0 .. fields.Length - 1 do
            JsonSerializer.Serialize(writer, values[i], fields[i].Type, options)
        writer.WriteEndArray()

    let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        writeFieldsAsRestOfArray writer case value options

    let coreWriteFieldsAsRestOfObject
        (writer: Utf8JsonWriter)
        (case: Case)
        (value: obj)
        (options: JsonSerializerOptions)
        =
        let fields = case.Fields
        let values = case.Dector value
        for i in 0 .. fields.Length - 1 do
            let f = fields[i]
            let v = values[i]
            if not (ignoreNullValues options && isNull v) && not (f.IsSkip v) then
                writer.WritePropertyName(f.Names[0])
                JsonSerializer.Serialize(writer, v, f.Type, options)
        writer.WriteEndObject()

    let writeFieldsAsRestOfObject (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        match case.UnwrappedRecordField with
        | ValueSome conv -> conv.WriteRestOfObject(writer, (case.Dector value)[0], options)
        | ValueNone -> coreWriteFieldsAsRestOfObject writer case value options

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

    let writeCaseNameAsField (writer: Utf8JsonWriter) (case: Case) =
        match case.Names[0] with
        | JsonName.String name -> writer.WriteString(fsOptions.UnionTagName, name)
        | JsonName.Int name -> writer.WriteNumber(fsOptions.UnionTagName, name)
        | JsonName.Bool name -> writer.WriteBoolean(fsOptions.UnionTagName, name)

    let writeCaseNameAsValue (writer: Utf8JsonWriter) (case: Case) =
        match case.Names[0] with
        | JsonName.String name -> writer.WriteStringValue(name)
        | JsonName.Int name -> writer.WriteNumberValue(name)
        | JsonName.Bool name -> writer.WriteBooleanValue(name)

    let writeAdjacentTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writeCaseNameAsField writer case
        if case.Fields.Length > 0 then
            writer.WritePropertyName(fsOptions.UnionFieldsName)
            writeFields writer case value options
        writer.WriteEndObject()

    let writeExternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartObject()
        writer.WritePropertyName(case.Names[ 0 ].AsString())
        writeFields writer case value options
        writer.WriteEndObject()

    let writeInternalTag (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        if namedFields then
            writer.WriteStartObject()
            writeCaseNameAsField writer case
            writeFieldsAsRestOfObject writer case value options
        else
            writer.WriteStartArray()
            writeCaseNameAsValue writer case
            writeFieldsAsRestOfArray writer case value options

    let writeUntagged (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        if case.UnwrappedSingleField then
            JsonSerializer.Serialize(writer, (case.Dector value)[0], case.Fields[0].Type, options)
        else
            writeFieldsAsObject writer case value options

    override _.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null ->
            nullValue
            |> ValueOption.defaultWith (fun () -> failf "Union %s can't be deserialized from null" ty.FullName)
        | JsonTokenType.String
        | JsonTokenType.Number
        | JsonTokenType.True
        | JsonTokenType.False when unwrapFieldlessTags ->
            let case = getCaseByTagReader &reader
            case.Ctor [||] :?> 'T
        | _ ->
            match baseFormat with
            | JsonUnionEncoding.AdjacentTag -> readAdjacentTag &reader options
            | JsonUnionEncoding.ExternalTag -> readExternalTag &reader options
            | JsonUnionEncoding.InternalTag -> readInternalTag &reader options
            | UntaggedBit ->
                if fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleFieldCases then
                    readUnwrapedUntagged &reader
                elif not hasDistinctFieldNames then
                    failf
                        "Union %s can't be deserialized as Untagged because it has duplicate field names across unions"
                        ty.FullName
                else
                    readUntagged &reader options
            | _ -> failf "Invalid union encoding: %A" fsOptions.UnionEncoding

    override _.Write(writer, value, options) =
        let value = box value
        if isNull value then
            writer.WriteNullValue()
        else

            let tag = tagReader value
            let case = cases[tag]
            if unwrapFieldlessTags && case.Fields.Length = 0 then
                writeCaseNameAsValue writer case
            else
                match baseFormat with
                | JsonUnionEncoding.AdjacentTag -> writeAdjacentTag writer case value options
                | JsonUnionEncoding.ExternalTag -> writeExternalTag writer case value options
                | JsonUnionEncoding.InternalTag -> writeInternalTag writer case value options
                | UntaggedBit -> writeUntagged writer case value options
                | _ -> failf "Invalid union encoding: %A" fsOptions.UnionEncoding

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
        ctor [| box (JsonSerializer.Deserialize<'FieldT>(&reader, options)) |] :?> 'T

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
            match tryGetUnwrappedSingleCaseField (fsOptions, typeToConvert) with
            | true, cases, unwrappedSingleCaseField ->
                let case = cases[0]
                jsonUnwrappedUnionConverterTy
                    .MakeGenericType([| typeToConvert; unwrappedSingleCaseField.PropertyType |])
                    .GetConstructor([| caseTy |])
                    .Invoke([| case |])
                :?> JsonConverter
            | false, cases, _ ->
                jsonUnionConverterTy
                    .MakeGenericType([| typeToConvert |])
                    .GetConstructor([| optionsTy; fsOptionsTy; casesTy; overridesTy |])
                    .Invoke([| options; fsOptions; cases; overrides |])
                :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions, null)
