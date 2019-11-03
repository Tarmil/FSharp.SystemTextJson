namespace System.Text.Json.Serialization

open System
open System.Runtime.InteropServices
open System.Text.Json

type JsonFSharpConverter
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue("Case")>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue("Fields")>]
        unionFieldsName: JsonUnionFieldsName
    ) =
    inherit JsonConverterFactory()

    override _.CanConvert(typeToConvert) =
        JsonListConverter.CanConvert(typeToConvert) ||
        JsonSetConverter.CanConvert(typeToConvert) ||
        JsonMapConverter.CanConvert(typeToConvert) ||
        JsonTupleConverter.CanConvert(typeToConvert) ||
        JsonRecordConverter.CanConvert(typeToConvert) ||
        JsonUnionConverter.CanConvert(typeToConvert)

    static member internal CreateConverter(typeToConvert, options, unionEncoding, unionTagName, unionFieldsName) =
        if JsonListConverter.CanConvert(typeToConvert) then
            JsonListConverter.CreateConverter(typeToConvert)
        elif JsonSetConverter.CanConvert(typeToConvert) then
            JsonSetConverter.CreateConverter(typeToConvert)
        elif JsonMapConverter.CanConvert(typeToConvert) then
            JsonMapConverter.CreateConverter(typeToConvert)
        elif JsonTupleConverter.CanConvert(typeToConvert) then
            JsonTupleConverter.CreateConverter(typeToConvert)
        elif JsonRecordConverter.CanConvert(typeToConvert) then
            JsonRecordConverter.CreateConverter(typeToConvert, options)
        elif JsonUnionConverter.CanConvert(typeToConvert) then
            JsonUnionConverter.CreateConverter(typeToConvert, unionEncoding, unionTagName, unionFieldsName)
        else
            invalidOp ("Not an F# record or union type: " + typeToConvert.FullName)

    override _.CreateConverter(typeToConvert, options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, unionEncoding, unionTagName, unionFieldsName)

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute
    (
        [<Optional; DefaultParameterValue(JsonUnionEncoding.Default)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue("Case")>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue("Fields")>]
        unionFieldsName: JsonUnionFieldsName
    ) =
    inherit JsonConverterAttribute()

    let options = JsonSerializerOptions()

    override _.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, unionEncoding, unionTagName, unionFieldsName)
