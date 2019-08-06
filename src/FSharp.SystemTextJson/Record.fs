namespace System.Text.Json.Serialization

open System
open System.Text.Json
open FSharp.Reflection

type internal RecordProperty =
    {
        Name: string
        Type: Type
        Ignore: bool
    }

type JsonRecordConverter<'T>() =
    inherit JsonConverter<'T>()

    static let fieldProps =
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

    static let expectedFieldCount =
        fieldProps
        |> Seq.filter (fun p -> not p.Ignore)
        |> Seq.length

    static let ctor = FSharpValue.PreComputeRecordConstructor(typeof<'T>, true)

    static let dector = FSharpValue.PreComputeRecordReader(typeof<'T>, true)

    static let fieldIndex (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < fieldProps.Length do
            let p = fieldProps.[i]
            if reader.ValueTextEquals(p.Name.AsSpan()) then
                found <- ValueSome (struct (i, p))
            else
                i <- i + 1
        found

    override __.Read(reader, typeToConvert, options) =
        if reader.TokenType <> JsonTokenType.StartObject then
            raise (JsonException("Failed to parse record type " + typeToConvert.FullName + ", expected JSON object, found " + string reader.TokenType))

        let fields = Array.zeroCreate fieldProps.Length
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

        if fieldsFound < expectedFieldCount then
            raise (JsonException("Missing field for record type " + typeToConvert.FullName))
        ctor fields :?> 'T

    override __.Write(writer, value, options) =
        writer.WriteStartObject()
        (fieldProps, dector value)
        ||> Array.iter2 (fun p v ->
            if not p.Ignore then
                writer.WritePropertyName(p.Name)
                JsonSerializer.Serialize(writer, v, options))
        writer.WriteEndObject()

type JsonRecordConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert) =
        FSharpType.IsRecord(typeToConvert, true)

    static member internal CreateConverter(typeToConvert) =
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override __.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override __.CreateConverter(typeToConvert) =
        JsonRecordConverter.CreateConverter(typeToConvert)
