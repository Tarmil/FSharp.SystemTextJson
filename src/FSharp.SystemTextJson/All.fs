namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Text.Json

type JsonFSharpConverter
    (
        fsOptions: JsonFSharpOptions,
        [<Optional>]
        overrides: IDictionary<Type, JsonFSharpOptions>
    ) =
    inherit JsonConverterFactory()

    override _.CanConvert(typeToConvert) =
        TypeCache.getKind typeToConvert <> TypeCache.TypeKind.Other

    static member internal CreateConverter(typeToConvert, options, fsOptions, overrides) =
        match TypeCache.getKind typeToConvert with
        | TypeCache.TypeKind.List ->
            JsonListConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Set ->
            JsonSetConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Map ->
            JsonMapConverter.CreateConverter(typeToConvert)
        | TypeCache.TypeKind.Tuple ->
            JsonTupleConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Record ->
            JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)
        | TypeCache.TypeKind.Union ->
            JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)
        | _ ->
            invalidOp ("Not an F# record or union type: " + typeToConvert.FullName)

    override _.CreateConverter(typeToConvert, options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)

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
            unionTagCaseInsensitive: bool,
            [<Optional; DefaultParameterValue(Default.AllowNullFields)>]
            allowNullFields: bool,
            [<Optional; DefaultParameterValue(false)>]
            allowOverride: bool,
            [<Optional>]
            overrides: IDictionary<Type, JsonFSharpOptions>
        ) =
        JsonFSharpConverter(JsonFSharpOptions(unionEncoding, unionTagName, unionFieldsName, unionTagNamingPolicy, unionTagCaseInsensitive, allowNullFields, allowOverride), overrides)

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute
    (
        [<Optional; DefaultParameterValue(Default.UnionEncoding ||| JsonUnionEncoding.Inherit)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>]
        unionFieldsName: JsonUnionFieldsName,
        [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>]
        unionTagCaseInsensitive: bool,
        [<Optional; DefaultParameterValue(Default.AllowNullFields)>]
        allowNullFields: bool
    ) =
    inherit JsonConverterAttribute()

    let options = JsonSerializerOptions()

    let fsOptions = JsonFSharpOptions(unionEncoding, unionTagName, unionFieldsName, Default.UnionTagNamingPolicy, unionTagCaseInsensitive, allowNullFields, false)

    override _.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions, null)

    interface IJsonFSharpConverterAttribute with
        member this.Options = fsOptions
