namespace System.Text.Json.Serialization

open System
open System.Reflection
open System.Text.Json
open FSharp.Reflection
open System.Runtime.InteropServices

type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// "Case" contains the union tag, and "Fields" contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    | AdjacentTag       = 0x00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag       = 0x00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named "Case" if NamedFields is set) is the union tag, the rest are the union fields.
    | InternalTag       = 0x00_04

    /// Encode unions as a n-valued object:
    /// the union tag is not encoded, only the union fields are.
    /// Deserialization is only possible if the fields of all cases have different names.
    | Untagged          = 0x01_08


    //// Additional options

    /// If unset, union fields are encoded as an array.
    /// If set, union fields are encoded as an object using their field names.
    | NamedFields       = 0x01_00

    /// If set, union cases that don't have fields are encoded as a bare string.
    | BareFieldlessTags = 0x02_00


    //// Specific formats

    | Default           = 0x00_01
    | NewtonsoftLike    = 0x00_01
    | ThothLike         = 0x02_04

type private Case =
    {
        Info: UnionCaseInfo
        Fields: PropertyInfo[]
        Ctor: obj[] -> obj
        Dector: obj -> obj[]
    }

type JsonUnionConverter<'T>(encoding: JsonUnionEncoding) =
    inherit JsonConverter<'T>()

    static let ty = typeof<'T>

    static let cases =
        FSharpType.GetUnionCases(ty, true)
        |> Array.map (fun uci ->
            {
                Info = uci
                Fields = uci.GetFields()
                Ctor = FSharpValue.PreComputeUnionConstructor(uci, true)
                Dector = FSharpValue.PreComputeUnionReader(uci, true)
            })

    static let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)

    static let usesNull =
        ty.GetCustomAttributes(typeof<CompilationRepresentationAttribute>, false)
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
            ty.FullName expected reader.TokenType
        |> JsonException
        |> raise

    static let readExpecting expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> expectedTokenType then
            fail expectedLabel &reader

    static let readExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) =
        if not (reader.Read()) || reader.TokenType <> JsonTokenType.PropertyName || not (reader.ValueTextEquals expectedPropertyName) then
            fail ("\"" + expectedPropertyName + "\"") &reader

    static let getCaseTag (reader: byref<Utf8JsonReader>) =
        match caseIndex &reader with
        | ValueNone ->
            raise (JsonException("Unknow case for union type " + ty.FullName + ": " + reader.GetString()))
        | ValueSome case ->
            case

    static let readFieldsAsArray (reader: byref<Utf8JsonReader>) (case: Case) (options: JsonSerializerOptions) =
        let fieldCount = case.Fields.Length
        let fields = Array.zeroCreate fieldCount
        readExpecting JsonTokenType.StartArray "array" &reader
        for i in 0..fieldCount-1 do
            reader.Read() |> ignore
            fields.[i] <- JsonSerializer.Deserialize(&reader, case.Fields.[i].PropertyType, options)
        readExpecting JsonTokenType.EndArray "end of array" &reader
        case.Ctor fields :?> 'T

    static let readAdjacentTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.StartObject ->
            readExpectingPropertyNamed "Case" &reader
            readExpecting JsonTokenType.String "case name" &reader
            let case = getCaseTag &reader
            let res =
                if case.Fields.Length > 0 then
                    readExpectingPropertyNamed "Fields" &reader
                    readFieldsAsArray &reader case options
                else
                    case.Ctor [||] :?> 'T
            readExpecting JsonTokenType.EndObject "end of object" &reader
            res
        | _ ->
            fail "JSON object" &reader

    static let readExternalTag (reader: byref<Utf8JsonReader>) (options: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.StartObject ->
            readExpecting JsonTokenType.PropertyName "case name" &reader
            let case = getCaseTag(&reader)
            let res = readFieldsAsArray &reader case options
            readExpecting JsonTokenType.EndObject "end of object" &reader
            res
        | _ ->
            fail "JSON object" &reader

    static let writeFieldsAsArray (writer: Utf8JsonWriter) (case: Case) (value: obj) (options: JsonSerializerOptions) =
        writer.WriteStartArray()
        for field in case.Dector value do
            JsonSerializer.Serialize(writer, field, options)
        writer.WriteEndArray()

    static let writeAdjacentTag (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        writer.WriteStartObject()
        writer.WriteString("Case", case.Info.Name)
        if case.Fields.Length > 0 then
            writer.WritePropertyName("Fields")
            writeFieldsAsArray writer case value options
        writer.WriteEndObject()

    static let writeExternalTag (writer: Utf8JsonWriter) (value: obj) (options: JsonSerializerOptions) =
        let tag = tagReader value
        let case = cases.[tag]
        writer.WriteStartObject()
        writer.WritePropertyName(case.Info.Name)
        writeFieldsAsArray writer case value options
        writer.WriteEndObject()

    override __.Read(reader, _typeToConvert, options) =
        match reader.TokenType with
        | JsonTokenType.Null when usesNull ->
            (null : obj) :?> 'T
        | _ ->
            if encoding.HasFlag(JsonUnionEncoding.AdjacentTag) then
                readAdjacentTag &reader options
            elif encoding.HasFlag(JsonUnionEncoding.ExternalTag) then
                readExternalTag &reader options
            else
                raise (JsonException("Unsupported union encoding: " + string encoding))

    override __.Write(writer, value, options) =
        let value = box value
        if isNull value then writer.WriteNullValue() else

        if encoding.HasFlag(JsonUnionEncoding.AdjacentTag) then
            writeAdjacentTag writer value options
        elif encoding.HasFlag(JsonUnionEncoding.ExternalTag) then
            writeExternalTag writer value options
        else
            raise (JsonException("Unsupported union encoding: " + string encoding))

type JsonUnionConverter
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        encoding: JsonUnionEncoding
    ) =
    inherit JsonConverterFactory()

    static let jsonUnionConverterTy = typedefof<JsonUnionConverter<_>>
    static let jsonUnionEncodingTy = typeof<JsonUnionEncoding>

    static member internal CanConvert(typeToConvert) =
        TypeCache.isUnion typeToConvert

    static member internal CreateConverter(typeToConvert, encoding: JsonUnionEncoding) =
        jsonUnionConverterTy
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([|jsonUnionEncodingTy|])
            .Invoke([|encoding|])
        :?> JsonConverter

    override this.CanConvert(typeToConvert) =
        JsonUnionConverter.CanConvert(typeToConvert)

    override this.CreateConverter(typeToConvert) =
        JsonUnionConverter.CreateConverter(typeToConvert, encoding)
