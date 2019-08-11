namespace System.Text.Json.Serialization

open System
open System.Reflection
open FSharp.Reflection
open System.Reflection.Emit
open System.Text.Json

type internal Serializer = Action<Utf8JsonWriter, obj, JsonSerializerOptions>

type internal RefobjFieldSetter<'Record, 'Field> = Action<'Record, 'Field>
type internal StructFieldSetter<'Record, 'Field> = delegate of byref<'Record> * 'Field -> unit

type internal RefobjDeserializer<'Record> = delegate of byref<Utf8JsonReader> * 'Record * JsonSerializerOptions -> unit
type internal StructDeserializer<'Record> = delegate of byref<Utf8JsonReader> * byref<'Record> * JsonSerializerOptions -> unit

[<Struct>]
type internal Deserializer<'Record> =
    | DStruct of s: StructDeserializer<'Record>
    | DRefobj of n: RefobjDeserializer<'Record>

type internal RecordField<'Record> =
    {
        Name: string
        Type: Type
        Ignore: bool
        Serialize: Serializer
        Deserialize: Deserializer<'Record>
    }

module internal RecordReflection =

    let private name (p: PropertyInfo) =
        match p.GetCustomAttributes(typeof<JsonPropertyNameAttribute>, true) with
        | [| :? JsonPropertyNameAttribute as name |] -> name.Name
        | _ -> p.Name

    let private isIgnore (p: PropertyInfo) =
        p.GetCustomAttributes(typeof<JsonIgnoreAttribute>, true)
        |> Array.isEmpty
        |> not

    let private deserializer<'Record, 'Field> (f: FieldInfo) =
        let setter =
            let dynMethod =
                new DynamicMethod(
                    f.Name,
                    typeof<Void>,
                    [|
                        (if f.DeclaringType.IsValueType
                            then typeof<'Record>.MakeByRefType()
                            else typeof<'Record>)
                        f.FieldType
                    |],
                    typedefof<RecordField<_>>.Module,
                    skipVisibility = true
                )
            let gen = dynMethod.GetILGenerator()
            gen.Emit(OpCodes.Ldarg_0)
            gen.Emit(OpCodes.Ldarg_1)
            gen.Emit(OpCodes.Stfld, f)
            gen.Emit(OpCodes.Ret)
            dynMethod
        if f.DeclaringType.IsValueType then
            let setter = setter.CreateDelegate(typeof<StructFieldSetter<'Record, 'Field>>) :?> StructFieldSetter<'Record, 'Field>
            StructDeserializer<'Record>(fun reader record options ->
                let value = JsonSerializer.Deserialize<'Field>(&reader, options)
                setter.Invoke(&record, value))
            |> DStruct
        else
            let setter = setter.CreateDelegate(typeof<RefobjFieldSetter<'Record, 'Field>>) :?> RefobjFieldSetter<'Record, 'Field>
            RefobjDeserializer<'Record>(fun reader record options ->
                let value = JsonSerializer.Deserialize<'Field>(&reader, options)
                setter.Invoke(record, value))
            |> DRefobj

    let private serializer<'Field> (f: FieldInfo) =
        let getter =
            let dynMethod =
                new DynamicMethod(
                    f.Name,
                    f.FieldType,
                    [| typeof<obj> |],
                    typedefof<RecordField<_>>.Module,
                    skipVisibility = true
                )
            let gen = dynMethod.GetILGenerator()
            gen.Emit(OpCodes.Ldarg_0)
            if f.DeclaringType.IsValueType then
                gen.Emit(OpCodes.Unbox, f.DeclaringType)
            gen.Emit(OpCodes.Ldfld, f)
            gen.Emit(OpCodes.Ret)
            dynMethod.CreateDelegate(typeof<Func<obj, 'Field>>) :?> Func<obj, 'Field>
        Serializer(fun writer record options ->
            let v = getter.Invoke(record)
            JsonSerializer.Serialize<'Field>(writer, v, options)
        )

    let private thisModule = typedefof<RecordField<_>>.Assembly.GetType("System.Text.Json.Serialization.RecordReflection")

    let fields<'Record> () =
        let recordTy = typeof<'Record>
        let fields = recordTy.GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic)
        let props = FSharpType.GetRecordFields(recordTy, true)
        (fields, props)
        ||> Array.map2 (fun f p ->
            let serializer =
                thisModule.GetMethod("serializer", BindingFlags.Static ||| BindingFlags.NonPublic)
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(null, [|f|])
                    :?> Serializer
            let deserializer =
                thisModule.GetMethod("deserializer", BindingFlags.Static ||| BindingFlags.NonPublic)
                    .MakeGenericMethod(recordTy, p.PropertyType)
                    .Invoke(null, [|f|])
                    :?> Deserializer<'Record>
            {
                Name = name p
                Type = p.PropertyType
                Ignore = isIgnore p
                Serialize = serializer
                Deserialize = deserializer
            } : RecordField<'Record>
        )
