# Customizing the serialization format

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [How to apply customizations](#how-to-apply-customizations)
- [`unionEncoding`](#unionencoding)
  - [Base encoding](#base-encoding)
    - [`AdjacentTag`](#adjacenttag)
    - [`ExternalTag`](#externaltag)
    - [`InternalTag`](#internaltag)
    - [`Untagged`](#untagged)
  - [Additional options](#additional-options)
    - [`NamedFields`](#namedfields)
    - [`UnwrapFieldlessTags`](#unwrapfieldlesstags)
    - [`UnwrapOption`](#unwrapoption)
    - [`UnwrapSingleCaseUnions`](#unwrapsinglecaseunions)
    - [`UnwrapSingleFieldCases`](#unwrapsinglefieldcases)
    - [`UnwrapRecordCases`](#unwraprecordcases)
    - [`UnionFieldNamesFromTypes`](#unionfieldnamesfromtypes)
    - [`AllowUnorderedTag`](#allowunorderedtag)
  - [Combined flags](#combined-flags)
- [`unionTagName`](#uniontagname)
- [`unionFieldsName`](#unionfieldsname)
- [`unionTagNamingPolicy`](#uniontagnamingpolicy)
- [`unionFieldNamingPolicy`](#unionfieldnamingpolicy)
- [`unionTagCaseInsensitive`](#uniontagcaseinsensitive)
- [`allowNullFields`](#allownullfields)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## How to apply customizations

The way to customize the serialization format depends on [how FSharp.SystemTextJson is used](Using.md).

* When passing a `JsonFSharpConverter` to the `JsonSerializerOptions` (the recommended way), the converter takes customization options as arguments.

    ```fsharp
    let options = JsonSerializerOptions()
    JsonFSharpConverter(
        unionEncoding = JsonUnionEncoding.InternalTag,
        unionTagName = "type"
    )
    |> options.Converters.Add
    ```
    
    Additionally, the format can be set for specific types.
    The argument `overrides` takes an `IDictionary<System.Type, JsonFSharpOptions>` that lists the customized types.
    `JsonFSharpOptions` is an object whose constructor takes the same arguments as `JsonFSharpConverter`.
    
    ```fsharp
    let options = JsonSerializerOptions()
    JsonFSharpConverter(
        // These options apply by default to all types:
        unionEncoding = JsonUnionEncoding.InternalTag,
        unionTagName = "type",
        // These options apply to specific types:
        overrides = dict [
            typeof<MySpecialUnion>, JsonFSharpOptions(unionTagName = "tag")
            typeof<AnotherUnion>, JsonFSharpOptions(unionEncoding = JsonUnionEncoding.Default)
        ]
    )
    ```
    
    Finally, passing the argument `allowOverride = true` allows the custom format to be overridden by a `JsonFSharpConverterAttribute` (see below).
    By default (ie when `allowOverride = false`), when using `JsonFSharpConverter`, `JsonFSharpConverterAttribute` is ignored.
    
* When using `JsonFSharpConverterAttribute`, the attribute itself takes customization options as arguments.

    ```fsharp
    [<JsonFSharpConverter(unionEncoding = JsonUnionEncoding.InternalTag)>]
    type MySpecialUnion =
        | MyCase of int
        | MyOtherCase of string
    ```

    Note that due to .NET limitations on the types of arguments that can be passed to an attribute, some options are unavailable for `JsonFSharpConverterAttribute`, such as `unionTagNamingPolicy`.

## `unionEncoding`

The customization option `unionEncoding` defines the format used to encode discriminated unions.
Its type is `JsonUnionEncoding`, and it is an enum of flags that can be combined using the binary "or" operator (`|||`).

A union encoding should be the combination of a [*base encoding*](#base-encoding) and any number of [*additional options*](#additional-options).
For example:

```fsharp
let options = JsonSerializerOptions()
JsonFSharpConverter(
    unionEncoding = (
        // Base encoding:
        JsonUnionEncoding.InternalTag
        // Additional options:
        ||| JsonUnionEncoding.UnwrapOption
        ||| JsonUnionEncoding.UnwrapRecordCases
    )
)
|> options.Converters.Add
```

Examples in this section will serialize values of the following type:

```fsharp
type Example =
    | NoArgs
    | WithOneArg of aFloat: float
    | WithArgs of anInt: int * aString: string
```

### Base encoding

There are four base encodings available.
These encodings and their names are inspired by Rust's excellent Serde library, although they differ in some specifics.

#### `AdjacentTag`

`JsonUnionEncoding.AdjacentTag` is the default format.

It represents unions similarly to the Json.NET library.
A union value is serialized into a JSON object with the following fields:

* A field `"Case"` whose value is a string representing the name of the union case;
* If the case has fields, a field `"Fields"` whose value is an array.

For example:

```fsharp
JsonSerializer.Serialize(NoArgs, options)
// --> {"Case":"NoArgs"}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"Case":"WithOneArg","Fields":[3.14]}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"Case":"WithArgs","Fields":[123,"Hello, world!"]}
```
    
The names `"Case"` and `"Fields"` can be customized (see [`unionTagName`](#uniontagname) and [`unionFieldsName`](#unionfieldsname) below).

#### `ExternalTag`

`JsonUnionEncoding.ExternalTag` represents unions similarly to FSharpLu.Json.
A union value is serialized as a JSON object with one field, whose name is the name of the union case, and whose value is an array.

```fsharp
JsonSerializer.Serialize(NoArgs, options)
// --> {"NoArgs":[]}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"WithOneArg":[3.14]}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"WithArgs":[123,"Hello, world!"]}
```

#### `InternalTag`

`JsonUnionEncoding.InternalTag` represents unions similarly to Thoth.Json.
A union value is serialized as a JSON array whose first item is the name of the case, and the rest are its fields.

```fsharp
JsonSerializer.Serialize(NoArgs, options)
// --> ["NoArgs"]

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> ["WithOneArg",3.14]

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> ["WithArgs",123,"Hello, world!"]
```

#### `Untagged`
    
`JsonUnionEncoding.Untagged` represents unions as an object whose fields have the names and values of the union's fields.
The name of the case is not encoded at all.
Deserialization is only possible if the fields of all cases have different names.

```fsharp
JsonSerializer.Serialize(NoArgs, options)
// --> {}

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> {"aFloat":3.14}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"anInt":123,"aString":"Hello world!"}
```

This flag also sets the `NamedFields` additional flag (see [below](#additional-options)).
    
### Additional options

#### `NamedFields`

`JsonUnionEncoding.NamedFields` causes the fields of a union to be encoded as a JSON object rather than an array.
The properties of the object are named after the value's fields (`aFloat`, `anInt` and `aString` in our example).
Its exact effect depends on the base format:

* `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields` replaces the array of case fields with an object.

    ```fsharp
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

* `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields` replaces the array of case fields with an object.

    ```fsharp
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

* `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields` replaces the entire value with an object.
    The property containing the case's name is named `"Case"`.

    ```fsharp
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

    The name `"Case"` can be customized (see [`unionTagName`](#uniontagname) below).

* `JsonUnionEncoding.Untagged` is unchanged by `JsonUnionEncoding.NamedFields`.

If a field doesn't have a name specified in F# code, then a default name is assigned by the compiler:
`Item` if the case has a single field, and `Item1`, `Item2`, etc if the case has multiple fields.
    
#### `UnwrapFieldlessTags`

`JsonUnionEncoding.UnwrapFieldlessTags` represents cases that don't have any fields as a simple string.

```fsharp
JsonSerializer.Serialize(NoArgs, options)
// --> "NoArgs"

JsonSerializer.Serialize(WithOneArg 3.14, options)
// --> (same format as without UnwrapFiledlessTags)

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> (same format as without UnwrapFieldlessTags)
```

#### `UnwrapOption`

`JsonUnionEncoding.UnwrapOption` is enabled by default.
It causes the types `'T option` and `'T voption` (aka `ValueOption`) to be treated specially.

* The value `None` or `ValueNone` is represented as `null`.
* The value `Some x` or `ValueSome x` is represented the same as `x`, without wrapping it in the union representation for `Some`.

Combined with the option `IgnoreNullValues` on `JsonSerializerOptions`, this can be used to represent optional fields: `Some` is a value that is present in the JSON object, and `None` is a value that is absent from the JSON object.
Note that the same effect can also be achieved more explicitly and more safely using [the `Skippable` type](#skippable).

#### `UnwrapSingleCaseUnions`
    
`JsonUnionEncoding.UnwrapSingleCaseUnions` is enabled by default.
It causes unions that have a single case with a single field to be treated as simple "wrappers", and serialized as their single field's value.

```fsharp
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

#### `UnwrapSingleFieldCases`

`JsonUnionEncoding.UnwrapSingleFieldCases`: if a union case has a single field, it is not wrapped in an array or object.
The exact effect depends on the base format:

* `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`:

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> (same format as without UnwrapSingleFieldCases)

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"Case":"WithOneArg","Fields":3.14}
    //                                   ^^^^
    //                        Instead of [3.14] or {"aFloat":3.14}

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> (same format as without UnwrapSingleCaseUnions)
    ```

* `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`:

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> (same format as without UnwrapSingleFieldCases)

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"WithOneArg":3.14}
    //                   ^^^^
    //        Instead of [3.14] or {"aFloat":3.14}

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> (same format as without UnwrapSingleCaseUnions)
    ```

* `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`: no effect.

* `JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapSingleFieldCases`: no effect.

#### `UnwrapRecordCases`
    
`JsonUnionEncoding.UnwrapRecordCases` implicitly sets `NamedFields` (see above).
If a union case has a single field which is a record, then this record's fields are used directly as the fields of the object representing the union.
The exact effect depends on the base format:

```fsharp
type Coordinates = { lat: float; long: float }

type Location =
    | Address of address: string
    | ExactLocation of Coordinates
```

* `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.UnwrapRecordCases`:

    ```fsharp
    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Case":"Address","Fields":{"address":"5 Avenue Anatole France"}}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"Case":"ExactLocation","Fields":{"lat":48.858,"long":2.295}}
    //                                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                           Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapRecordCases`:

    ```fsharp
    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Address":{"address":"5 Avenue Anatole France"}}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"ExactLocation":{"lat":48.858,"long":2.295}}
    //                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //           Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapRecordCases`:

    ```fsharp
    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"Case":"Address","address":"5 Avenue Anatole France"}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"Case":"ExactLocation","lat":48.858,"long":2.295}
    //                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                  Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

* `JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapRecordCases`:

    ```fsharp
    JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
    // --> {"address":"5 Avenue Anatole France"}
    // (same as without UnwrapRecordCases)

    JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 }, options)
    // --> {"lat":48.858,"long":2.295}
    // Instead of {"Item":{"lat":48.858,"long":2.295}}
    ```

#### `UnionFieldNamesFromTypes`

When using `NamedFields`, if a field doesn't have a name specified in F# code, then a default name is assigned by the compiler:
`Item` if the case has a single field, and `Item1`, `Item2`, etc if the case has multiple fields.
Using `UnionFieldNamesFromTypes`, the unnamed field is serialized using its type name instead.

If there are several fields with the same type, a suffix `1`, `2`, etc is added.

```fsharp
JsonFSharpConverter(
    JsonUnionEncoding.Default |||
    JsonUnionEncoding.InternalTag |||
    JsonUnionEncoding.NamedFields |||
    JsonUnionEncoding.UnionFieldNamesFromTypes)
|> options.Converters.Add

type Pair = Pair of int * string

JsonSerializer.Serialize(Pair(123, "test"), options)
// --> {"Case":"Pair","Int32":123,"String":"test"}
```


#### `AllowUnorderedTag`

`JsonUnionEncoding.AllowUnorderedTag` is enabled by default.
It takes effect during deserialization in AdjacentTag and InternalTag modes.
When it is disabled, the name of the case must be the first field of the JSON object.
When it is enabled, the name of the case may come later in the object, at the cost of a slight performance penalty if it does.

For example, without `AllowUnorderedTag`, the following will fail to parse:

```fsharp
JsonSerializer.Deserialize("""{"Fields":[3.14],"Case":"WithOneArg"}""", options)
// --> Error: Failed to find union case field for Example: expected Case
```

Whereas with `AllowUnorderedTag`, it will succeed:

```fsharp
JsonSerializer.Deserialize("""{"Fields":[3.14],"Case":"WithOneArg"}""", options)
// --> WithOneArg 3.14
```

### Combined flags

`JsonUnionEncoding` also contains a few items that combine several of the above flags.

* `JsonUnionEncoding.Default` represents the encoding that is used when `unionEncoding` is not specified.
    It is equivalent to:

    ```fsharp
    JsonUnionEncoding.AdjacentTag
    ||| JsonUnionEncoding.UnwrapOption
    ||| JsonUnionEncoding.UnwrapSingleCaseUnions
    ||| JsonUnionEncoding.AllowUnorderedTag
    ```

    It is particularly useful if you want to use the default encoding with some additional options, for example:

    ```fsharp
    JsonFSharpConverter(unionEncoding = (JsonUnionEncoding.Default ||| JsonUnionEncoding.UnwrapRecordCases))
    ```

* `JsonUnionEncoding.NewtonsoftLike` causes similar behavior to the library Json.NET (aka Newtonsoft.Json).
    It is equivalent to:

    ```fsharp
    JsonUnionEncoding.AdjacentTag
    ||| JsonUnionEncoding.AllowUnorderedTag
    ```

* `JsonUnionEncoding.ThothLike` causes similar behavior to the library [Thoth.Json](https://thoth-org.github.io/Thoth.Json/).
    It is equivalent to:

    ```fsharp
    JsonUnionEncoding.InternalTag
    ||| JsonUnionEncoding.UnwrapFieldlessTags
    ||| JsonUnionEncoding.AllowUnorderedTag
    ```

* `JsonUnionEncoding.FSharpLuLike` causes similar behavior to the library [FSharpLu.Json](https://github.com/microsoft/fsharplu/wiki/FSharpLu.Json) in Compact mode.
    It is equivalent to:

    ```fsharp
    JsonUnionEncoding.ExternalTag
    ||| JsonUnionEncoding.UnwrapFieldlessTags
    ||| JsonUnionEncoding.UnwrapOption
    ||| JsonUnionEncoding.UnwrapSingleFieldCases
    ||| JsonUnionEncoding.AllowUnorderedTag
    ```

## `unionTagName`

This option sets the name of the property that contains the union case.
This affects the base encodings `AdjacentTag` and `InternalTag ||| NamedFields`.
The default value is `"Case"`.

```fsharp
JsonFSharpConverter(unionTagName = "type")
|> options.Converters.Add

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"type":"WithArgs","Fields":[123,"Hello, world!"]}
```

## `unionFieldsName`

This option sets the name of the property that contains the union fields.
This affects the base encoding `AdjacentTag`.
The default value is `"Fields"`.

```fsharp
JsonFSharpConverter(unionFieldsName = "value")
|> options.Converters.Add

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"Case":"WithArgs","value":[123,"Hello, world!"]}
```

## `unionTagNamingPolicy`

This option sets the naming policy for union case names.
See [the System.Text.Json documentation about naming policies](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-customize-properties).

```fsharp
JsonFSharpConverter(unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
|> options.Converters.Add

JsonSerializer.Serialize(WithArgs(123, "Hello, world!"), options)
// --> {"Case":"withArgs","Fields":[123,"Hello, world!"]}
```

## `unionFieldNamingPolicy`

This option sets the naming policy for union field names.
See [the System.Text.Json documentation about naming policies](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-customize-properties).

If this option is not set, `JsonSerializerOptions.PropertyNamingPolicy` is used as the naming policy for union fields.

```fsharp
JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields,
    unionFieldNamingPolicy = JsonNamingPolicy.CamelCase)
|> options.Converters.Add

type Person = Person of FirstName: string * LastName: string

JsonSerializer.Serialize(Person("John", "Doe"), options)
// --> {"Case":"Person","firstName":"John","lastName":"Doe"}
```

## `unionTagCaseInsensitive`

This option only affects deserialization.
It makes the parsing of union case names case-insensitive.

```fsharp
JsonFSharpConverter(unionTagCaseInsensitive = true)
|> options.Converters.Add

JsonSerializer.Deserialize<Example>("""{"Case":"wIThArgS","Fields":[123,"Hello, world!"]}""", options)
// --> WithArgs (123, "Hello, world!")
```

## `allowNullFields`

By default, FSharp.SystemTextJson throws an exception when the following conditions are met:

* it is deserializing a record or a union;
* a field's JSON value is `null` or the field is unspecified;
* that field's type isn't an explicitly nullable F# type (like `option`, `voption` and `Skippable`).

With `allowNullFields = true`, such a null field is also allowed if the field's type is a class.

```fsharp
type Point() =
    member val X = 0. with get, set
    member val Y = 0. with get, set

type Rectangle = { BottomLeft: Point; TopRight: Point }

JsonFSharpConverter(allowNullFields = true)
|> options.Converters.Add

// With allowNullFields = false: throws an exception
JsonSerializer.Deserialize<Rectangle>("""{"TopRight":{"X":1,"Y":2}}""", options)

// With allowNullFields = true: succeeds
JsonSerializer.Deserialize<Rectangle>("""{"TopRight":{"X":1,"Y":2}}""", options)
// --> { BottomLeft = null; TopRight = Point(X = 1., Y = 2.) }
```
