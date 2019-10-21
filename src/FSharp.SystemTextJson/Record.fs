namespace System.Text.Json.Serialization

open System
open System.Text.Json
open FSharp.Reflection
open System.Text.Json.Serialization.Helpers

type internal RecordProperty =
    {
        Name: string
        Type: Type
        Ignore: bool
    }

type JsonRecordConverter<'T>() =
    inherit JsonConverter<'T>()

    let fieldProps =
        FSharpType.GetRecordFields(typeof<'T>, true)
        |> Array.map (fun p ->
            let name =
                match p.GetCustomAttributes(typeof<JsonPropertyNameAttribute>, true) with
                | [| :? JsonPropertyNameAttribute as name |] -> name.Name
                | _ -> p.Name
            let ignore =
                p.GetCustomAttributes(typeof<JsonIgnoreAttribute>, true)
                |> Array.isEmpty
                |> not
            { Name = name; Type = p.PropertyType; Ignore = ignore }
        )

    let fieldCount = fieldProps.Length
    let expectedFieldCount =
        fieldProps
        |> Seq.filter (fun p -> not p.Ignore)
        |> Seq.length

    let ctor = FSharpValue.PreComputeRecordConstructor(typeof<'T>, true)

    let dector = FSharpValue.PreComputeRecordReader(typeof<'T>, true)

    let fieldIndex (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < fieldCount do
            let p = fieldProps.[i]
            if reader.ValueTextEquals(p.Name) then
                found <- ValueSome (struct (i, p))
            else
                i <- i + 1
        found

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader typeToConvert

        let fields = Array.zeroCreate fieldCount
        let mutable cont = true
        let mutable fieldsFound = 0
        while cont && reader.Read() do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                match fieldIndex &reader with
                | ValueSome (i, p) when not p.Ignore ->
                    fieldsFound <- fieldsFound + 1
                    fields.[i] <- JsonSerializer.Deserialize(&reader, p.Type, options)
                | _ ->
                    reader.Skip()
            | _ -> ()

        if fieldsFound < expectedFieldCount && not options.IgnoreNullValues then
            raise (JsonException("Missing field for record type " + typeToConvert.FullName))
        ctor fields :?> 'T

    override _.Write(writer, value, options) =
        writer.WriteStartObject()
        (fieldProps, dector value)
        ||> Array.iter2 (fun p v ->
            if not p.Ignore && not (options.IgnoreNullValues && isNull v) then
                writer.WritePropertyName(p.Name)
                JsonSerializer.Serialize(writer, v, options))
        writer.WriteEndObject()

type JsonRecordConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert) =
        TypeCache.isRecord typeToConvert

    static member internal CreateConverter(typeToConvert) =
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonRecordConverter.CreateConverter(typeToConvert)
