namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type JsonListConverter<'T>(fsOptions) =
    inherit JsonConverter<list<'T>>()
    let tType = typeof<'T>
    let tIsNullable = isNullableFieldType fsOptions tType
    let needsNullChecking = not tIsNullable && not tType.IsValueType

    override _.Read(reader, _typeToConvert, options) =
        let array = JsonSerializer.Deserialize(&reader, typeof<'T[]>, options) :?> 'T[]
        if needsNullChecking then
            for elem in array do
                if isNull (box elem) then
                    let msg = sprintf "Unexpected null inside array. Expected only elements of type %s" tType.Name
                    raise (JsonException msg)
        array |> List.ofArray

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize(writer, value, typeof<seq<'T>>, options)

type JsonListConverter(fsOptions) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isList typeToConvert

    static member internal CreateConverter(typeToConvert: Type, fsOptions: JsonFSharpOptions) =
        typedefof<JsonListConverter<_>>
            .MakeGenericType([|typeToConvert.GetGenericArguments().[0]|])
            .GetConstructor([|typeof<JsonFSharpOptions>|])
            .Invoke([|fsOptions|])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonListConverter.CreateConverter(typeToConvert, fsOptions)

type JsonSetConverter<'T when 'T : comparison>(fsOptions) =
    inherit JsonConverter<Set<'T>>()
    let tType = typeof<'T>
    let tIsNullable = isNullableFieldType fsOptions tType
    let needsNullChecking = not tIsNullable && not tType.IsValueType

    let rec read (acc: Set<'T>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndArray -> acc
        | _ ->
            let elt = JsonSerializer.Deserialize(&reader, typeof<'T>, options) :?> 'T
            read (Set.add elt acc) &reader options

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "JSON array" &reader typeToConvert
        let set = read Set.empty &reader options
        if needsNullChecking then
            for elem in set do
                if isNull (box elem) then
                    let msg = sprintf "Unexpected null inside set. Expected only elements of type %s" tType.Name
                    raise (JsonException msg)
        set

    override _.Write(writer, value, options) =
        JsonSerializer.Serialize(writer, value, typeof<seq<'T>>, options)

type JsonSetConverter(fsOptions) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isSet typeToConvert

    static member internal CreateConverter(typeToConvert: Type, fsOptions: JsonFSharpOptions) =
        typedefof<JsonSetConverter<_>>
            .MakeGenericType([|typeToConvert.GetGenericArguments().[0]|])
            .GetConstructor([|typeof<JsonFSharpOptions>|])
            .Invoke([|fsOptions|])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonSetConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonSetConverter.CreateConverter(typeToConvert, fsOptions)

type JsonStringMapConverter<'V>() =
    inherit JsonConverter<Map<string, 'V>>()

    let ty = typeof<Map<string, 'V>>

    let rec read (acc: Map<string, 'V>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndObject -> acc
        | JsonTokenType.PropertyName ->
            let key = reader.GetString()
            let value = JsonSerializer.Deserialize(&reader, typeof<'V>, options) :?> 'V
            read (Map.add key value acc) &reader options
        | _ ->
            fail "JSON field" &reader ty

    override _.Read(reader, _typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader ty
        read Map.empty &reader options

    override _.Write(writer, value, options) =
        writer.WriteStartObject()
        for kv in value do
            let k =
                match options.DictionaryKeyPolicy with
                | null -> kv.Key
                | p -> p.ConvertName kv.Key
            writer.WritePropertyName(k)
            JsonSerializer.Serialize(writer, kv.Value, typeof<'V>, options)
        writer.WriteEndObject()

type JsonWrappedStringMapConverter<'K, 'V when 'K : comparison>() =
    inherit JsonConverter<Map<'K, 'V>>()

    let ty = typeof<Map<'K, 'V>>
    let kty = typeof<'K>

    let case = FSharpType.GetUnionCases(kty, true).[0]
    let wrap = FSharpValue.PreComputeUnionConstructor(case, true)
    let unwrap = FSharpValue.PreComputeUnionReader(case, true)

    let rec read (acc: Map<'K, 'V>) (reader: byref<Utf8JsonReader>) options =
        if not (reader.Read()) then acc else
        match reader.TokenType with
        | JsonTokenType.EndObject -> acc
        | JsonTokenType.PropertyName ->
            let key = reader.GetString()
            let value = JsonSerializer.Deserialize(&reader, typeof<'V>, options) :?> 'V
            read (Map.add (wrap [|key|] :?> 'K) value acc) &reader options
        | _ ->
            fail "JSON field" &reader ty

    override _.Read(reader, _typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader ty
        read Map.empty &reader options

    override _.Write(writer, value: Map<'K, 'V>, options) =
        writer.WriteStartObject()
        for kv in value do
            let k =
                let k = (unwrap kv.Key).[0] :?> string
                match options.DictionaryKeyPolicy with
                | null -> k
                | p -> p.ConvertName k
            writer.WritePropertyName(k)
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
            let key = JsonSerializer.Deserialize(&reader, typeof<'K>, options) :?> 'K
            reader.Read() |> ignore
            let value = JsonSerializer.Deserialize(&reader, typeof<'V>, options) :?> 'V
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
            JsonSerializer.Serialize(writer, kv.Key, typeof<'K>, options)
            JsonSerializer.Serialize(writer, kv.Value, typeof<'V>, options)
            writer.WriteEndArray()
        writer.WriteEndArray()

type JsonMapConverter() =
    inherit JsonConverterFactory()

    static let isWrappedString (ty: Type) =
        TypeCache.isUnion ty &&
        let cases = FSharpType.GetUnionCases(ty, true)
        cases.Length = 1 &&
        let fields = cases.[0].GetFields()
        fields.Length = 1 &&
        fields.[0].PropertyType = typeof<string>

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isMap typeToConvert

    static member internal CreateConverter(typeToConvert: Type) =
        let genArgs = typeToConvert.GetGenericArguments()
        let ty =
            if genArgs.[0] = typeof<string> then
                typedefof<JsonStringMapConverter<_>>
                    .MakeGenericType([|genArgs.[1]|])
            elif isWrappedString genArgs.[0] then
                typedefof<JsonWrappedStringMapConverter<_,_>>
                    .MakeGenericType(genArgs)
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
