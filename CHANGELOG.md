# Changelog

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
