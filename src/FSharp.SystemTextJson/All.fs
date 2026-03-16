namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text.Json

type JsonFSharpConverter(fsOptions: JsonFSharpOptions, [<Optional>] overrides: IDictionary<Type, JsonFSharpOptions>) =
    inherit JsonConverterFactory()

    let fsOptions =
        if isNull overrides || overrides.Count = 0 then
            fsOptions
        else
            fsOptions.WithOverrides(overrides)

    member _.Options = fsOptions

    member _.Overrides = fsOptions.Overrides

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

    static member internal CreateConverter(typeToConvert, options, fsOptions) =
        match TypeCache.getKind typeToConvert with
        | TypeCache.TypeKind.List -> JsonListConverter.CreateConverter(typeToConvert, options, fsOptions)
        | TypeCache.TypeKind.Set -> JsonSetConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Map -> JsonMapConverter.CreateConverter(typeToConvert, options, fsOptions)
        | TypeCache.TypeKind.Tuple -> JsonTupleConverter.CreateConverter(typeToConvert, fsOptions)
        | TypeCache.TypeKind.Record -> JsonRecordConverter.CreateConverter(typeToConvert, options, fsOptions)
        | TypeCache.TypeKind.Union -> JsonUnionConverter.CreateConverter(typeToConvert, options, fsOptions)
        | _ -> invalidOp ("Not an F# type: " + typeToConvert.FullName)

    override _.CreateConverter(typeToConvert, options) =
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions)

    new() = JsonFSharpConverter(JsonFSharpOptions())

    new
        (
            [<Optional; DefaultParameterValue(Default.UnionEncoding)>] unionEncoding: JsonUnionEncoding,
            [<Optional; DefaultParameterValue(Default.UnionTagName)>] unionTagName: JsonUnionTagName,
            [<Optional; DefaultParameterValue(Default.UnionFieldsName)>] unionFieldsName: JsonUnionFieldsName,
            [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionTagNamingPolicy: JsonNamingPolicy,
            [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionFieldNamingPolicy: JsonNamingPolicy,
            [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>] unionTagCaseInsensitive: bool,
            [<Optional; DefaultParameterValue(Default.AllowNullFields)>] allowNullFields: bool,
            [<Optional; DefaultParameterValue(Default.IncludeRecordProperties)>] includeRecordProperties: bool,
            [<Optional; DefaultParameterValue(Default.Types)>] types: JsonFSharpTypes,
            [<Optional; DefaultParameterValue(false)>] allowOverride: bool,
            [<Optional>] overrides: IDictionary<Type, JsonFSharpOptions>
        ) =
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
            )
                .WithOverrides(overrides)
        )

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute(fsOptions: JsonFSharpOptions) =
    inherit JsonConverterAttribute()

    let mutable fsOptions = fsOptions

    static let namingPolicy =
        function
        | JsonKnownNamingPolicy.Unspecified -> null
        | JsonKnownNamingPolicy.CamelCase -> JsonNamingPolicy.CamelCase
        | p -> failwithf "Unknown naming policy: %A" p

    member _.UnionEncoding
        with set v = fsOptions <- fsOptions.WithUnionEncoding(v)
    member _.BaseUnionEncoding
        with set v = fsOptions <- fsOptions.WithBaseUnionEncoding(v)
    member _.UnionTagName
        with set v = fsOptions <- fsOptions.WithUnionTagName(v)
    member _.UnionFieldsName
        with set v = fsOptions <- fsOptions.WithUnionFieldsName(v)
    member _.UnionTagNamingPolicy
        with set v = fsOptions <- fsOptions.WithUnionTagNamingPolicy(namingPolicy v)
    member _.UnionFieldNamingPolicy
        with set v = fsOptions <- fsOptions.WithUnionFieldNamingPolicy(namingPolicy v)
    member _.UnionTagCaseSensitive
        with set v = fsOptions <- fsOptions.WithUnionTagCaseInsensitive(v)
    member _.AllowNullFields
        with set v = fsOptions <- fsOptions.WithAllowNullFields(v)
    member _.IncludeRecordProperties
        with set v = fsOptions <- fsOptions.WithIncludeRecordProperties(v)
    member _.Types
        with set v = fsOptions <- fsOptions.WithTypes(v)
    member _.UnionNamedFields
        with set v = fsOptions <- fsOptions.WithUnionNamedFields(v)
    member _.UnionUnwrapFieldlessTags
        with set v = fsOptions <- fsOptions.WithUnionUnwrapFieldlessTags(v)
    member _.UnwrapOption
        with set v = fsOptions <- fsOptions.WithUnwrapOption(v)
    member _.UnionUnwrapSingleCaseUnions
        with set v = fsOptions <- fsOptions.WithUnionUnwrapSingleCaseUnions(v)
    member _.UnionUnwrapSingleFieldCases
        with set v = fsOptions <- fsOptions.WithUnionUnwrapSingleFieldCases(v)
    member _.UnionUnwrapRecordCases
        with set v = fsOptions <- fsOptions.WithUnionUnwrapRecordCases(v)
    member _.UnionAllowUnorderedTag
        with set v = fsOptions <- fsOptions.WithUnionAllowUnorderedTag(v)
    member _.UnionFieldNamesFromTypes
        with set v = fsOptions <- fsOptions.WithUnionFieldNamesFromTypes(v)
    member _.SkippableOptionFields
        with set v = fsOptions <- fsOptions.WithSkippableOptionFields(v: SkippableOptionFields)

    new() = JsonFSharpConverterAttribute(JsonFSharpOptions())

    new
        (
            [<Optional; DefaultParameterValue(Default.UnionEncoding ||| JsonUnionEncoding.Inherit)>] unionEncoding:
                JsonUnionEncoding,
            [<Optional; DefaultParameterValue(Default.UnionTagName)>] unionTagName: JsonUnionTagName,
            [<Optional; DefaultParameterValue(Default.UnionFieldsName)>] unionFieldsName: JsonUnionFieldsName,
            [<Optional; DefaultParameterValue(JsonKnownNamingPolicy.Unspecified)>] unionTagNamingPolicy:
                JsonKnownNamingPolicy,
            [<Optional; DefaultParameterValue(JsonKnownNamingPolicy.Unspecified)>] unionFieldNamingPolicy:
                JsonKnownNamingPolicy,
            [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>] unionTagCaseInsensitive: bool,
            [<Optional; DefaultParameterValue(Default.AllowNullFields)>] allowNullFields: bool,
            [<Optional; DefaultParameterValue(Default.IncludeRecordProperties)>] includeRecordProperties: bool,
            [<Optional; DefaultParameterValue(Default.Types)>] types: JsonFSharpTypes
        ) =
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
        JsonFSharpConverterAttribute(fsOptions)

    override _.CreateConverter(typeToConvert) =
        let options = JsonSerializerOptions()
        JsonFSharpConverter.CreateConverter(typeToConvert, options, fsOptions)

    interface IJsonFSharpConverterAttribute with
        member this.Options = fsOptions

[<Extension>]
type JsonFSharpConverterExtensions =

    [<Extension>]
    static member AddToJsonSerializerOptions(this: JsonFSharpConverter, jsonSerializerOptions: JsonSerializerOptions) =
        jsonSerializerOptions.Converters.Add(this)

    [<Extension>]
    static member ToJsonSerializerOptions(this: JsonFSharpConverter) =
        let jsonSerializerOptions = JsonSerializerOptions()
        this.AddToJsonSerializerOptions(jsonSerializerOptions)
        jsonSerializerOptions

    [<Extension>]
    static member AddToJsonSerializerOptions(this: JsonFSharpOptions, jsonSerializerOptions: JsonSerializerOptions) =
        JsonFSharpConverter(this).AddToJsonSerializerOptions(jsonSerializerOptions)

    [<Extension>]
    static member ToJsonSerializerOptions(this: JsonFSharpOptions) =
        JsonFSharpConverter(this).ToJsonSerializerOptions()
