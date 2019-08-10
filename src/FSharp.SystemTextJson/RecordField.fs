namespace System.Text.Json.Serialization

open System
open System.Reflection
open FSharp.Reflection
open System.Reflection.Emit
open System.Text.Json

type internal Serializer<'Record> = delegate of Utf8JsonWriter * 'Record * JsonSerializerOptions -> unit
type internal FieldReader<'Record, 'Field> = delegate of 'Record -> 'Field

type internal RecordField<'Record> =
    {
        Name: string
        Type: Type
        Ignore: bool
        Serialize: Serializer<'Record>
    }

    static member name (p: PropertyInfo) =
        match p.GetCustomAttributes(typeof<JsonPropertyNameAttribute>, true) with
        | [| :? JsonPropertyNameAttribute as name |] -> name.Name
        | _ -> p.Name

    static member isIgnore (p: PropertyInfo) =
        p.GetCustomAttributes(typeof<JsonIgnoreAttribute>, true)
        |> Array.isEmpty
        |> not

    static member serializer<'Field> (f: FieldInfo) =
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
            gen.Emit(OpCodes.Ldfld, f)
            gen.Emit(OpCodes.Ret)
            dynMethod.CreateDelegate(typeof<FieldReader<'Record, 'Field>>) :?> FieldReader<'Record, 'Field>
        Serializer<'Record>(fun writer r options ->
            let v = getter.Invoke(r)
            JsonSerializer.Serialize<'Field>(writer, v, options)
        )

    static member properties () =
        let recordTy = typeof<'Record>
        let fields = recordTy.GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic)
        let props = FSharpType.GetRecordFields(recordTy, true)
        (fields, props)
        ||> Array.map2 (fun f p ->
            let serializer =
                typeof<RecordField<'Record>>.GetMethod("serializer", BindingFlags.Static ||| BindingFlags.NonPublic)
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(null, [|f|])
                    :?> Serializer<'Record>
            {
                Name = RecordField<'Record>.name p
                Type = p.PropertyType
                Ignore = RecordField<'Record>.isIgnore p
                Serialize = serializer
            } : RecordField<'Record>
        )
