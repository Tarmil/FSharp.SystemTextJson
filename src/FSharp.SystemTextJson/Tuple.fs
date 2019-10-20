namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type JsonTupleConverter<'T>() =
    inherit JsonConverter<'T>()

    let ty = typeof<'T>
    let types = FSharpType.GetTupleElements(ty)
    let ctor = FSharpValue.PreComputeTupleConstructor(ty)
    let reader = FSharpValue.PreComputeTupleReader(ty)

    override _.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "array" &reader typeToConvert
        let elts = Array.zeroCreate types.Length
        for i in 0..types.Length-1 do
            reader.Read() |> ignore
            elts.[i] <- JsonSerializer.Deserialize(&reader, types.[i], options)
        readExpecting JsonTokenType.EndArray "end of array" &reader typeToConvert
        ctor elts :?> 'T

    override _.Write(writer, value, options) =
        writer.WriteStartArray()
        for value in reader value do
            JsonSerializer.Serialize(writer, value, options)
        writer.WriteEndArray()

type JsonTupleConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isTuple typeToConvert

    static member internal CreateConverter(typeToConvert: Type) =
        typedefof<JsonTupleConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonTupleConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonTupleConverter.CreateConverter(typeToConvert)
