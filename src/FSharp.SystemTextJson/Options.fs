namespace System.Text.Json.Serialization

open System
open System.Runtime.InteropServices
open System.Text.Json

type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// `unionTagName` (defaults to "Case") contains the union tag,
    /// and `unionFieldsName` (defaults to "Fields") contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    /// This flag is included in Default.
    | AdjacentTag               = 0x00_00_00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag               = 0x00_00_00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named `unionTagName`, defaulting to "Case", if NamedFields is set)
    /// is the union tag, the rest are the union fields.
    | InternalTag               = 0x00_00_00_04

    /// Encode unions as a n-valued object:
    /// the union tag is not encoded, only the union fields are.
    /// Deserialization is only possible if the fields of all cases have different names.
    | Untagged                  = 0x00_00_01_08



    //// Inheritance

    /// When set on an FSharpJsonConverterAttribute (true if no encoding is passed),
    /// inherit the union encoding from the FSharpJsonConverter, if any.
    /// When set on an FSharpJsonConverter object, this flag is ignored.
    | Inherit                   = 0x80_00_00_00


    //// Additional options

    /// If unset, union fields are encoded as an array.
    /// If set, union fields are encoded as an object using their field names.
    | NamedFields               = 0x00_00_01_00

    /// If set, union cases that don't have fields are encoded as a bare string.
    | UnwrapFieldlessTags       = 0x00_00_02_00

    /// Obsolete: use UnwrapFieldlessTags instead.
    | [<Obsolete "Use UnwrapFieldlessTags">]
      BareFieldlessTags         = 0x00_00_02_00

    /// If set, `None` is represented as null,
    /// and `Some x`  is represented the same as `x`.
    /// This flag is included in Default.
    | UnwrapOption              = 0x00_00_04_00

    /// Obsolete: use UnwrapOption instead.
    | [<Obsolete "Use UnwrapOption">]
      SuccintOption             = 0x00_00_04_00

    /// If set, single-case single-field unions are serialized as the single field's value.
    /// This flag is included in Default.
    | UnwrapSingleCaseUnions    = 0x00_00_08_00

    /// Obsolete: use UnwrapSingleCaseUnions instead.
    | [<Obsolete "Use UnwrapSingleCaseUnions">]
      EraseSingleCaseUnions     = 0x00_00_08_00

    /// If set, the field of a single-field union case is encoded as just the value
    /// rather than a single-value array or object.
    | UnwrapSingleFieldCases    = 0x00_00_10_00

    /// Implicitly sets NamedFields. If set, when a union case has a single field which is a record,
    /// the fields of this record are encoded directly as fields of the object representing the union.
    | UnwrapRecordCases         = 0x00_00_21_00

    /// In AdjacentTag and InternalTag mode, allow deserializing unions
    /// where the tag is not the first field in the JSON object.
    | AllowUnorderedTag         = 0x00_00_40_00


    //// Specific formats

    | Default                   = 0x00_00_4C_01 // AdjacentTag ||| UnwrapOption ||| UnwrapSingleCaseUnions ||| AllowUnorderedTag
    | NewtonsoftLike            = 0x00_00_40_01 // AdjacentTag ||| AllowUnorderedTag
    | ThothLike                 = 0x00_00_42_04 // InternalTag ||| BareFieldlessTags ||| AllowUnorderedTag
    | FSharpLuLike              = 0x00_00_56_02 // ExternalTag ||| BareFieldlessTags ||| UnwrapOption ||| UnwrapSingleFieldCases ||| AllowUnorderedTag

type JsonUnionTagName = string
type JsonUnionFieldsName = string

module internal Default =

    let [<Literal>] UnionEncoding = JsonUnionEncoding.Default

    let [<Literal>] UnionTagName = "Case"

    let [<Literal>] UnionFieldsName = "Fields"

    let [<Literal>] UnionTagNamingPolicy = null : JsonNamingPolicy

    let [<Literal>] UnionTagCaseInsensitive = false

    let [<Literal>] AllowNullFields = false

type JsonFSharpOptions
    (
        [<Optional; DefaultParameterValue(Default.UnionEncoding ||| JsonUnionEncoding.Inherit)>]
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
        allowOverride: bool
    ) =

    member this.UnionEncoding = unionEncoding

    member this.UnionTagName = unionTagName

    member this.UnionFieldsName = unionFieldsName

    member this.UnionTagNamingPolicy = unionTagNamingPolicy

    member this.UnionTagCaseInsensitive = unionTagCaseInsensitive

    member this.AllowNullFields = allowNullFields

    member this.AllowOverride = allowOverride

    member this.WithUnionEncoding(unionEncoding) =
        JsonFSharpOptions(unionEncoding, unionTagName, unionFieldsName, unionTagNamingPolicy, unionTagCaseInsensitive, allowNullFields, allowOverride)

type IJsonFSharpConverterAttribute =
    abstract Options: JsonFSharpOptions
