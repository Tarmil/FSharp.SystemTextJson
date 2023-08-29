namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Reflection
open System.Text.Json
open FSharp.Reflection
open System.Text.Json.Serialization.Helpers

type private RecordField
    (
        fsOptions: JsonFSharpOptionsRecord,
        options: JsonSerializerOptions,
        i: int,
        p: PropertyInfo,
        fieldOrderIndices: int[] voption,
        names: string[]
    ) =
    inherit FieldHelper
        (
            options,
            fsOptions,
            p.PropertyType,
            sprintf
                "%s.%s was expected to be of type %s, but was null."
                p.DeclaringType.Name
                names[0]
                p.PropertyType.Name
        )

    let ignore =
        p.GetCustomAttributes(typeof<JsonIgnoreAttribute>, true) |> Array.isEmpty |> not

    let read =
        let m = p.GetGetMethod()
        fun o -> m.Invoke(o, Array.empty)

    new(fsOptions, options: JsonSerializerOptions, i, p: PropertyInfo, fieldOrderIndices) =
        let names =
            match getJsonNames "field" (fun ty -> p.GetCustomAttributes(ty, true)) with
            | ValueSome names -> names |> Array.map (fun n -> n.AsString())
            | ValueNone -> [| convertName options.PropertyNamingPolicy p.Name |]
        RecordField(fsOptions, options, i, p, fieldOrderIndices, names)

    member _.Names = names

    member _.Type = p.PropertyType

    member _.Ignore = ignore

    member this.MustBePresent = not (ignore || this.CanBeSkipped)

    member _.Read(value: obj) =
        read value

    member _.WriteOrder =
        match fieldOrderIndices with
        | ValueSome a -> a[i]
        | ValueNone -> i

type internal IRecordConverter =
    abstract ReadRestOfObject: byref<Utf8JsonReader> * skipFirstRead: bool -> obj
    abstract WriteRestOfObject: Utf8JsonWriter * obj * JsonSerializerOptions -> unit
    abstract FieldNames: string[]

type JsonRecordConverter<'T> internal (options: JsonSerializerOptions, fsOptions: JsonFSharpOptionsRecord) =
    inherit JsonConverter<'T>()

    let recordType: Type = typeof<'T>

    let fields = FSharpType.GetRecordFields(recordType, true)

    let allProperties =
        let allPublic =
            recordType.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
        let all =
            if fields.Length = 0 || fields[0].GetGetMethod(true).IsPublic then
                allPublic
            else
                Array.append fields allPublic
        if fsOptions.IncludeRecordProperties then
            all
        else
            all
            |> Array.filter (fun p ->
                Array.contains p fields
                || (p.GetCustomAttributes(typeof<JsonIncludeAttribute>, true) |> Seq.isEmpty |> not)
            )

    let fieldOrderIndices =
        let revIndices =
            allProperties
            |> Array.mapi (fun i field ->
                let revI =
                    match field.GetCustomAttributes(typeof<JsonPropertyOrderAttribute>, true) with
                    | [| :? JsonPropertyOrderAttribute as attr |] -> attr.Order
                    | _ -> 0
                struct (revI, i)
            )
        if revIndices |> Array.exists (fun struct (revI, _) -> revI <> 0) then
            // Using Seq.sort rather than Array.sort because it is stable
            let revIndices =
                revIndices |> Seq.sortBy (fun struct (revI, _) -> revI) |> Array.ofSeq
            let res = Array.zeroCreate allProperties.Length
            for i in 0 .. res.Length - 1 do
                let struct (_, x) = revIndices[i]
                res[x] <- i
            ValueSome res
        else
            ValueNone

    let allProps =
        allProperties
        |> Array.mapi (fun i p -> RecordField(fsOptions, options, i, p, fieldOrderIndices))

    let fieldProps =
        if fsOptions.IncludeRecordProperties then
            allProps |> Array.take fields.Length
        else
            allProps

    let writeOrderedFieldProps =
        let a = Array.mapi (fun i x -> struct (i, x)) allProps
        a |> Array.sortInPlaceBy (fun struct (_, x) -> x.WriteOrder)
        a

    let fieldCount = fields.Length
    let minExpectedFieldCount =
        fieldProps |> Seq.filter (fun p -> p.MustBePresent) |> Seq.length

    let ctor = FSharpValue.PreComputeRecordConstructor(recordType, true)

    let dector = FSharpValue.PreComputeRecordReader(recordType, true)

    let defaultFields =
        let arr = Array.zeroCreate fieldCount
        fieldProps
        |> Array.iteri (fun i field ->
            match field.DefaultValue with
            | ValueSome v -> arr[i] <- v
            | ValueNone -> ()
        )
        arr

    let propertiesByName =
        if options.PropertyNameCaseInsensitive then
            let d = Dictionary(StringComparer.OrdinalIgnoreCase)
            fieldProps
            |> Array.iteri (fun i f ->
                if not f.Ignore then
                    for name in f.Names do
                        d[name] <- struct (i, f)
            )
            ValueSome d
        else
            ValueNone

    let fieldIndex (reader: byref<Utf8JsonReader>) =
        match propertiesByName with
        | ValueNone ->
            let mutable found = ValueNone
            let mutable i = 0
            while found.IsNone && i < fieldCount do
                let p = fieldProps[i]
                let mutable j = 0
                while found.IsNone && j < p.Names.Length do
                    if reader.ValueTextEquals(p.Names[j]) then
                        found <- ValueSome(struct (i, p))
                    else
                        j <- j + 1
                i <- i + 1
            found
        | ValueSome d ->
            match d.TryGetValue(reader.GetString()) with
            | true, p -> ValueSome p
            | false, _ -> ValueNone

    override this.Read(reader, typeToConvert, options) =
        expectAlreadyRead JsonTokenType.StartObject "JSON object" &reader typeToConvert
        this.ReadRestOfObject(&reader, false)

    member internal _.ReadRestOfObject(reader, skipFirstRead) =
        let fields = Array.copy defaultFields
        let mutable cont = true
        let mutable requiredFieldCount = 0
        let mutable skipRead = skipFirstRead
        while cont && (skipRead || reader.Read()) do
            match reader.TokenType with
            | JsonTokenType.EndObject -> cont <- false
            | JsonTokenType.PropertyName ->
                skipRead <- false
                match fieldIndex &reader with
                | ValueSome (i, p) when not p.Ignore ->
                    if p.MustBePresent then requiredFieldCount <- requiredFieldCount + 1
                    reader.Read() |> ignore
                    fields[i] <- p.Deserialize(&reader)
                | _ -> reader.Skip()
            | _ -> ()

        if requiredFieldCount < minExpectedFieldCount then
            for i in 0 .. fieldCount - 1 do
                if fields[i] = defaultFields[i] && fieldProps[i].MustBePresent then
                    failf "Missing field for record type %s: %s" recordType.FullName fieldProps[i].Names[0]

        ctor fields :?> 'T

    override this.Write(writer, value, options) =
        if isNull (box value) then
            JsonSerializer.Serialize(writer, null, options)
        else
            writer.WriteStartObject()
            this.WriteRestOfObject(writer, value, options)

    override this.HandleNull = true

    member internal _.WriteRestOfObject(writer, value, options) =
        let values = dector value
        for struct (i, p) in writeOrderedFieldProps do
            let v = if i < fieldCount then values[i] else p.Read value
            if not (p.Ignore || p.IgnoreOnWrite v) then
                writer.WritePropertyName(p.Names[0])
                JsonSerializer.Serialize(writer, v, p.Type, options)
        writer.WriteEndObject()

    interface IRecordConverter with
        member this.ReadRestOfObject(reader, skipFirstRead) =
            box (this.ReadRestOfObject(&reader, skipFirstRead))
        member this.WriteRestOfObject(writer, value, options) =
            this.WriteRestOfObject(writer, unbox value, options)
        member _.FieldNames = fieldProps |> Array.collect (fun p -> p.Names)

    new(options, fsOptions: JsonFSharpOptions) = JsonRecordConverter<'T>(options, fsOptions.Record)

type JsonRecordConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    new() = JsonRecordConverter(JsonFSharpOptions())

    static member internal CanConvert(typeToConvert) =
        TypeCache.isRecord typeToConvert

    static member internal CreateConverter
        (
            typeToConvert: Type,
            options: JsonSerializerOptions,
            fsOptions: JsonFSharpOptions
        ) =
        let fsOptions = overrideOptions typeToConvert fsOptions
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([| typeToConvert |])
            .GetConstructor(
                BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance,
                null,
                [| typeof<JsonSerializerOptions>
                   typeof<JsonFSharpOptionsRecord> |],
                null
            )
            .Invoke([| options; fsOptions.Record |])
        :?> JsonConverter

    override _.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override _.CreateConverter(typeToConvert, options) =
        JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions)
