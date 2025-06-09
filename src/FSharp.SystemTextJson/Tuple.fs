namespace System.Text.Json.Serialization

open System
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization.Helpers
open FSharp.Reflection

type internal TupleProperty<'T> =
    { Type: Type
      NeedsNullChecking: bool }

    static member MakeUntyped fsOptions t =
        let needsNullChecking =
            let tIsNullable = isNullableFieldType fsOptions t
            not tIsNullable && not t.IsValueType
        { Type = t; NeedsNullChecking = needsNullChecking }

    static member MakeTyped fsOptions =
        TupleProperty<'T>.MakeUntyped fsOptions typeof<'T>

    member this.CheckNull(x: 'T) =
        if this.NeedsNullChecking && isNull (box x) then
            failf "Unexpected null inside tuple-array. Expected type %s, but got null." this.Type.Name

    member this.ReadAndDeserializeTyped(reader: Utf8JsonReader byref, options: JsonSerializerOptions) =
        reader.Read() |> ignore
        let res = JsonSerializer.Deserialize<'T>(&reader, options)
        this.CheckNull(res)
        res

[<AbstractClass>]
type BaseJsonTupleConverter<'T>() =
    inherit JsonConverter<'T>()

    abstract ReadCore: Utf8JsonReader byref * JsonSerializerOptions -> 'T
    abstract WriteCore: Utf8JsonWriter * 'T * JsonSerializerOptions -> unit

    override this.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartArray "array" &reader typeToConvert
        let res = this.ReadCore(&reader, options)
        readExpecting JsonTokenType.EndArray "end of array" &reader typeToConvert
        res

    override this.Write(writer, value, options) =
        writer.WriteStartArray()
        this.WriteCore(writer, value, options)
        writer.WriteEndArray()

    override _.HandleNull = true

type JsonTupleConverter<'T>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<'T>()

    let ty = typeof<'T>
    let fieldProps =
        FSharpType.GetTupleElements(ty)
        |> Array.map (TupleProperty<obj>.MakeUntyped fsOptions.Record)
    let ctor = FSharpValue.PreComputeTupleConstructor(ty)
    let reader = FSharpValue.PreComputeTupleReader(ty)

    override _.ReadCore(reader, options) =
        let elts = Array.zeroCreate fieldProps.Length
        for i in 0 .. fieldProps.Length - 1 do
            let p = fieldProps[i]
            reader.Read() |> ignore
            let value = JsonSerializer.Deserialize(&reader, p.Type, options)
            p.CheckNull value
            elts[i] <- value
        ctor elts :?> 'T

    override _.WriteCore(writer, value, options) =
        let values = reader value
        for i in 0 .. fieldProps.Length - 1 do
            JsonSerializer.Serialize(writer, values[i], fieldProps[i].Type, options)

type JsonTupleConverter<'T1, 'T2>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<'T1 * 'T2>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options), p2.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)

type JsonStructTupleConverter<'T1, 'T2>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<struct ('T1 * 'T2)>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options), p2.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)

type JsonTupleConverter<'T1, 'T2, 'T3>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<'T1 * 'T2 * 'T3>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record
    let p3 = TupleProperty<'T3>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options),
         p2.ReadAndDeserializeTyped(&reader, options),
         p3.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2, x3), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)
        JsonSerializer.Serialize(writer, x3, options)

type JsonStructTupleConverter<'T1, 'T2, 'T3>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<struct ('T1 * 'T2 * 'T3)>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record
    let p3 = TupleProperty<'T3>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options),
         p2.ReadAndDeserializeTyped(&reader, options),
         p3.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2, x3), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)
        JsonSerializer.Serialize(writer, x3, options)

type JsonTupleConverter<'T1, 'T2, 'T3, 'T4>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<'T1 * 'T2 * 'T3 * 'T4>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record
    let p3 = TupleProperty<'T3>.MakeTyped fsOptions.Record
    let p4 = TupleProperty<'T4>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options),
         p2.ReadAndDeserializeTyped(&reader, options),
         p3.ReadAndDeserializeTyped(&reader, options),
         p4.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2, x3, x4), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)
        JsonSerializer.Serialize(writer, x3, options)
        JsonSerializer.Serialize(writer, x4, options)

type JsonStructTupleConverter<'T1, 'T2, 'T3, 'T4>(fsOptions: JsonFSharpOptions) =
    inherit BaseJsonTupleConverter<struct ('T1 * 'T2 * 'T3 * 'T4)>()

    let p1 = TupleProperty<'T1>.MakeTyped fsOptions.Record
    let p2 = TupleProperty<'T2>.MakeTyped fsOptions.Record
    let p3 = TupleProperty<'T3>.MakeTyped fsOptions.Record
    let p4 = TupleProperty<'T4>.MakeTyped fsOptions.Record

    override this.ReadCore(reader, options) =
        (p1.ReadAndDeserializeTyped(&reader, options),
         p2.ReadAndDeserializeTyped(&reader, options),
         p3.ReadAndDeserializeTyped(&reader, options),
         p4.ReadAndDeserializeTyped(&reader, options))

    override this.WriteCore(writer, (x1, x2, x3, x4), options) =
        JsonSerializer.Serialize(writer, x1, options)
        JsonSerializer.Serialize(writer, x2, options)
        JsonSerializer.Serialize(writer, x3, options)
        JsonSerializer.Serialize(writer, x4, options)


type JsonTupleConverter(fsOptions, [<Optional>] forceGeneric) =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert: Type) =
        TypeCache.isTuple typeToConvert

    static member internal CreateConverter
        (typeToConvert: Type, fsOptions: JsonFSharpOptions, [<Optional>] forceGeneric: bool)
        =
        let converterType =
            if forceGeneric then
                typedefof<JsonTupleConverter<_>>.MakeGenericType(typeToConvert)
            else
                let targs = typeToConvert.GetGenericArguments()
                let isStruct =
                    typeToConvert.GetGenericTypeDefinition().Name.StartsWith("ValueTuple")
                match targs.Length, isStruct with
                | 2, false -> typedefof<JsonTupleConverter<_, _>>.MakeGenericType(targs)
                | 3, false -> typedefof<JsonTupleConverter<_, _, _>>.MakeGenericType(targs)
                | 4, false -> typedefof<JsonTupleConverter<_, _, _, _>>.MakeGenericType(targs)
                | 2, true -> typedefof<JsonStructTupleConverter<_, _>>.MakeGenericType(targs)
                | 3, true -> typedefof<JsonStructTupleConverter<_, _, _>>.MakeGenericType(targs)
                | 4, true -> typedefof<JsonStructTupleConverter<_, _, _, _>>.MakeGenericType(targs)
                | _ -> typedefof<JsonTupleConverter<_>>.MakeGenericType(typeToConvert)
        converterType.GetConstructor([| typeof<JsonFSharpOptions> |]).Invoke([| fsOptions |]) :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonTupleConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, _options) =
        JsonTupleConverter.CreateConverter(typeToConvert, fsOptions, forceGeneric)
