namespace System.Text.Json.Serialization

open System
open System.Runtime.InteropServices

type JsonFSharpConverter
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue("Case")>]
        unionTagName: JsonUnionTagName
    ) =
    inherit JsonConverterFactory()

    override this.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert) ||
        JsonSetConverter.CanConvert(typeToConvert) ||
        JsonMapConverter.CanConvert(typeToConvert) ||
        JsonTupleConverter.CanConvert(typeToConvert) ||
        JsonRecordConverter.CanConvert(typeToConvert) ||
        JsonUnionConverter.CanConvert(typeToConvert)

    static member internal CreateConverter(typeToConvert, unionEncoding, unionTagName) =
        if JsonListConverter.CanConvert(typeToConvert) then
            JsonListConverter.CreateConverter(typeToConvert)
        elif JsonSetConverter.CanConvert(typeToConvert) then
            JsonSetConverter.CreateConverter(typeToConvert)
        elif JsonMapConverter.CanConvert(typeToConvert) then
            JsonMapConverter.CreateConverter(typeToConvert)
        elif JsonTupleConverter.CanConvert(typeToConvert) then
            JsonTupleConverter.CreateConverter(typeToConvert)
        elif JsonRecordConverter.CanConvert(typeToConvert) then
            JsonRecordConverter.CreateConverter(typeToConvert)
        elif JsonUnionConverter.CanConvert(typeToConvert) then
            JsonUnionConverter.CreateConverter(typeToConvert, unionEncoding, unionTagName)
        else
            invalidOp ("Not an F# record or union type: " + typeToConvert.FullName)

    override this.CreateConverter(typeToConvert, _options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, unionEncoding, unionTagName)

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue("Case")>]
        unionTagName: JsonUnionTagName
    ) =
    inherit JsonConverterAttribute()

    override __.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert, unionEncoding, unionTagName)
