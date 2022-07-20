namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Text.Json

type JsonFSharpConverter(fsOptions: JsonFSharpOptions, [<Optional>] overrides: IDictionary<Type, JsonFSharpOptions>) =
    inherit JsonConverterFactory()

    member _.Options = fsOptions

    member _.Overrides = overrides

    override _.CanConvert(typeToConvert) =
        match TypeCache.getKind typeToConvert with
        | TypeCache.TypeKind.List -> fsOptions.Types.HasFlag JsonFSharpTypes.Lists
        | TypeCache.TypeKind.Set -> fsOptions.Types.HasFlag JsonFSharpTypes.Sets
        | TypeCache.TypeKind.Map -> fsOptions.Types.HasFlag JsonFSharpTypes.Maps
        | TypeCache.TypeKind.Tuple -> fsOptions.Types.HasFlag JsonFSharpTypes.Tuples
        | TypeCache.TypeKind.Record -> fsOptions.Types.HasFlag JsonFSharpTypes.Records
        | TypeCache.TypeKind.Union ->
            if typeToConvert.IsGenericType then
                let gen = typeToConvert.GetGenericTypeDefinition()
                if gen = typedefof<option<_>> then
                    fsOptions.Types.HasFlag JsonFSharpTypes.Options
                elif gen = typedefof<voption<_>> then
                    fsOptions.Types.HasFlag JsonFSharpTypes.ValueOptions
                else
                    fsOptions.Types.HasFlag JsonFSharpTypes.Unions
            else
                fsOptions.Types.HasFlag JsonFSharpTypes.Unions
        | _ -> false

    static member internal CreateConverter(typeToConvert, options, fsOptions, overrides) =
        match TypeCache.getKind typeToConvert with
        | TypeCache.TypeKind.List -> JsonListConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Set -> JsonSetConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Map -> JsonMapConverter.CreateConverter(typeToConvert)
        | TypeCache.TypeKind.Tuple -> JsonTupleConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Record -> JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)
        | TypeCache.TypeKind.Union -> JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)
        | _ -> invalidOp ("Not an F# type: " + typeToConvert.FullName)

    override _.CreateConverter(typeToConvert, options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions, overrides)

    new() = JsonFSharpConverter(JsonFSharpOptions())

    new([<Optional; DefaultParameterValue(Default.UnionEncoding)>] unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>] unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>] unionFieldsName: JsonUnionFieldsName,
        [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionTagNamingPolicy: JsonNamingPolicy,
        [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionFieldNamingPolicy: JsonNamingPolicy,
        [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>] unionTagCaseInsensitive: bool,
        [<Optional; DefaultParameterValue(Default.AllowNullFields)>] allowNullFields: bool,
        [<Optional; DefaultParameterValue(Default.IncludeRecordProperties)>] includeRecordProperties: bool,
        [<Optional; DefaultParameterValue(Default.Types)>] types: JsonFSharpTypes,
        [<Optional; DefaultParameterValue(false)>] allowOverride: bool,
        [<Optional>] overrides: IDictionary<Type, JsonFSharpOptions>) =
        JsonFSharpConverter(
            JsonFSharpOptions(
                unionEncoding = unionEncoding,
                unionTagName = unionTagName,
                unionFieldsName = unionFieldsName,
                unionTagNamingPolicy = unionTagNamingPolicy,
                unionFieldNamingPolicy = unionFieldNamingPolicy,
                unionTagCaseInsensitive = unionTagCaseInsensitive,
                allowNullFields = allowNullFields,
                includeRecordProperties = includeRecordProperties,
                types = types,
                allowOverride = allowOverride
            ),
            overrides
        )

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute
    (
        [<Optional; DefaultParameterValue(Default.UnionEncoding ||| JsonUnionEncoding.Inherit)>] unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>] unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>] unionFieldsName: JsonUnionFieldsName,
        [<Optional; DefaultParameterValue(JsonKnownNamingPolicy.Unspecified)>] unionTagNamingPolicy: JsonKnownNamingPolicy,
        [<Optional; DefaultParameterValue(JsonKnownNamingPolicy.Unspecified)>] unionFieldNamingPolicy: JsonKnownNamingPolicy,
        [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>] unionTagCaseInsensitive: bool,
        [<Optional; DefaultParameterValue(Default.AllowNullFields)>] allowNullFields: bool,
        [<Optional; DefaultParameterValue(Default.IncludeRecordProperties)>] includeRecordProperties: bool,
        [<Optional; DefaultParameterValue(Default.Types)>] types: JsonFSharpTypes
    ) =
    inherit JsonConverterAttribute()

    let options = JsonSerializerOptions()

    let namingPolicy =
        function
        | JsonKnownNamingPolicy.Unspecified -> null
        | JsonKnownNamingPolicy.CamelCase -> JsonNamingPolicy.CamelCase
        | p -> failwithf "Unknown naming policy: %A" p

    let fsOptions =
        JsonFSharpOptions(
            unionEncoding = unionEncoding,
            unionTagName = unionTagName,
            unionFieldsName = unionFieldsName,
            unionTagNamingPolicy = namingPolicy unionTagNamingPolicy,
            unionFieldNamingPolicy = namingPolicy unionFieldNamingPolicy,
            unionTagCaseInsensitive = unionTagCaseInsensitive,
            allowNullFields = allowNullFields,
            includeRecordProperties = includeRecordProperties,
            types = types,
            allowOverride = false
        )

    override _.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions, null)

    interface IJsonFSharpConverterAttribute with
        member this.Options = fsOptions
