# Customizing the serialization format

The serialization and deserialization of `FSharp.SystemTextJson` can be customized in two ways: using global options, and by adding attributes to specific types and properties.

<!-- doctoc --github docs/Customizing.md -->
<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

- [Global options](#global-options)
  - [How to apply options](#how-to-apply-options)
  - [Initial options](#initial-options)
  - [Base union encoding](#base-union-encoding)
    - [Adjacent tag](#adjacent-tag)
    - [External tag](#external-tag)
    - [Internal tag](#internal-tag)
    - [Untagged](#untagged)
    - [Base union encoding with `JsonFSharpConverterAttribute`](#base-union-encoding-with-jsonfsharpconverterattribute)
  - [Named union fields](#named-union-fields)
  - [Unwrap union cases without fields](#unwrap-union-cases-without-fields)
  - [Unwrap option values](#unwrap-option-values)
  - [Unwrap single-case unions](#unwrap-single-case-unions)
  - [Unwrap single-field union cases](#unwrap-single-field-union-cases)
  - [Unwrap union cases with a record field](#unwrap-union-cases-with-a-record-field)
  - [Infer union field names from their types](#infer-union-field-names-from-their-types)
  - [Allowing unordered tag](#allowing-unordered-tag)
  - [Union tag name](#union-tag-name)
  - [Union fields name](#union-fields-name)
  - [Union tag naming policy](#union-tag-naming-policy)
  - [Union field naming policy](#union-field-naming-policy)
  - [Union tag case-insensitive](#union-tag-case-insensitive)
  - [Include record properties](#include-record-properties)
  - [Skippable option fields](#skippable-option-fields)
  - [Allowing null fields](#allowing-null-fields)
  - [Changing the supported types](#changing-the-supported-types)
- [Attributes](#attributes)
  - [JsonFSharpConverter](#jsonfsharpconverter)
  - [JsonName](#jsonname)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Global options

### How to apply options

The way to customize the serialization format depends on [how FSharp.SystemTextJson is used](Using.md).

* When using `JsonFSharpOptions` (the recommended way), the options are customized using fluent methods.

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionTagName("type")
            .ToJsonSerializerOptions()
    ```

    Additionally, the format can be set for specific types.
    The method `WithOverrides` takes a function from `JsonFSharpOptions` to `IDictionary<System.Type, JsonFSharpOptions>` that lists the customized types.

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionTagName("type")
            .WithOverrides(fun options ->
                dict [
                    // Use the main options with a few changes:
                    typeof<MySpecialUnion>, options
                        .WithUnionTagName("tag")
                        .WithUnionFieldsName("args")

                    // Use completely new options:
                    typeof<AnotherUnion>, JsonFSharpOptions.FSharpLuLike()
                        .WithUnionAdjacentTag()

                    // Apply to a generic type:
                    typedefof<AGenericUnion<_>>, options
                        .WithUnionTagName("GenericCase")

                    // A specific override takes priority over a generic one:
                    typeof<AGenericUnion<string>>, options
                        .WithUnionTagName("SpecificCase")
                ])
            .ToJsonSerializerOptions()
    ```

    Finally, the method `WithAllowOverride()` allows the custom format to be overridden by a `JsonFSharpConverterAttribute` (see below).
    By default, when using `JsonFSharpOptions`, `JsonFSharpConverterAttribute` is ignored.

    Any option named `Foo` in this documentation is set using the method `.WithFoo()`, and unset with the method `.WithFoo(false)`.

* When using `JsonFSharpConverterAttribute`, the attribute itself takes customization options as mutable properties.

    ```fsharp
    [<JsonFSharpConverter(BaseUnionEncoding = JsonUnionEncoding.InternalTag)>]
    type MySpecialUnion =
        | MyCase of int
        | MyOtherCase of string
    ```

### Initial options

`JsonFSharpOptions` has a number of static methods that provide different baseline settings.

* `JsonFSharpOptions.Default()` is the default encoding.
  It is equivalent to:

    ```fsharp
    JsonFSharpOptions()
        .WithUnionAdjacentTag()
        .WithUnwrapOption()
        .WithUnionUnwrapSingleCaseUnions()
        .WithUnionAllowUnorderedTag()
    ```

* `JsonFSharpOptions.NewtonsoftLike()` causes similar behavior to the library Json.NET (aka Newtonsoft.Json).
  It is equivalent to:

    ```fsharp
    JsonFSharpOptions()
        .WithUnionAdjacentTag()
        .WithUnionAllowUnorderedTag()
    ```

* `JsonFSharpOptions.ThothLike()` causes similar behavior to the library [Thoth.Json's auto encoding](https://thoth-org.github.io/Thoth.Json/documentation/auto/json-representation.html#tuple-with-arguments).
  It is equivalent to:

    ```fsharp
    JsonFSharpOptions()
        .WithUnionInternalTag()
        .WithUnwrapFieldlessTags()
        .WithUnionAllowUnorderedTag()
    ```

* `JsonFSharpOptions.FSharpLuLike()` causes similar behavior to the library [FSharpLu.Json](https://github.com/microsoft/fsharplu/wiki/FSharpLu.Json) in Compact mode.
  It is equivalent to:

    ```fsharp
    JsonFSharpOptions()
        .WithUnionExternalTag()
        .WithUnionUnwrapFieldlessTags()
        .WithUnwrapOption()
        .WithUnionUnwrapSingleFieldCases()
        .WithUnionAllowUnorderedTag()
    ```

### Base union encoding

There are four base encodings available for F# unions.
These encodings and their names are inspired by Rust's excellent Serde library, although they differ in some specifics.

Examples in this section will serialize values of the following type:

```fsharp
type Example =
    | NoArgs
    | WithOneArg of aFloat: float
    | WithArgs of anInt: int * aString: string
```

#### Adjacent tag

`UnionAdjacentTag` is the default format.

It represents unions similarly to the Json.NET library.
A union value is serialized into a JSON object with the following fields:

* A field `"Case"` whose value is a string representing the name of the union case;
* If the case has fields, a field `"Fields"` whose value is an array.

For example:

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionAdjacentTag()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> {"Case":"NoArgs"}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"Case":"WithOneArg","Fields":[3.14]}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"Case":"WithArgs","Fields":[123,"Hello, world!"]}
```

The names `"Case"` and `"Fields"` can be customized (see [`unionTagName`](#union-tag-name) and [`unionFieldsName`](#union-fields-name) below).

#### External tag

`UnionExternalTag` represents unions similarly to FSharpLu.Json.
A union value is serialized as a JSON object with one field, whose name is the name of the union case, and whose value is an array.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionExternalTag()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> {"NoArgs":[]}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"WithOneArg":[3.14]}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"WithArgs":[123,"Hello, world!"]}
```

#### Internal tag

`UnionInternalTag` represents unions similarly to [Thoth.Json's auto encoding](https://thoth-org.github.io/Thoth.Json/documentation/auto/json-representation.html#tuple-with-arguments).
A union value is serialized as a JSON array whose first item is the name of the case, and the rest are its fields.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionInternalTag()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> ["NoArgs"]

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> ["WithOneArg",3.14]

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> ["WithArgs",123,"Hello, world!"]
```

#### Untagged

`UnionUntagged` represents unions as an object whose fields have the names and values of the union's fields.
The name of the case is not encoded at all.
Deserialization is only possible if the fields of all cases have different names.

```fsharp
let options = 
    JsonFSharpOptions.Default()
        .WithUnionUntagged()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> {}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"aFloat":3.14}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"anInt":123,"aString":"Hello world!"}
```

This flag also sets the `NamedFields` additional flag (see [below](#named-union-fields)).

#### Base union encoding with `JsonFSharpConverterAttribute`

The base union encoding on a type using `JsonFSharpConverterAttribute` is set using the property `BaseUnionEncoding` of type `JsonUnionEncoding`.

```fsharp
[<JsonFSharpConverter(BaseUnionEncoding = JsonUnionEncoding.ExternalTag)>]
type Example =
    | NoArgs
    | WithOneArg of aFloat: float
    | WithArgs of anInt: int * aString: string
```

### Named union fields

`UnionNamedFields` causes the fields of a union to be encoded as a JSON object rather than an array.
The properties of the object are named after the value's fields (`aFloat`, `anInt` and `aString` in our example).
Its exact effect depends on the base format:

* With `UnionAdjacentTag`, it replaces the array of case fields with an object.

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionAdjacentTag()
            .WithUnionNamedFields()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(NoArgs, options)
    // --> {"Case":"NoArgs"}
    // (same format as without NamedFields)

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"Case":"WithOneArg","Fields":{"aFloat":3.14}}
    //                                   ^^^^^^^^^^^^^^
    //                        Instead of [3.14]

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> {"Case":"WithArgs","Fields":{"anInt":123,"aString":"Hello, world!"}}
    //                                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                      Instead of [123,"Hello, world!"]
    ```

* With `UnionExternalTag`, it replaces the array of case fields with an object.

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionExternalTag()
            .WithUnionNamedFields()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(NoArgs, options)
    // --> {"NoArgs":{}}
    //               ^^
    //    Instead of []

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"WithOneArg":{"aFloat":3.14}}
    //                   ^^^^^^^^^^^^^^
    //        Instead of [3.14]

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> {"WithArgs":{"anInt":123,"aString":"Hello, world!"}}
    //                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //      Instead of [123,"Hello, world!"]
    ```

* With `UnionInternalTag`, it replaces the entire value with an object.
    The property containing the case's name is named `"Case"`.

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionNamedFields()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(NoArgs, options)
    // --> {"Case":"NoArgs"}
    // Instead of ["NoArgs"]

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"Case":"WithOneArg","aFloat":3.14}
    // Instead of ["WithOneArg",3.14]

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> {"Case":"WithArgs","anInt":123,"aString":"Hello, world!"}
    // Instead of ["WithArgs",123,"Hello, world!"]
    ```

    The name `"Case"` can be customized (see [`UnionTagName`](#union-tag-name) below).

* `UnionUntagged` is unchanged by `UnionNamedFields`.

If a field doesn't have a name specified in F# code, then a default name is assigned by the compiler:
`Item` if the case has a single field, and `Item1`, `Item2`, etc if the case has multiple fields.

### Unwrap union cases without fields

`UnionUnwrapFieldlessTags` represents union cases that don't have any fields as a simple string.

```fsharp
let options = 
    JsonFSharpOptions.Default()
        .WithUnionUnwrapFieldlessTags()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> "NoArgs"

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> (same format as without UnwrapFiledlessTags)

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> (same format as without UnwrapFieldlessTags)
```

Additionally, when this flag is active, enum-like unions, ie unions where no cases have any fields, can be used as dictionary keys.

```fsharp
type Color = Red | Green | Blue

let options =
    JsonFSharpOptions.Default()
        .WithUnionUnwrapFieldlessTags()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(dict [ (Red, 1); (Blue, 2) ], options)
// --> {"Red":1,"Blue":2}
```

### Unwrap option values

`UnwrapOption` is enabled by default.
It causes the types `'T option` and `'T voption` (aka `ValueOption`) to be treated specially.

* The value `None` or `ValueNone` is represented as `null`.
* The value `Some x` or `ValueSome x` is represented the same as `x`, without wrapping it in the union representation for `Some`.

To represent `None` or `ValueNone` as the absence of a field instead, see [Skippable option fields](#skippable-option-fields) or [the Skippable type](Format.md#skippable).

### Unwrap single-case unions

`UnionUnwrapSingleCaseUnions` is enabled by default.
It causes unions that have a single case with a single field to be treated as simple "wrappers", and serialized as their single field's value.

```fsharp
let options = 
    JsonFSharpOptions.Default()
        .WithUnionUnwrapSingleCaseUnions()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(NoArgs, options)
// --> (same format as without UnwrapSingleCaseUnions)

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> (same format as without UnwrapSingleCaseUnions)

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> (same format as without UnwrapSingleCaseUnions)

type UserId = UserId of string

JsonSerializer.Serialize(UserId "tarmil", options)
// --> "tarmil"
```

### Unwrap single-field union cases

`UnionUnwrapSingleFieldCases`: if a union case has a single field, it is not wrapped in an array or object.
The exact effect depends on the base format:

* With `UnionAdjacentTag`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionAdjacentTag()
            .WithUnionUnwrapSingleFieldCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(NoArgs, options)
    // --> (same format as without UnwrapSingleFieldCases)

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"Case":"WithOneArg","Fields":3.14}
    //                                   ^^^^
    //                        Instead of [3.14] or {"aFloat":3.14}

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> (same format as without UnwrapSingleCaseUnions)
    ```

* With `UnionExternalTag`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionExternalTag()
            .WithUnionUnwrapSingleFieldCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(NoArgs, options)
    // --> (same format as without UnwrapSingleFieldCases)

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"WithOneArg":3.14}
    //                   ^^^^
    //        Instead of [3.14] or {"aFloat":3.14}

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> (same format as without UnwrapSingleCaseUnions)
    ```

* With `UnionInternalTag`: no effect.

* With `UnionUntagged`: no effect.

### Unwrap union cases with a record field

`UnionUnwrapRecordCases` implicitly sets [`NamedFields`](#named-union-fields).
If a union case has a single field which is a record, then this record's fields are used directly as the fields of the object representing the union.
The exact effect depends on the base format:

```fsharp
type Coordinates = { lat: float; long: float }

type Location =
    | Address of address: string
    | ExactLocation of Coordinates
```

* With `UnionAdjacentTag`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionAdjacentTag()
            .WithUnionUnwrapRecordCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Case":"Address","Fields":{"address":"5 Avenue Anatole France"}}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"Case":"ExactLocation","Fields":{"lat":48.858,"long":2.295}}
    //                                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                           Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* With `UnionExternalTag`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionExternalTag()
            .WithUnionUnwrapRecordCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Address":{"address":"5 Avenue Anatole France"}}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"ExactLocation":{"lat":48.858,"long":2.295}}
    //                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //           Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* With `UnionInternalTag`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionUnwrapRecordCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Case":"Address","address":"5 Avenue Anatole France"}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"Case":"ExactLocation","lat":48.858,"long":2.295}
    //                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                  Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* With `UnionUntagged`:

    ```fsharp
    let options =
        JsonFSharpOptions.Default()
            .WithUnionUntagged()
            .WithUnionUnwrapRecordCases()
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"address":"5 Avenue Anatole France"}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"lat":48.858,"long":2.295}
    // Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

### Infer union field names from their types

When using `UnionNamedFields`, if a field doesn't have a name specified in F# code, then a default name is assigned by the compiler:
`Item` if the case has a single field, and `Item1`, `Item2`, etc if the case has multiple fields.
Using `UnionFieldNamesFromTypes`, the unnamed field is serialized using its type name instead.

If there are several fields with the same type, a suffix `1`, `2`, etc is added.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionInternalTag()
        .WithUnionNamedFields()
        .WithUnionFieldNamesFromTypes()
        .ToJsonSerializerOptions()

type Pair = Pair of int * string

JsonSerializer.Serialize(Pair(123, "test"), options)
// --> {"Case":"Pair","Int32":123,"String":"test"}
```

### Allowing unordered tag

`UnionAllowUnorderedTag` is enabled by default.
It takes effect during deserialization in `UnionAdjacentTag` and `UnionInternalTag` modes:

* When it is disabled, the name of the case must be the first field of the JSON object.
* When it is enabled, the name of the case may come later in the object, at the cost of a slight performance penalty if it does.

For example, without `UnionAllowUnorderedTag`, the following will fail to parse:

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionAllowUnorderedTag(false)
        .ToJsonSerializerOptions()

JsonSerializer.Deserialize("""{"Fields":[3.14],"Case":"WithOneArg"}""", options)
// --> Error: Failed to find union case field for Example: expected Case
```

Whereas with `UnionAllowUnorderedTag`, it will succeed:

```fsharp
let options =
    JsonFSharpOptions.Default()
        .ToJsonSerializerOptions()

JsonSerializer.Deserialize("""{"Fields":[3.14],"Case":"WithOneArg"}""", options)
// --> WithOneArg 3.14
```

### Union tag name

The option `UnionTagName` sets the name of the property that contains the union case.
This affects the base encodings `UnionAdjacentTag` and `UnionInternalTag` with `UnionNamedFields`.
The default value is `"Case"`.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionTagName("type")
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"type":"WithArgs","Fields":[123,"Hello, world!"]}
```

### Union fields name

The option `UnionFieldsName` sets the name of the property that contains the union fields.
This affects the base encoding `UnionAdjacentTag`.
The default value is `"Fields"`.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionFieldsName("value")
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"Case":"WithArgs","value":[123,"Hello, world!"]}
```

### Union tag naming policy

The option `UnionTagNamingPolicy` sets the naming policy for union case names.
See [the System.Text.Json documentation about naming policies](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-customize-properties).

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionTagNamingPolicy(JsonNamingPolicy.CamelCase)
        .ToJsonSerializerOptions()

JsonSerializer.Serialize(WithArgs(123, "Hello, world!"), options)
// --> {"Case":"withArgs","Fields":[123,"Hello, world!"]}
```

When using the attribute, this option has enum type `JsonKnownNamingPolicy` instead.

### Union field naming policy

The option `UnionFieldNamingPolicy` sets the naming policy for union field names.
See [the System.Text.Json documentation about naming policies](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-customize-properties).

If this option is not set, `JsonSerializerOptions.PropertyNamingPolicy` is used as the naming policy for union fields.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionFieldNamingPolicy(JsonNamingPolicy.CamelCase)
        .ToJsonSerializerOptions()

type Person = Person of FirstName: string * LastName: string

JsonSerializer.Serialize(Person("John", "Doe"), options)
// --> {"Case":"Person","firstName":"John","lastName":"Doe"}
```

When using the attribute, this option has enum type `JsonKnownNamingPolicy` instead.

### Union tag case-insensitive

The option `UnionTagCaseInsensitive` only affects deserialization.
It makes the parsing of union case names case-insensitive.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionTagCaseInsensitive()
        .ToJsonSerializerOptions()

JsonSerializer.Deserialize<Example>("""{"Case":"wIThArgS","Fields":[123,"Hello, world!"]}""", options)
// --> WithArgs (123, "Hello, world!")
```

### Include record properties

By default, only record fields are serialized. When `IncludeRecordProperties` is set, record properties are serialized as well.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithIncludeRecordProperties()
        .ToJsonSerializerOptions()

type Rectangle =
    { Width: float
      Height: float }

    member this.Area = this.Width * this.Height

JsonSerializer.Serialize({ Width = 4.; Height = 5. }, options)
// --> {"Width":4,"Height":5,"Area":20}
```

Deserialization is unaffected by `IncludeRecordProperties`.

To include a specific record property in serialization, rather than all of properties of all records, use `JsonIncludeAttribute` on this property.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .ToJsonSerializerOptions()

type Rectangle =
    { Width: float
      Height: float }

    [<JsonInclude>]
    member this.Area = this.Width * this.Height

    member this.Perimeter = 2. * (this.Width + this.Height)

JsonSerializer.Serialize({ Width = 4.; Height = 5. }, options)
// --> {"Width":4,"Height":5,"Area":20}
```

### Skippable option fields

The option `SkippableOptionFields` allows record and union fields of type `option` or `voption` to behave similarly to the type [`Skippable`](Format.md#skippable):

* `None` / `ValueNone` is represented as a missing JSON field;
* `Some` / `ValueSome` is represented as an actual JSON field.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithSkippableOptionFields()
        .ToJsonSerializerOptions()

type Range =
    {
        min: int
        max: int option
    }

let betweenOneAndTwo = { min = 1; max = Some 2 }
JsonSerializer.Serialize(betweenOneAndTwo, options)
// --> {"min":1,"max":2}

let fromThreeToInfinity = { min = 3; max = None }
JsonSerializer.Serialize(fromThreeToInfinity, options)
// --> {"min":3}
```

### Allowing null fields

By default, FSharp.SystemTextJson throws an exception when the following conditions are met:

* it is deserializing a record or a union;
* a field's JSON value is `null` or the field is unspecified;
* that field's type isn't an explicitly nullable F# type (like `option`, `voption` and `Skippable`).

With the option `AllowNullFields`, such a null field is also allowed if the field's type is a class.

```fsharp
type Point() =
    member val X = 0. with get, set
    member val Y = 0. with get, set

type Rectangle = { BottomLeft: Point; TopRight: Point }

let options =
    JsonFSharpOptions.Default()
        .WithAllowNullFields()
        .ToJsonSerializerOptions()

// With allowNullFields = false: throws an exception
JsonSerializer.Deserialize<Rectangle>("""{"TopRight":{"X":1,"Y":2}}""", options)

// With allowNullFields = true: succeeds
JsonSerializer.Deserialize<Rectangle>("""{"TopRight":{"X":1,"Y":2}}""", options)
// --> { BottomLeft = null; TopRight = Point(X = 1., Y = 2.) }
```

### Changing the supported types

Since the first release of `FSharp.SystemTextJson`, the base library `System.Text.Json` has added support for a number of F# types, including:

* Records;
* Lists;
* Sets;
* Maps. Only primitive keys are supported.
* Options and value options. They are serialized as `null` for `None`/`ValueNone`, and the wrapped value directly for `Some`/`ValueSome`.
  This is the same as `FSharp.SystemTextJson`'s default format, but unlike `FSharp.SystemTextJson`, it is not customizable.

`FSharp.SystemTextJson` still takes over the serialization of these types by befault; but the option `Types: JsonFSharpTypes` allows customizing which types should be serialized by `FSharp.SystemTextJson`, and which types should be left to `System.Text.Json`.
It is an enum of flags:

* `Records`: serialize and deserialize records, struct records and anonymous records.
* `Unions`: serialize and deserialize discriminated unions and struct discriminated unions.
* `Tuples`: serialize and deserialize tuples, including those with more than 7 items which are not considered a single tuple by System.Text.Json.
* `Lists`: serialize and deserialize F# lists.
* `Sets`: serialize and deserialize F# sets.
* `Maps`: serialize and deserialize F# maps, including maps with complex key types, which are not supported by System.Text.Json.
* `Options`: serialize and deserialize the F# option type.
* `ValueOptions`: serialize and deserialize the F# voption type.

It also includes a few combined flags:

* `OptionalTypes`: Options and ValueOptions.
* `Collections`: lists, sets and maps.
* `Minimal`: All types not already fully supported by `System.Text.Json`. As of `FSharp.SystemTextJson` on .NET 6, this includes unions, maps (for complex key types) and tuples (for tuples of more than 7 items).
* `All`: this is the default, `FSharp.SystemTextJson` handles all the types it supports.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithTypes(JsonFSharpTypes.Unions ||| JsonFSharpTypes.OptionalTypes)
        .ToJsonSerializerOptions()

type Point = { x: int; y: int }

JsonSerializer.Serialize({ x = 1; y = 2 }, options)
// The above serialization is handled by System.Text.Json's default handling of F# records,
// and not by FSharp.SystemTextJson.
```

## Attributes

### JsonFSharpConverter

The attribute `JsonFSharpConverterAttribute` on a type indicates that this type must be serialized using `FSharp.SystemTextJson`.
See [Using attributes](Using.md#using-attributes) for applying it, and [How to apply options](#how-to-apply-options) for specializing global options on a type using this attribute.

### JsonName

`FSharp.SystemTextJson` supports the standard `JsonPropertyNameAttribute` to define the name of a property in JSON.

```fsharp
type Example =
    { [<JsonPropertyName "thisIsX">] x: string
      y: string }

JsonSerializer.Serialize({ x = "Hello"; y = "world!" }, options)
// --> {"thisIsX":"Hello","y":"world!"}
```

However, it also includes its own attribute `JsonNameAttribute` which provides more functionality.

* It can be used exactly like `JsonPropertyNameAttribute`:

    ```fsharp
    type Example =
        { [<JsonName "thisIsX">] x: string
          y: string }

    JsonSerializer.Serialize({ x = "Hello"; y = "world!" }, options)
    // --> {"thisIsX":"Hello","y":"world!"}
    ```

* It can be used with multiple values.
  In this case, all the values are recognized as field names for deserialization.
  The first value is used for serialization.

    ```fsharp
    type Example =
        { [<JsonName("thisIsX", "reallyX")>] x: string
          y: string }

    JsonSerializer.Deserialize("""{"thisIsX":"Hello","y":"world!"}""", options)
    // --> { x = "Hello"; y = "world!" }

    JsonSerializer.Deserialize("""{"reallyX":"Hello","y":"world!"}""", options)
    // --> { x = "Hello"; y = "world!" }

    JsonSerializer.Serialize({ x = "Hello"; y = "world!" }, options)
    // --> {"thisIsX":"Hello","y":"world!"}
    ```

* It can be used on a discriminated union case to determine the tag of this case.
  In this situation, the value can be a string, an integer or a boolean.

    ```fsharp
    type Example =
        | [<JsonName 1>] One of int
        | [<JsonName 2>] Two of string

    JsonSerializer.Serialize(Two "hello", options)
    // --> {"Case":2,"Fields":["hello"]}
    ```

    Combined with `UnionInternalTag`, this is a way to encode the common pattern in JSON where the value of one field determines what other fields are allowed:

    ```fsharp
    type MyResult<'t> =
        | [<JsonName false>] Error of message: string
        | [<JsonName true>] Ok of 't

    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionNamedFields()
            .WithUnionTagName("isSuccess")
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Ok {| x = 1; y = "hello" |}, options)
    // --> {"isSuccess":true,"x":1,"y":"hello"}

    JsonSerializer.Serialize(Error "Failed to retrieve x", options)
    // --> {"isSuccess":false,"message":"Failed to retrieve x"}
    ```

* Using the `Field` property, it can be used to indicate the name(s) of a discriminated case field.

    ```fsharp
    type MyResult<'t> =
        | [<JsonName false>]
          [<JsonName("error", "errorMessage", Field = "message")]
          Error of message: string
        | [<JsonName true>] Ok of 't

    let options =
        JsonFSharpOptions.Default()
            .WithUnionInternalTag()
            .WithUnionNamedFields()
            .WithUnionTagName("isSuccess")
            .ToJsonSerializerOptions()

    JsonSerializer.Serialize(Error "Failed to retrieve x", options)
    // --> {"isSuccess":false,"error":"Failed to retrieve x"}
    ```
