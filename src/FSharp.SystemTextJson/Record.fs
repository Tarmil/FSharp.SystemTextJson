namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Text.Json
open FSharp.Reflection
open System.Text.Json.Serialization.Helpers

type internal RecordProperty =
    {
        Name: string
        Type: Type
        Ignore: bool
        MustBeNonNull: bool
        MustBePresent: bool
        IsSkip: obj -> bool
    }

type internal IRecordConverter =
    abstract ReadRestOfObject : byref<Utf8JsonReader> * JsonSerializerOptions * skipFirstRead: bool -> obj
    abstract WriteRestOfObject : Utf8JsonWriter * obj * JsonSerializerOptions -> unit
    abstract FieldNames : string[]

type JsonRecordConverter<'T>(options: JsonSerializerOptions, fsOptions: JsonFSharpOptions) =
    inherit JsonConverter<'T>()

    let recordType: Type = typeof<'T>

    let fieldProps =
        FSharpType.GetRecordFields(recordType, true)
        |> Array.map (fun p ->
            let name =
                match p.GetCustomAttributes(typeof<JsonPropertyNameAttribute>, true) with
                | [| :? JsonPropertyNameAttribute as name |] -> name.Name
                | _ ->
                    match options.PropertyNamingPolicy with
                    | null -> p.Name
                    | policy -> policy.ConvertName p.Name
            let ignore =
                p.GetCustomAttributes(typeof<JsonIgnoreAttribute>, true)
                |> Array.isEmpty
                |> not
            let canBeNull =
                ignore
                || options.IgnoreNullValues
                || isNullableFieldType fsOptions p.PropertyType
            let canBeSkipped =
                ignore
                || options.IgnoreNullValues
                || isSkippableFieldType fsOptions p.PropertyType
            {
                Name = name
                Type = p.PropertyType
                Ignore = ignore
                MustBeNonNull = not canBeNull
                MustBePresent = not canBeSkipped
                IsSkip = isSkip p.PropertyType
            }
        )

    let fieldCount = fieldProps.Length
    let minExpectedFieldCount =
        fieldProps
        |> Seq.filter (fun p -> p.MustBePresent)
        |> Seq.length

    let ctor = FSharpValue.PreComputeRecordConstructor(recordType, true)

    let dector = FSharpValue.PreComputeRecordReader(recordType, true)

    let propertiesByName =
        if options.PropertyNameCaseInsensitive then
            let d = Dictionary(StringComparer.OrdinalIgnoreCase)
            fieldProps |> Array.iteri (fun i f ->
                if not f.Ignore then
                    d.[f.Name] <- struct (i, f))
            ValueSome d
        else
            ValueNone

    let fieldIndex (reader: byref<Utf8JsonReader>) =
        match propertiesByName with
        | ValueNone ->
            let mutable found = ValueNone
            let mutable i = 0
            while found.IsNone && i < fieldCount do
                let p = fieldProps.[i]
                if reader.ValueTextEquals(p.Name) then
                    found <- ValueSome (struct (i, p))
                else
                    i <- i + 1
            found
        | ValueSome d ->
            match d.TryGetValue(reader.GetString()) with
            | true, p -> ValueSome p
            | false, _ -> ValueNone

    override this.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader typeToConvert
        this.ReadRestOfObject(&reader, options, false)

    member internal _.ReadRestOfObject(reader, options, skipFirstRead) =
        let fields = Array.zeroCreate fieldCount
        let mutable cont = true
        let mutable requiredFieldCount = 0
        let mutable skipRead = skipFirstRead
        while cont && (skipRead || reader.Read()) do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                skipRead <- false
                match fieldIndex &reader with
                | ValueSome (i, p) when not p.Ignore ->
                    if p.MustBePresent then
                        requiredFieldCount <- requiredFieldCount + 1
                    fields.[i] <- JsonSerializer.Deserialize(&reader, p.Type, options)

                    if p.MustBeNonNull && isNull fields.[i] then
                        let msg = sprintf "%s.%s was expected to be of type %s, but was null." recordType.Name p.Name p.Type.Name
                        raise (JsonException msg)
                | _ ->
                    reader.Skip()
            | _ -> ()

        if requiredFieldCount < minExpectedFieldCount && not options.IgnoreNullValues then
            for i in 0..fieldCount-1 do
                if isNull fields.[i] && fieldProps.[i].MustBePresent then
                    raise (JsonException("Missing field for record type " + recordType.FullName + ": " + fieldProps.[i].Name))

        ctor fields :?> 'T

    override this.Write(writer, value, options) =
        writer.WriteStartObject()
        this.WriteRestOfObject(writer, value, options)

    member internal _.WriteRestOfObject(writer, value, options) =
        let values = dector value
        for i in 0..fieldProps.Length-1 do
            let v = values.[i]
            let p = fieldProps.[i]
            if not p.Ignore && not (options.IgnoreNullValues && isNull v) && not (p.IsSkip v) then
                writer.WritePropertyName(p.Name)
                JsonSerializer.Serialize(writer, v, p.Type, options)
        writer.WriteEndObject()

    interface IRecordConverter with
        member this.ReadRestOfObject(reader, options, skipFirstRead) =
            box (this.ReadRestOfObject(&reader, options, skipFirstRead))
        member this.WriteRestOfObject(writer, value, options) =
            this.WriteRestOfObject(writer, unbox value, options)
        member _.FieldNames =
            fieldProps |> Array.map (fun p -> p.Name)

type JsonRecordConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    new() =
        JsonRecordConverter(JsonFSharpOptions())

    static member internal CanConvert(typeToConvert) =
        TypeCache.isRecord typeToConvert

    static member internal CreateConverter
        (
            typeToConvert: Type,
            options: JsonSerializerOptions,
            fsOptions: JsonFSharpOptions,
            overrides: IDictionary<Type, JsonFSharpOptions>
        ) =
        let fsOptions = overrideOptions typeToConvert fsOptions overrides
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([|typeof<JsonSerializerOptions>; typeof<JsonFSharpOptions>|])
            .Invoke([|options; fsOptions|])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions, null)
