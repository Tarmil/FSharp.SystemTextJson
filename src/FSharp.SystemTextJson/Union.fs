namespace System.Text.Json.Serialization

open System
open System.Reflection
open System.Text.Json
open FSharp.Reflection

type private Case =
    {
        Info: UnionCaseInfo
        Fields: PropertyInfo[]
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
    }

type JsonUnionConverter<'T>() =
    inherit JsonConverter<'T>()

    static let cases =
        FSharpType.GetUnionCases(typeof<'T>, true)
        |> Array.map (fun uci ->
            {
                Info = uci
                Fields = uci.GetFields()
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
            })

    static let tagReader = FSharpValue.PreComputeUnionTagReader(typeof<'T>, true)

    static let usesNull =
        typeof<'T>.GetCustomAttributes(typeof<CompilationRepresentationAttribute>, false)
        |> Array.exists (fun x ->
            let x = (x :?> CompilationRepresentationAttribute)
            x.Flags.HasFlag(CompilationRepresentationFlags.UseNullAsTrueValue))

    static let caseIndex (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < cases.Length do
            let case = cases.[i]
            if reader.ValueTextEquals(case.Info.Name.AsSpan()) then
                found <- ValueSome case
            else
                i <- i + 1
        found

    static let fail expected (reader: byref<Utf8JsonReader>) =
        sprintf "Failed to parse union type %s: expected %s, found %A"
            typeof<'T>.FullName expected reader.TokenType
        |> JsonException
        |> raise

    static let readExpecting expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> expectedTokenType then
            fail expectedLabel &reader

    static let readExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> JsonTokenType.PropertyName || not (reader.ValueTextEquals expectedPropertyName) then
            fail ("\"" + expectedPropertyName + "\"") &reader

    override __.Read(reader, typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null when usesNull ->
            (null : obj) :?> 'T
        | JsonTokenType.StartObject ->
            readExpectingPropertyNamed "Case" &reader
            readExpecting JsonTokenType.String "case name" &reader
            match caseIndex &reader with
            | ValueNone ->
                raise (JsonException("Unknow case for union type " + typeToConvert.FullName + ": " + reader.GetString()))
            | ValueSome case ->
                let fieldCount = case.Fields.Length
                let fields = Array.zeroCreate fieldCount
                if case.Fields.Length > 0 then
                    readExpectingPropertyNamed "Fields" &reader
                    readExpecting JsonTokenType.StartArray "array" &reader
                    for i in 0..fieldCount-1 do
                        reader.Read() |> ignore
                        fields.[i] <- JsonSerializer.Deserialize(&reader, case.Fields.[i].PropertyType, options)
                    readExpecting JsonTokenType.EndArray "end of array" &reader
                readExpecting JsonTokenType.EndObject "end of object" &reader
                case.Ctor fields :?> 'T
        | _ ->
            fail "JSON object" &reader

    override this.Write(writer, value, options) =
        let value = box value
        if isNull value then writer.WriteNullValue() else

        let tag = tagReader value
        let case = cases.[tag]
        writer.WriteStartObject()
        writer.WriteString("Case", case.Info.Name)
        if case.Fields.Length > 0 then
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            for field in case.Dector value do
                JsonSerializer.Serialize(writer, field, options)
            writer.WriteEndArray()
        writer.WriteEndObject()

type JsonUnionConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter(typeToConvert) =
        typedefof<JsonUnionConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override this.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override this.CreateConverter(typeToConvert) =
        JsonUnionConverter.CreateConverter(typeToConvert)
