namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization.Helpers

type JsonListConverter<'T> internal (fsOptions) =
    inherit JsonConverter<list<'T>>()
    let tType = typeof<'T>
    let tIsNullable = isNullableFieldType fsOptions tType
    let needsNullChecking = not tIsNullable && not tType.IsValueType

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "JSON array" &reader typeToConvert
        let array = JsonSerializer.Deserialize(&reader, typeof<'T[]>, options) :?> 'T[]
        if needsNullChecking then
            for elem in array do
                if isNull (box elem) then
                    failf "Unexpected null inside array. Expected only elements of type %s" tType.Name
        array |> List.ofArray

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize<seq<'T>>(writer, value, options)

    override _.HandleNull = true

    new(fsOptions: JsonFSharpOptions) = JsonListConverter<'T>(fsOptions.Record)

type JsonListConverter(fsOptions) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isList typeToConvert

    static member internal CreateConverter(typeToConvert: Type, fsOptions: JsonFSharpOptions) =
        typedefof<JsonListConverter<_>>
            .MakeGenericType([| typeToConvert.GetGenericArguments().[0] |])
            .GetConstructor([| typeof<JsonFSharpOptions> |])
            .Invoke([| fsOptions |])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonListConverter.CreateConverter(typeToConvert, fsOptions)

type JsonSetConverter<'T when 'T: comparison> internal (fsOptions) =
    inherit JsonConverter<Set<'T>>()
    let tType = typeof<'T>
    let tIsNullable = isNullableFieldType fsOptions tType
    let needsNullChecking = not tIsNullable && not tType.IsValueType

    let rec read (acc: Set<'T>) (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if not (reader.Read()) then
            acc
        else
            match reader.TokenType with
            | JsonTokenType.EndArray -> acc
            | _ ->
                let elt = JsonSerializer.Deserialize<'T>(&reader, options)
                read (Set.add elt acc) &reader options

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "JSON array" &reader typeToConvert
        let set = read Set.empty &reader options
        if needsNullChecking then
            for elem in set do
                if isNull (box elem) then
                    failf "Unexpected null inside set. Expected only elements of type %s" tType.Name
        set

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize<seq<'T>>(writer, value, options)

    override _.HandleNull = true

    new(fsOptions: JsonFSharpOptions) = JsonSetConverter<'T>(fsOptions.Record)

type JsonSetConverter(fsOptions) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isSet typeToConvert

    static member internal CreateConverter(typeToConvert: Type, fsOptions: JsonFSharpOptions) =
        typedefof<JsonSetConverter<_>>
            .MakeGenericType([| typeToConvert.GetGenericArguments().[0] |])
            .GetConstructor([| typeof<JsonFSharpOptions> |])
            .Invoke([| fsOptions |])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonSetConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonSetConverter.CreateConverter(typeToConvert, fsOptions)

type JsonMapObjectConverter<'K, 'V when 'K: comparison>(options: JsonSerializerOptions) =
    inherit JsonConverter<Map<'K, 'V>>()

    let ty = typeof<Map<'K, 'V>>
    let kty = typeof<'K>
    let keyConverter =
        match getConverterForDictionaryKey<'K> options with
        | null -> failf "Cannot serialize type %s as a map key with MapFormat.Object" kty.FullName
        | c -> c

    let rec read (acc: Map<'K, 'V>) (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if not (reader.Read()) then
            acc
        else
            match reader.TokenType with
            | JsonTokenType.EndObject -> acc
            | JsonTokenType.PropertyName ->
                let key = keyConverter.ReadAsPropertyName(&reader, kty, options)
                let value = JsonSerializer.Deserialize<'V>(&reader, options)
                read (Map.add key value acc) &reader options
            | _ -> failExpecting "JSON field" &reader ty

    override _.Read(reader, _typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader ty
        read Map.empty &reader options

    override _.Write(writer, value: Map<'K, 'V>, options) =
        writer.WriteStartObject()
        for kv in value do
            keyConverter.WriteAsPropertyName(writer, kv.Key, options)
            JsonSerializer.Serialize<'V>(writer, kv.Value, options)
        writer.WriteEndObject()

    override _.HandleNull = true

type JsonMapArrayOfPairsConverter<'K, 'V when 'K: comparison>() =
    inherit JsonConverter<Map<'K, 'V>>()

    let ty = typeof<Map<'K, 'V>>

    let rec read (acc: Map<'K, 'V>) (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        if not (reader.Read()) then
            acc
        else
            match reader.TokenType with
            | JsonTokenType.EndArray -> acc
            | JsonTokenType.StartArray ->
                reader.Read() |> ignore
                let key = JsonSerializer.Deserialize<'K>(&reader, options)
                reader.Read() |> ignore
                let value = JsonSerializer.Deserialize<'V>(&reader, options)
                readExpecting JsonTokenType.EndArray "JSON array" &reader ty
                read (Map.add key value acc) &reader options
            | _ -> failExpecting "JSON array" &reader ty

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

    override _.HandleNull = true

type JsonMapConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    static let jsonMapObjectConverter (genArgs: Type array) (options: JsonSerializerOptions) =
        typedefof<JsonMapObjectConverter<_, _>>
            .MakeGenericType(genArgs)
            .GetConstructor([| typeof<JsonSerializerOptions> |])
            .Invoke([| options |])
        :?> JsonConverter

    static let jsonMapArrayOfPairsConverter genArgs =
        typedefof<JsonMapArrayOfPairsConverter<_, _>>
            .MakeGenericType(genArgs)
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isMap typeToConvert

    static member internal CreateConverter
        (
            typeToConvert: Type,
            options: JsonSerializerOptions,
            fsOptions: JsonFSharpOptions
        ) =
        let genArgs = typeToConvert.GetGenericArguments()
        match fsOptions.MapFormat with
        | MapFormat.Object -> jsonMapObjectConverter genArgs options
        | MapFormat.ArrayOfPairs -> jsonMapArrayOfPairsConverter genArgs
        | MapFormat.ObjectOrArrayOfPairs ->
            if genArgs[0] = typeof<string> || isWrappedString genArgs[0] then
                jsonMapObjectConverter genArgs options
            else
                jsonMapArrayOfPairsConverter genArgs
        | format -> failf "Invalid F# Map format: %A" format

    override _.CanConvert(typeToConvert) =
        JsonMapConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonMapConverter.CreateConverter(typeToConvert, options, fsOptions)
