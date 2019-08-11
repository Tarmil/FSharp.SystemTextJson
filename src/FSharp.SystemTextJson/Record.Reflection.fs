namespace System.Text.Json.Serialization

open System
open System.Reflection
open FSharp.Reflection
open System.Reflection.Emit
open System.Text.Json

type internal Serializer = delegate of Utf8JsonWriter * obj * JsonSerializerOptions -> unit
type internal Deserializer = delegate of byref<Utf8JsonReader> * obj * JsonSerializerOptions -> unit

type internal RecordField<'Record> =
    {
        Name: string
        Type: Type
        Ignore: bool
        Serialize: Serializer
        Deserialize: Deserializer
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

    let private deserializer<'Field> (f: FieldInfo) =
        let setter =
            let dynMethod =
                new DynamicMethod(
                    f.Name,
                    typeof<Void>,
                    [| typeof<obj>; f.FieldType |],
                    typedefof<RecordField<_>>.Module,
                    skipVisibility = true
                )
            let gen = dynMethod.GetILGenerator()
            gen.Emit(OpCodes.Ldarg_0)
            if f.DeclaringType.IsValueType then
                gen.Emit(OpCodes.Unbox, f.DeclaringType)
            gen.Emit(OpCodes.Ldarg_1)
            gen.Emit(OpCodes.Stfld, f)
            gen.Emit(OpCodes.Ret)
            dynMethod.CreateDelegate(typeof<Action<obj, 'Field>>) :?> Action<obj, 'Field>
        Deserializer(fun reader record options ->
            let value = JsonSerializer.Deserialize<'Field>(&reader, options)
            setter.Invoke(record, value))

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
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(null, [|f|])
                    :?> Deserializer
            {
                Name = name p
                Type = p.PropertyType
                Ignore = isIgnore p
                Serialize = serializer
                Deserialize = deserializer
            } : RecordField<'Record>
        )
