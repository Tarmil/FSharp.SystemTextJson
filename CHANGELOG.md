# Changelog

## 1.0

* [#89](https://github.com/Tarmil/FSharp.SystemTextJson/issues/89): Add `JsonNameAttribute` as a more powerful substitute for the standard `JsonPropertyNameAttribute`.
    * When used on a discriminated union case, `JsonNameAttribute` can take a value of type `int` or `bool` instead of `string`.
    * `JsonNameAttribute` can take multiple values. When deserializing, all these values are treated as equivalent. When serializing, the first one is used.
    * `JsonNameAttribute` has a settable property `Field: string`. It is used to set the JSON name of a union case field with the given name.

* [#92](https://github.com/Tarmil/FSharp.SystemTextJson/issues/92): Serialization of record properties (in addition to fields).
    * Add option `includeRecordProperties: bool` to enable serializing record properties.
    * Also serialize record properties with `JsonIncludeAttribute` even when `includeRecordProperties` is false.

* Add support for the standard `DefaultIgnoreCondition.WhenWritingNull` as a synonym for `IgnoreNullValues = true` that allows `None` and `ValueNone` to be omitted instead of `null`.

* [#106](https://github.com/Tarmil/FSharp.SystemTextJson/issues/106): Fix deserialization of single-case union containing a legitimate `null` value.

* [#123](https://github.com/Tarmil/FSharp.SystemTextJson/issues/123): **BREAKING CHANGE**: Missing fields of type `option` or `voption` now throw an error by default instead of being deserialized to `None` / `ValueNone`. To support missing fields, either enable the option `IgnoreNullValues = true` or `DefaultIgnoreCondition = WhenWritingNull`, or use the type `Skippable` instead of `option` or `voption`.

* [#126](https://github.com/Tarmil/FSharp.SystemTextJson/issues/126): Add option `types: JsonFSharpTypes` that specifies which types the F# converter should handle. Unlisted types will be handled by the default `System.Text.Json`. By default, all supported types are handled.

## 0.19

* [#111](https://github.com/Tarmil/FSharp.SystemTextJson/issues/111): Support `JsonKnownNamingPolicy` in `JsonFSharpConverterAttribute`.

* [#112](https://github.com/Tarmil/FSharp.SystemTextJson/issues/112): Add `Options` and `Overrides` properties to `JsonFSharpConverter`.

* [#263](https://github.com/Tarmil/FSharp.SystemTextJson/issues/263): Add a nullary constructor for JsonFSharpConverter.


## 0.18

* [#98](https://github.com/Tarmil/FSharp.SystemTextJson/issues/98): Implement `JsonPropertyOrderAttribute` on records.

* [#108](https://github.com/Tarmil/FSharp.SystemTextJson/issues/108): Allow serializing unnamed union fields using their type name, instead of `Item1` etc, using `JsonUnionEncoding.UnionFieldNamesFromTypes`.

* [#100](https://github.com/Tarmil/FSharp.SystemTextJson/issues/100): Allow unionTagName to be part of the record type definition when deserializing union of records.

* [#109](https://github.com/Tarmil/FSharp.SystemTextJson/issues/109): Add .xml with documentation comments to nupkg. Thanks @exyi!

* [#110](https://github.com/Tarmil/FSharp.SystemTextJson/issues/110): Allow customizing the naming policy for union fields with `unionFieldNamingPolicy`.  
    If not provided, use `JsonSerializerOptions.PropertyNamingPolicy` as before.

## 0.17

* [#87](https://github.com/Tarmil/FSharp.SystemTextJson/issues/87): Fix null checking on value type fields.

* [#93](https://github.com/Tarmil/FSharp.SystemTextJson/issues/93): Allow deserializing F# unions in `AdjacentTag` and `InternalTag` modes where the tag is not the first field in the JSON object.
    This capability is enabled by new option `JsonUnionEncoding.AllowUnorderedTag`, which is included in `JsonUnionEncoding.Default` and all `JsonUnionEncoding.*Like` combined formats.

## 0.16

* [#83](https://github.com/Tarmil/FSharp.SystemTextJson/issues/83): Fix null reference exception when deserializing a record that `Skip`s a field with FSharp.Core 5.0.
* Remove netcoreapp3.0 specific build.

## 0.15

* [#77](https://github.com/Tarmil/FSharp.SystemTextJson/issues/77): Fix compile-time error when publishing a trimmed .NET 5 application. Thanks @pchalamet!

## 0.14

* [#71](https://github.com/Tarmil/FSharp.SystemTextJson/issues/71): Allow overriding specific types in `JsonFSharpConverter` with new argument `overrides`. These override even types that have `JsonFSharpConverterAttribute` when `allowOverride = true`.

## 0.13

* [#69](https://github.com/Tarmil/FSharp.SystemTextJson/issues/69): Allow overriding `JsonFSharpConverter` options with `JsonFSharpConverterAttribute` by passing `allowOverride = true` to `JsonFSharpConverter`.
* Add `JsonUnionEncoding.Inherit`. When this flag is set on a `JsonFSharpConverterAttribute` that overrides a `JsonFSharpConverter`, this type still uses the `JsonFSharpConverter`'s `JsonUnionEncoding`.

## 0.12

* [#56](https://github.com/Tarmil/FSharp.SystemTextJson/issues/56): Add `JsonUnionEncoding.UnwrapRecordCases`, which implies `JsonUnionEncoding.NamedFields` and encodes union cases containing a single record field as if the record's fields were the union's fields instead. For example:
    ```fsharp
    type U =
        | U of r: {| x: int; y: bool |}

    U {| x = 1; y = true |}
    // Serialized as: {"Case":"U","Fields":{"x":1,"y":true}}
    // Instead of:    {"Case":"U","Fields":{"r":{"x":1,"y":true}}}
    ```
    This option is compatible with all union formats (`AdjacentTag`, `ExternalTag`, `InternalTag` and `Untagged`).
* [#64](https://github.com/Tarmil/FSharp.SystemTextJson/issues/64): Fix serialization of `unit` as field of an F# type. Thanks @NickDarvey!

## 0.11

* [#54](https://github.com/Tarmil/FSharp.SystemTextJson/issues/54): Do throw an exception when a mandatory field is missing and an optional field was omitted. Thanks @fuchen!

* [#57](https://github.com/Tarmil/FSharp.SystemTextJson/issues/57): Do throw an exception when a parsed value is null inside an array, a set or a tuple, unless `allowNullFields` is true. Thanks @drhumlen!

## 0.10

* [#47](https://github.com/Tarmil/FSharp.SystemTextJson/issues/47): Add `Skippable<'T>` to represent values that can be omitted from the serialization of a record or a union with `NamedFields`. This is particularly useful with `Skippable<'T option>` (or `voption`) to represent a field that can be omitted, null, or have a proper value.
    ```fsharp
    type R = { x: Skippable<int option> }

    { x = Skip }              // Serialized as: {}
    { x = Include None }      // Serialized as: {"x":null}
    { x = Include (Some 42) } // Serialized as: {"x":42}
    ```
    Also add a `Skippable` module with standard functions: `map`, `filter`, etc. Implementation largely based on @cmeeren's [JsonSkippable](https://github.com/cmeeren/FSharp.JsonSkippable/blob/master/src/FSharp.JsonSkippable/Skippable.fs) which provides the same functionality for Newtonsoft.Json.
* [#51](https://github.com/Tarmil/FSharp.SystemTextJson/issues/51): When the type `K` is a single-case union wrapping a string, serialize `Map<K, V>` into a JSON object, like `Map<string, V>`.

## 0.9

* [#43](https://github.com/Tarmil/FSharp.SystemTextJson/issues/43): In deserialization, allow omitting fields that are optional.

## 0.8

* [#30](https://github.com/Tarmil/FSharp.SystemTextJson/issues/30): Unwrap `'T voption` with `JsonUnionEncoding.UnwrapOption`.
* [#32](https://github.com/Tarmil/FSharp.SystemTextJson/issues/32): Add `JsonUnionEncoding.UnwrapSingleFieldCases`, which encodes the field of single-field cases as `x` instead of `[x]`. Include it in `JsonUnionEncoding.FSharpLuLike`.
* [#33](https://github.com/Tarmil/FSharp.SystemTextJson/issues/33): Fix "read too much or not enough" when parsing a list of unions with `JsonUnionEncoding.UnwrapFieldlessTags`.
* [#38](https://github.com/Tarmil/FSharp.SystemTextJson/issues/38): Add more consistent names for options:
    * `BareFieldlessTags` becomes `UnwrapFieldlessTags`;
    * `SuccintOption` becomes `UnwrapOption`;
    * `EraseSingleCaseUnions` becomes `UnwrapSingleCaseUnions`.

    The previous names are marked `Obsolete`.

## 0.7

* [#3](https://github.com/tarmil/FSharp.SystemTextJson/issues/3): Add support for `PropertyNamingPolicy` on record and union fields.
* [#22](https://github.com/tarmil/FSharp.SystemTextJson/issues/22): Add support for `DictionaryKeyPolicy` for `Map<string, 'T>`.
* [#27](https://github.com/tarmil/FSharp.SystemTextJson/issues/27): Add `unionTagNamingPolicy` option to `JsonFSharpConverter` and `JsonFSharpConverterAttribute` to customize the naming policy for union tag names.
* [#26](https://github.com/tarmil/FSharp.SystemTextJson/issues/26): Add `JsonUnionEncoding.EraseSingleCaseUnions`, which encodes single-case single-field unions the same as the value in the field.  
    **BREAKING CHANGE**: This is now the default option.
* [#5](https://github.com/tarmil/FSharp.SystemTextJson/issues/5): Add support for `PropertyNameCaseInsensitive` on record and union fields.  
    Add `unionTagCaseInsensitive` option to `JsonFSharpConverter` and `JsonFSharpConverterAttribute` to customize the case sensitivity for union tag names.

## 0.6

* [#4](https://github.com/tarmil/FSharp.SystemTextJson/issues/4): Add support for standard option `IgnoreNullValues` on record and union fields.
* [#13](https://github.com/tarmil/FSharp.SystemTextJson/issues/14): Add support for `JsonPropertyNameAttribute` on union cases to set the tag name to use for this case.
* [#16](https://github.com/tarmil/FSharp.SystemTextJson/issues/16): Add `JsonUnionEncoding.SuccintOption`, which encodes `Some x` the same as `x`.  
    **BREAKING CHANGE**: This is now the default option.
* Add `unionTagName` and `unionFieldsName` option to customize the `"Case"` and `"Fields"` tags for unions.
* Add `JsonUnionEncoding.FSharpLuLike`, which is equivalent to `ExternalTag ||| BareFieldlessTags ||| SuccintOption`.

## 0.5

* [#17](https://github.com/tarmil/FSharp.SystemTextJson/issues/17): Add encoding of collections:
    * `'T list` encoded as JSON array.
    * `Set<'T>` encoded as JSON array.
    * `Map<string, 'T>` encoded as JSON object with map keys as field names.
    * `Map<'K, 'V>` when `'K` is not `string` encoded as JSON array whose elements are `[key,value]` JSON arrays.
    * Tuples and struct tuples encoded as JSON array.

## 0.4

* [#6](https://github.com/tarmil/FSharp.SystemTextJson/issues/6): Add different encodings for F# unions.
    * `JsonFSharpConverter` and `JsonFSharpConverterAttribute` now take `JsonUnionEncoding` as optional argument.
    * Unions are encoded depending on the `JsonUnionEncoding` as detailed in [the documentation](README.md#unions).

## 0.3

* [#9](https://github.com/tarmil/FSharp.SystemTextJson/issues/9): Cache the result of `FSharpType.IsRecord` and `FSharpType.IsUnion`
* [#12](https://github.com/tarmil/FSharp.SystemTextJson/issues/12): Target .NET Standard 2.0 rather than .NET Core 3.0

## 0.2

* [#1](https://github.com/tarmil/FSharp.SystemTextJson/issues/1): Add support for `JsonPropertyNameAttribute` on record fields
* [#2](https://github.com/tarmil/FSharp.SystemTextJson/issues/2): Add support for `JsonIgnoreAttribute` on record fields

## 0.1

* Serialize records
* Serialize unions with Newtonsoft-like format
