namespace System.Text.Json.Serialization

open System.Runtime.InteropServices
open System.Text.Json

type JsonUnionEncoding =

    //// General base format

    /// Encode unions as a 2-valued object:
    /// `unionTagName` (defaults to "Case") contains the union tag, and "Fields" contains the union fields.
    /// If the case doesn't have fields, "Fields": [] is omitted.
    | AdjacentTag       = 0x00_01

    /// Encode unions as a 1-valued object:
    /// The field name is the union tag, and the value is the union fields.
    | ExternalTag       = 0x00_02

    /// Encode unions as a (n+1)-valued object or array (depending on NamedFields):
    /// the first value (named `unionTagName`, defaulting to "Case", if NamedFields is set)
    /// is the union tag, the rest are the union fields.
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

    /// If set, `None` is represented as null,
    /// and `Some x`  is represented the same as `x`.
    | SuccintOption     = 0x04_00


    //// Specific formats

    | Default           = 0x04_01
    | NewtonsoftLike    = 0x00_01
    | ThothLike         = 0x02_04
    | FSharpLuLike      = 0x06_02

type JsonUnionTagName = string
type JsonUnionFieldsName = string

module internal Default =

    let [<Literal>] UnionEncoding = JsonUnionEncoding.Default

    let [<Literal>] UnionTagName = "Case"

    let [<Literal>] UnionFieldsName = "Fields"

    let [<Literal>] UnionTagNamingPolicy = null : JsonNamingPolicy

type JsonFSharpOptions
    (
        [<Optional; DefaultParameterValue(Default.UnionEncoding)>]
        unionEncoding: JsonUnionEncoding,
        [<Optional; DefaultParameterValue(Default.UnionTagName)>]
        unionTagName: JsonUnionTagName,
        [<Optional; DefaultParameterValue(Default.UnionFieldsName)>]
        unionFieldsName: JsonUnionFieldsName,
        [<Optional; DefaultParameterValue(Default.UnionTagNamingPolicy)>]
        unionTagNamingPolicy: JsonNamingPolicy
    ) =

    member this.UnionEncoding = unionEncoding

    member this.UnionTagName = unionTagName

    member this.UnionFieldsName = unionFieldsName

    member this.UnionTagNamingPolicy = unionTagNamingPolicy
