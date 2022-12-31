namespace System.Text.Json.Serialization

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Text.Json

[<Flags>]
type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// `unionTagName` (defaults to "Case") contains the union tag,
    /// and `unionFieldsName` (defaults to "Fields") contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    /// This flag is included in Default.
    | AdjacentTag = 0x00_00_00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag = 0x00_00_00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named `unionTagName`, defaulting to "Case", if NamedFields is set)
    /// is the union tag, the rest are the union fields.
    | InternalTag = 0x00_00_00_04

    /// Encode unions as a n-valued object:
    /// the union tag is not encoded, only the union fields are.
    /// Deserialization is only possible if the fields of all cases have different names.
    | Untagged = 0x00_00_01_08



    //// Inheritance

    /// When set on an FSharpJsonConverterAttribute (true if no encoding is passed),
    /// inherit the union encoding from the FSharpJsonConverter, if any.
    /// When set on an FSharpJsonConverter object, this flag is ignored.
    | Inherit = 0x80_00_00_00


    //// Additional options

    /// If unset, union fields are encoded as an array.
    /// If set, union fields are encoded as an object using their field names.
    | NamedFields = 0x00_00_01_00

    /// If set, union cases that don't have fields are encoded as a bare string.
    | UnwrapFieldlessTags = 0x00_00_02_00

    /// Obsolete: use UnwrapFieldlessTags instead.
    | [<Obsolete "Use UnwrapFieldlessTags">] BareFieldlessTags = 0x00_00_02_00

    /// If set, `None` is represented as null,
    /// and `Some x`  is represented the same as `x`.
    /// This flag is included in Default.
    | UnwrapOption = 0x00_00_04_00

    /// Obsolete: use UnwrapOption instead.
    | [<Obsolete "Use UnwrapOption">] SuccintOption = 0x00_00_04_00

    /// If set, single-case single-field unions are serialized as the single field's value.
    /// This flag is included in Default.
    | UnwrapSingleCaseUnions = 0x00_00_08_00

    /// Obsolete: use UnwrapSingleCaseUnions instead.
    | [<Obsolete "Use UnwrapSingleCaseUnions">] EraseSingleCaseUnions = 0x00_00_08_00

    /// If set, the field of a single-field union case is encoded as just the value
    /// rather than a single-value array or object.
    | UnwrapSingleFieldCases = 0x00_00_10_00

    /// Implicitly sets NamedFields. If set, when a union case has a single field which is a record,
    /// the fields of this record are encoded directly as fields of the object representing the union.
    | UnwrapRecordCases = 0x00_00_21_00

    /// In AdjacentTag and InternalTag mode, allow deserializing unions
    /// where the tag is not the first field in the JSON object.
    | AllowUnorderedTag = 0x00_00_40_00

    /// When a union field doesn't have an explicit name, use its type as name.
    | UnionFieldNamesFromTypes = 0x00_00_80_00


    //// Specific formats

    | Default = 0x00_00_4C_01 // AdjacentTag ||| UnwrapOption ||| UnwrapSingleCaseUnions ||| AllowUnorderedTag
    | NewtonsoftLike = 0x00_00_40_01 // AdjacentTag ||| AllowUnorderedTag
    | ThothLike = 0x00_00_42_04 // InternalTag ||| BareFieldlessTags ||| AllowUnorderedTag
    | FSharpLuLike = 0x00_00_56_02 // ExternalTag ||| BareFieldlessTags ||| UnwrapOption ||| UnwrapSingleFieldCases ||| AllowUnorderedTag

type JsonUnionTagName = string
type JsonUnionFieldsName = string

type JsonFSharpTypes =
    /// F# records, struct records and anonymous records.
    | Records = 0x001
    /// F# discriminated unions and struct discriminated unions.
    | Unions = 0x002
    /// Tuples and struct tuples.
    | Tuples = 0x004
    /// F# lists.
    | Lists = 0x010
    /// F# sets.
    | Sets = 0x020
    /// F# maps, including those with non-string keys.
    | Maps = 0x040
    /// F# options.
    | Options = 0x100
    /// F# value options.
    | ValueOptions = 0x200
    /// F# options and value options.
    | OptionalTypes = 0xf00
    /// F# lists, sets and maps.
    | Collections = 0x0f0
    /// All types not already fully supported by System.Text.Json.
    | Minimal = 0x046
    /// All supported types.
    | All = 0xfff

module internal Default =

    [<Literal>]
    let UnionEncoding = JsonUnionEncoding.Default

    [<Literal>]
    let UnionTagName = "Case"

    [<Literal>]
    let UnionFieldsName = "Fields"

    [<Literal>]
    let UnionTagNamingPolicy: JsonNamingPolicy = null

    [<Literal>]
    let UnionFieldNamingPolicy: JsonNamingPolicy = null

    [<Literal>]
    let UnionTagCaseInsensitive = false

    [<Literal>]
    let IncludeRecordProperties = false

    [<Literal>]
    let AllowNullFields = false

    [<Literal>]
    let Types = JsonFSharpTypes.All

type internal JsonFSharpOptionsRecord =
    { UnionEncoding: JsonUnionEncoding
      UnionTagName: JsonUnionTagName
      UnionFieldsName: JsonUnionFieldsName
      UnionTagNamingPolicy: JsonNamingPolicy
      UnionFieldNamingPolicy: JsonNamingPolicy
      UnionTagCaseInsensitive: bool
      AllowNullFields: bool
      IncludeRecordProperties: bool
      Types: JsonFSharpTypes
      AllowOverride: bool
      Overrides: JsonFSharpOptions -> IDictionary<Type, JsonFSharpOptions> }

and JsonFSharpOptions internal (options: JsonFSharpOptionsRecord) =

    let removeBaseEncodings = enum<JsonUnionEncoding> ~~~ 0x00_00_00_FF

    static let emptyOverrides (_: JsonFSharpOptions) =
        null

    // Note: For binary compatibility, don't add options to this constructor.
    // New options will only be settable via fluent API.
    new([<Optional; DefaultParameterValue(Default.UnionEncoding ||| JsonUnionEncoding.Inherit)>] unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>] unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>] unionFieldsName: JsonUnionFieldsName,
        [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionTagNamingPolicy: JsonNamingPolicy,
        [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>] unionFieldNamingPolicy: JsonNamingPolicy,
        [<Optional; DefaultParameterValue(Default.UnionTagCaseInsensitive)>] unionTagCaseInsensitive: bool,
        [<Optional; DefaultParameterValue(Default.AllowNullFields)>] allowNullFields: bool,
        [<Optional; DefaultParameterValue(Default.IncludeRecordProperties)>] includeRecordProperties: bool,
        [<Optional; DefaultParameterValue(Default.Types)>] types: JsonFSharpTypes,
        [<Optional; DefaultParameterValue(false)>] allowOverride: bool) =
        JsonFSharpOptions(
            { UnionEncoding = unionEncoding
              UnionTagName = unionTagName
              UnionFieldsName = unionFieldsName
              UnionTagNamingPolicy = unionTagNamingPolicy
              UnionFieldNamingPolicy = unionFieldNamingPolicy
              UnionTagCaseInsensitive = unionTagCaseInsensitive
              AllowNullFields = allowNullFields
              IncludeRecordProperties = includeRecordProperties
              Types = types
              AllowOverride = allowOverride
              Overrides = emptyOverrides }
        )

    static member Default() =
        JsonFSharpOptions(Default.UnionEncoding)

    static member InheritUnionEncoding() =
        JsonFSharpOptions(JsonUnionEncoding.Inherit)

    static member NewtonsoftLike() =
        JsonFSharpOptions(JsonUnionEncoding.NewtonsoftLike)

    static member ThothLike() =
        JsonFSharpOptions(JsonUnionEncoding.ThothLike)

    static member FSharpLuLike() =
        JsonFSharpOptions(JsonUnionEncoding.FSharpLuLike)

    member internal _.Record = options

    member _.UnionEncoding = options.UnionEncoding

    member _.UnionTagName = options.UnionTagName

    member _.UnionFieldsName = options.UnionFieldsName

    member _.UnionTagNamingPolicy = options.UnionTagNamingPolicy

    member _.UnionFieldNamingPolicy = options.UnionFieldNamingPolicy

    member _.UnionTagCaseInsensitive = options.UnionTagCaseInsensitive

    member _.AllowNullFields = options.AllowNullFields

    member _.IncludeRecordProperties = options.IncludeRecordProperties

    member _.Types = options.Types

    member _.AllowOverride = options.AllowOverride

    member _.Overrides = options.Overrides

    member _.WithUnionEncoding(unionEncoding) =
        JsonFSharpOptions({ options with UnionEncoding = unionEncoding })

    member _.WithUnionTagName(unionTagName) =
        JsonFSharpOptions({ options with UnionTagName = unionTagName })

    member _.WithUnionFieldsName(unionFieldsName) =
        JsonFSharpOptions({ options with UnionFieldsName = unionFieldsName })

    member _.WithUnionTagNamingPolicy(unionTagNamingPolicy) =
        JsonFSharpOptions({ options with UnionTagNamingPolicy = unionTagNamingPolicy })

    member _.WithUnionFieldNamingPolicy(unionFieldNamingPolicy) =
        JsonFSharpOptions({ options with UnionFieldNamingPolicy = unionFieldNamingPolicy })

    member _.WithUnionTagCaseInsensitive([<Optional; DefaultParameterValue true>] unionTagCaseInsensitive) =
        JsonFSharpOptions({ options with UnionTagCaseInsensitive = unionTagCaseInsensitive })

    member _.WithAllowNullFields([<Optional; DefaultParameterValue true>] allowNullFields) =
        JsonFSharpOptions({ options with AllowNullFields = allowNullFields })

    member _.WithIncludeRecordProperties([<Optional; DefaultParameterValue true>] includeRecordProperties) =
        JsonFSharpOptions({ options with IncludeRecordProperties = includeRecordProperties })

    member _.WithTypes(types) =
        JsonFSharpOptions({ options with Types = types })

    member _.WithAllowOverride([<Optional; DefaultParameterValue true>] allowOverride) =
        JsonFSharpOptions({ options with AllowOverride = allowOverride })

    member _.WithOverrides(overrides) =
        JsonFSharpOptions({ options with Overrides = overrides })

    member _.WithOverrides(overrides) =
        JsonFSharpOptions({ options with Overrides = fun _ -> overrides })

    member private this.WithUnionEncodingFlag(flag, set) =
        if set then
            this.WithUnionEncoding(options.UnionEncoding ||| flag)
        else
            this.WithUnionEncoding(options.UnionEncoding &&& ~~~flag)

    member internal this.WithBaseUnionEncoding(flag) =
        this.WithUnionEncoding(options.UnionEncoding &&& removeBaseEncodings ||| flag)

    /// Encode unions as a 2-valued object:
    /// `unionTagName` (defaults to "Case") contains the union tag,
    /// and `unionFieldsName` (defaults to "Fields") contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    /// This flag is included in Default.
    member this.WithUnionAdjacentTag() =
        this.WithBaseUnionEncoding(JsonUnionEncoding.AdjacentTag)

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    member this.WithUnionExternalTag() =
        this.WithBaseUnionEncoding(JsonUnionEncoding.ExternalTag)

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named `unionTagName`, defaulting to "Case", if NamedFields is set)
    /// is the union tag, the rest are the union fields.
    member this.WithUnionInternalTag() =
        this.WithBaseUnionEncoding(JsonUnionEncoding.InternalTag)

    /// Encode unions as a n-valued object:
    /// the union tag is not encoded, only the union fields are.
    /// Deserialization is only possible if the fields of all cases have different names.
    member this.WithUnionUntagged() =
        this.WithBaseUnionEncoding(JsonUnionEncoding.Untagged)

    /// If unset, union fields are encoded as an array.
    /// If set, union fields are encoded as an object using their field names.
    member this.WithUnionNamedFields([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.NamedFields, set)

    /// If set, union cases that don't have fields are encoded as a bare string.
    member this.WithUnionUnwrapFieldlessTags([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnwrapFieldlessTags, set)

    /// If set, `None` is represented as null,
    /// and `Some x`  is represented the same as `x`.
    /// This flag is included in Default.
    member this.WithUnwrapOption([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnwrapOption, set)

    /// If set, single-case single-field unions are serialized as the single field's value.
    /// This flag is included in Default.
    member this.WithUnionUnwrapSingleCaseUnions([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnwrapSingleCaseUnions, set)

    /// If set, the field of a single-field union case is encoded as just the value
    /// rather than a single-value array or object.
    member this.WithUnionUnwrapSingleFieldCases([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnwrapSingleFieldCases, set)

    /// Implicitly sets NamedFields. If set, when a union case has a single field which is a record,
    /// the fields of this record are encoded directly as fields of the object representing the union.
    member this.WithUnionUnwrapRecordCases([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnwrapRecordCases, set)

    /// In AdjacentTag and InternalTag mode, allow deserializing unions
    /// where the tag is not the first field in the JSON object.
    member this.WithUnionAllowUnorderedTag([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.AllowUnorderedTag, set)

    /// When a union field doesn't have an explicit name, use its type as name.
    member this.WithUnionFieldNamesFromTypes([<Optional; DefaultParameterValue true>] set: bool) =
        this.WithUnionEncodingFlag(JsonUnionEncoding.UnionFieldNamesFromTypes, set)

type IJsonFSharpConverterAttribute =
    abstract Options: JsonFSharpOptions
