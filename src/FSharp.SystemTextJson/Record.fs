namespace System.Text.Json.Serialization

open System
open System.Text.Json
open FSharp.Reflection

type internal RecordProperty =
    {
        Name: string
        Type: Type
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
            { Name = name; Type = p.PropertyType }
        )

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
                | ValueNone ->
                    raise (JsonException("Unknow field for record type " + typeToConvert.FullName + ": " + reader.GetString()))
                | ValueSome (i, p) ->
                    fieldsFound <- fieldsFound + 1
                    fields.[i] <- JsonSerializer.Deserialize(&reader, p.Type, options)
            | _ -> ()

        if fieldsFound < fields.Length then
            raise (JsonException("Missing field for record type " + typeToConvert.FullName))
        ctor fields :?> 'T

    override __.Write(writer, value, options) =
        writer.WriteStartObject()
        (fieldProps, dector value)
        ||> Array.iter2 (fun p v ->
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
