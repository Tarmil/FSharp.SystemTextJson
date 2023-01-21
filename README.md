# FSharp.SystemTextJson

[![Build status](https://github.com/Tarmil/FSharp.SystemTextJson/workflows/Build/badge.svg)](https://github.com/Tarmil/FSharp.SystemTextJson/actions?query=workflow%3ABuild)
[![Nuget](https://img.shields.io/nuget/v/FSharp.SystemTextJson?logo=nuget)](https://nuget.org/packages/FSharp.SystemTextJson)

This library provides support for F# types to [System.Text.Json](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/).

It adds support for the following types:

* F# records (including struct records and anonymous records);

* F# discriminated unions (including struct unions), with a variety of representations;

* F# collections: `list<'T>`, `Map<'T>`, `Set<'T>`.

It provides a number of customization options, allowing a wide range of JSON serialization formats.

## Documentation

* [How to use FSharp.SystemTextJson](docs/Using.md)

* [Serialization format](docs/Format.md)

* [Customizing the format](docs/Customizing.md)

## FAQ

* Does FSharp.SystemTextJson support alternative formats for unions?

[Yes!](docs/Customizing.md)

* Does FSharp.SystemTextJson support representing `'T option` as either just `'T` or `null` (or an absent field)?

Yes! Starting with v0.6, this is the default behavior.
To supersede it, use an explicit `JsonUnionEncoding` that does not include `UnwrapOption`.

* Does FSharp.SystemTextJson support `JsonPropertyNameAttribute` and `JsonIgnoreAttribute` on record fields?

Yes! It also provides [a more powerful `JsonNameAttribute`](docs/Customizing.md#jsonname) that supports non-string union tags.

* Does FSharp.SystemTextJson support options such as `PropertyNamingPolicy` and `IgnoreNullValues`?

Yes! It also supports naming policy [for union tags](docs/Customizing.md#uniontagnamingpolicy).

* Can I customize the format for a specific type?

[Yes!](docs/Customizing.md#how-to-apply-options)

* Does FSharp.SystemTextJson allocate memory?

As little as possible, but unfortunately the `FSharp.Reflection` API requires some allocations. In particular, an array is allocated for as many items as the record fields or union arguments, and structs are boxed. There is [work in progress](https://github.com/Tarmil/FSharp.SystemTextJson/pull/15) to improve this.

* Are there any benchmarks, e.g. against Newtonsoft.Json?

[Yes!](https://github.com/Tarmil/FSharp.SystemTextJson/pull/11)