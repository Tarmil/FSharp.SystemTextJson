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
    }

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
            {
                Name = name
                Type = p.PropertyType
                Ignore = ignore
                MustBeNonNull = not (isNullableFieldType fsOptions p.PropertyType)
            }
        )

    let fieldCount = fieldProps.Length

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

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader typeToConvert

        let fields = Array.zeroCreate fieldCount
        let mutable cont = true
        while cont && reader.Read() do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                match fieldIndex &reader with
                | ValueSome (i, p) when not p.Ignore ->
                    fields.[i] <- JsonSerializer.Deserialize(&reader, p.Type, options)

                    if isNull fields.[i] && p.MustBeNonNull then
                        let msg = sprintf "%s.%s was expected to be of type %s, but was null." typeToConvert.Name p.Name p.Type.Name
                        raise (JsonException msg)
                | _ ->
                    reader.Skip()
            | _ -> ()

        for i in 0..fieldCount-1 do
            if fields.[i] = null && fieldProps.[i].MustBeNonNull && not fieldProps.[i].Ignore then
                raise (JsonException("Missing field for record type " + typeToConvert.FullName + ": " + fieldProps.[i].Name))
        ctor fields :?> 'T

    override _.Write(writer, value, options) =
        writer.WriteStartObject()
        let values = dector value
        for i in 0..fieldProps.Length-1 do
            let v = values.[i]
            let p = fieldProps.[i]
            if not p.Ignore && not (options.IgnoreNullValues && isNull v) then
                writer.WritePropertyName(p.Name)
                JsonSerializer.Serialize(writer, v, p.Type, options)
        writer.WriteEndObject()

type JsonRecordConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    new() =
        JsonRecordConverter(JsonFSharpOptions())

    static member internal CanConvert(typeToConvert) =
        TypeCache.isRecord typeToConvert

    static member internal CreateConverter(typeToConvert, options: JsonSerializerOptions, fsOptions: JsonFSharpOptions) =
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([|typeof<JsonSerializerOptions>; typeof<JsonFSharpOptions>|])
            .Invoke([|options; fsOptions|])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions)
