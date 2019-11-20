namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization.Helpers

type JsonListConverter<'T>() =
    inherit JsonConverter<list<'T>>()

    override _.Read(reader, _typeToConvert, options) =
        JsonSerializer.Deserialize<'T[]>(&reader, options)
        |> List.ofArray

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize<seq<'T>>(writer, value, options)

type JsonListConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isList typeToConvert

    static member internal CreateConverter(typeToConvert: Type) =
        typedefof<JsonListConverter<_>>
            .MakeGenericType([|typeToConvert.GetGenericArguments().[0]|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonListConverter.CreateConverter(typeToConvert)

type JsonSetConverter<'T when 'T : comparison>() =
    inherit JsonConverter<Set<'T>>()

    let rec read (acc: Set<'T>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndArray -> acc
        | _ ->
            let elt = JsonSerializer.Deserialize<'T>(&reader, options)
            read (Set.add elt acc) &reader options

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "JSON array" &reader typeToConvert
        read Set.empty &reader options

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize<seq<'T>>(writer, value, options)

type JsonSetConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isSet typeToConvert

    static member internal CreateConverter(typeToConvert: Type) =
        typedefof<JsonSetConverter<_>>
            .MakeGenericType([|typeToConvert.GetGenericArguments().[0]|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonSetConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonSetConverter.CreateConverter(typeToConvert)

type JsonStringMapConverter<'V>() =
    inherit JsonConverter<Map<string, 'V>>()

    let ty = typeof<Map<string, 'V>>

    let rec read (acc: Map<string, 'V>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndObject -> acc
        | JsonTokenType.PropertyName ->
            let key = reader.GetString()
            let value = JsonSerializer.Deserialize<'V>(&reader, options)
            read (Map.add key value acc) &reader options
        | _ ->
            fail "JSON field" &reader ty

    override _.Read(reader, _typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader ty
        read Map.empty &reader options

    override _.Write(writer, value, options) =
        writer.WriteStartObject()
        for kv in value do
            match options.DictionaryKeyPolicy with
            | null -> kv.Key
            | p -> p.ConvertName kv.Key
            |> writePropertyName writer options
            JsonSerializer.Serialize<'V>(writer, kv.Value, options)
        writer.WriteEndObject()

type JsonMapConverter<'K, 'V when 'K : comparison>() =
    inherit JsonConverter<Map<'K, 'V>>()

    let ty = typeof<Map<'K, 'V>>

    let rec read (acc: Map<'K, 'V>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndArray -> acc
        | JsonTokenType.StartArray ->
            reader.Read() |> ignore
            let key = JsonSerializer.Deserialize<'K>(&reader, options)
            reader.Read() |> ignore
            let value = JsonSerializer.Deserialize<'V>(&reader, options)
            readExpecting JsonTokenType.EndArray "JSON array" &reader ty
            read (Map.add key value acc) &reader options
        | _ ->
            fail "JSON array" &reader ty

    override _.Read(reader, _typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "JSON array" &reader ty
        read Map.empty &reader options

    override _.Write(writer, value, options) =
        writer.WriteStartArray()
        for kv in value do
            writer.WriteStartArray()
            JsonSerializer.Serialize<'K>(writer, kv.Key, options)
            JsonSerializer.Serialize<'V>(writer, kv.Value, options)
            writer.WriteEndArray()
        writer.WriteEndArray()

type JsonMapConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isMap typeToConvert

    static member internal CreateConverter(typeToConvert: Type) =
        let genArgs = typeToConvert.GetGenericArguments()
        let ty =
            if genArgs.[0] = typeof<string> then
                typedefof<JsonStringMapConverter<_>>
                    .MakeGenericType([|genArgs.[1]|])
            else
                typedefof<JsonMapConverter<_,_>>
                    .MakeGenericType(genArgs)
        ty.GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonMapConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonMapConverter.CreateConverter(typeToConvert)
