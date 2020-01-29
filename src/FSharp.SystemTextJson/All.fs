namespace System.Text.Json.Serialization

open System
open System.Runtime.InteropServices
open System.Text.Json

type JsonFSharpConverter(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterFactory()

    override _.CanConvert(typeToConvert) =
        TypeCache.getKind typeToConvert <> TypeCache.TypeKind.Other

    static member internal CreateConverter(typeToConvert, options, fsOptions) =
        if JsonListConverter.CanConvert(typeToConvert) then
            JsonListConverter.CreateConverter(typeToConvert)
        elif JsonSetConverter.CanConvert(typeToConvert) then
            JsonSetConverter.CreateConverter(typeToConvert)
        elif JsonMapConverter.CanConvert(typeToConvert) then
            JsonMapConverter.CreateConverter(typeToConvert)
        elif JsonTupleConverter.CanConvert(typeToConvert) then
            JsonTupleConverter.CreateConverter(typeToConvert)
        elif JsonRecordConverter.CanConvert(typeToConvert) then
            JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions)
        elif JsonUnionConverter.CanConvert(typeToConvert) then
            JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions)
        else
            invalidOp ("Not an F# record or union type: " + typeToConvert.FullName)

    override _.CreateConverter(typeToConvert, options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions)

    new (
            [<Optional; DefaultParameterValue(Default.UnionEncoding)>]
            unionEncoding: JsonUnionEncoding,
            [<Optional; DefaultParameterValue(Default.UnionTagName)>]
            unionTagName: JsonUnionTagName,
            [<Optional; DefaultParameterValue(Default.UnionFieldsName)>]
            unionFieldsName: JsonUnionFieldsName,
            [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>]
            unionTagNamingPolicy: JsonNamingPolicy,
            [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>]
            unionTagCaseInsensitive: bool
        ) =
        JsonFSharpConverter(JsonFSharpOptions(unionEncoding, unionTagName, unionFieldsName, unionTagNamingPolicy, unionTagCaseInsensitive))

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute
    (
        [<Optional; DefaultParameterValue(Default.UnionEncoding)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>]
        unionFieldsName: JsonUnionFieldsName
    ) =
    inherit JsonConverterAttribute()

    let options = JsonSerializerOptions()

    let fsOptions = JsonFSharpOptions(unionEncoding, unionTagName, unionFieldsName, Default.UnionTagNamingPolicy)

    override _.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions)
