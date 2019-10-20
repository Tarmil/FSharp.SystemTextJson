# Changelog

## 0.6

* Add `unionTagName` option to customize the `"Case"` tag for unions.

## 0.5

* [#17](https://github.com/tarmil/FSharp.SystemTextJson/issues/17) Add encoding of collections:
    * `'T list` encoded as JSON array.
    * `Set<'T>` encoded as JSON array.
    * `Map<string, 'T>` encoded as JSON object with map keys as field names.
    * `Map<'K, 'V>` when `'K` is not `string` encoded as JSON array whose elements are `[key,value]` JSON arrays.
    * Tuples and struct tuples encoded as JSON array.

## 0.4

* [#6](https://github.com/tarmil/FSharp.SystemTextJson/issues/6) Add different encodings for F# unions.
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
