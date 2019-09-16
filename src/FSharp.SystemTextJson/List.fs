namespace System.Text.Json.Serialization

open System
open System.Text.Json

type JsonListConverter<'T>() =
    inherit JsonConverter<list<'T>>()

    override __.Read(reader, _typeToConvert, options) =
        JsonSerializer.Deserialize<'T[]>(&reader, options)
        |> List.ofArray

    override __.Write(writer, value, options) =
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

    override __.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert)

    override __.CreateConverter(typeToConvert, _options) =
        JsonListConverter.CreateConverter(typeToConvert)
