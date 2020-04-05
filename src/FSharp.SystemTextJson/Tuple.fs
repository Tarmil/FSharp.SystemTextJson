namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type JsonTupleConverter<'T>(fsOptions) =
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
            let value = JsonSerializer.Deserialize(&reader, types.[i], options)
            let tType = types.[i]
            let tIsNullable = isNullableFieldType fsOptions tType
            let needsNullChecking = not tIsNullable && not tType.IsValueType
            if needsNullChecking then
                if isNull (box value) then
                    let msg = sprintf "Unexpected null inside tuple-array. Expected type %s, but got null." tType.Name
                    raise (JsonException msg)
            elts.[i] <- value
        readExpecting JsonTokenType.EndArray "end of array" &reader typeToConvert
        ctor elts :?> 'T

    override _.Write(writer, value, options) =
        writer.WriteStartArray()
        let values = reader value
        for i in 0..types.Length-1 do
            JsonSerializer.Serialize(writer, values.[i], types.[i], options)
        writer.WriteEndArray()

type JsonTupleConverter(fsOptions) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isTuple typeToConvert

    static member internal CreateConverter(typeToConvert: Type, fsOptions: JsonFSharpOptions) =
        typedefof<JsonTupleConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([|typeof<JsonFSharpOptions>|])
            .Invoke([|fsOptions|])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonTupleConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonTupleConverter.CreateConverter(typeToConvert, fsOptions)
