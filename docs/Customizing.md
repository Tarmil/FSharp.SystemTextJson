# Customizing the serialization format

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [How to apply customizations](#how-to-apply-customizations)
- [`unionEncoding`](#unionencoding)
  - [Base encoding](#base-encoding)
  - [Additional options](#additional-options)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## How to apply customizations

The way to customize the serialization format depends on [how FSharp.SystemTextJson is used](Using.md).

* When passing an `JsonFSharpConverter` to the `JsonSerializerOptions` (the recommended way), the converter takes customization options as arguments.

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

## `unionEncoding`

The customization option `unionEncoding` defines the format used to encode discriminated unions.
Its type is `JsonUnionEncoding`, and it is an enum of flags that can be combined using the binary "or" operator (`|||`).

A union encoding should be the combination of a *base encoding* and any number of *additional options*.

Examples in this section will serialize values of the following type:

```fsharp
type Example =
    | NoArgs
    | WithOneArg of aFloat: float
    | WithArgs of anInt: int * aString: string
```

### Base encoding

There are four base encodings available:

* `JsonUnionEncoding.AdjacentTag` is the default format.

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

* `JsonUnionEncoding.ExternalTag` represents unions similarly to FSharpLu.Json.
    A union value is serialized as a JSON object with one field, whose name is the name of the union case, and whose value is an array.
    
    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {"NoArgs":[]}
    
    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"WithOneArg":[3.14]}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> {"WithArgs":[123,"Hello, world!"]}
    ```
    
* `JsonUnionEncoding.InternalTag` represents unions similarly to Thoth.Json.
    A union value is serialized as a JSON array whose first item is the name of the case, and the rest are its fields.
    
    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> ["NoArgs"]
    
    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> ["WithOneArg",3.14]
    
    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> ["WithArgs",123,"Hello, world!"]
    ```
    
* `JsonUnionEncoding.Untagged` represents unions as an object whose fields have the names and values of the union's fields.
    The name of the case is not encoded at all.
    Deserialization is only possible if the fields of all cases have different names.
    
    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {}

    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> {"aBool":3.14}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> {"anInt":123,"aString":"Hello world!"}
    ```
    
    This flag also sets the `NamedFields` additional flag (see [below](#additional-options)).
    
### Additional options

* `JsonUnionEncoding.NamedFields` causes the fields of a union to be encoded as a JSON object rather than an array.
    The properties of the object are named after the value's fields (`anInt` and `aString` in our example).
    Its exact effect depends on the base format:
    
    * `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields` replaces the array of case fields with an object.
    
        ```fsharp
        JsonSerializer.Serialize(NoArgs, options)
        // --> {"Case":"NoArgs"}
        
        JsonSerializer.Serialize(WithOneArg 3.14, options)
        // --> {"Case":"WithOneArg","Fields":{"aBool":3.14}}

        JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
        // --> {"Case":"WithArgs","Fields":{"anInt":123,"aString":"Hello, world!"}}
        ```
        
    * `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields` replaces the array of case fields with an object.
    
        ```fsharp
        JsonSerializer.Serialize(NoArgs, options)
        // --> {"NoArgs":{}}
        
        JsonSerializer.Serialize(WithOneArg 3.14, options)
        // --> {"WithOneArg":{"aBool":3.14}}

        JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
        // --> {"WithArgs":{"anInt":123,"aString":"Hello, world!"}}
        ```
        
    * `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields` replaces the entire value with an object.
        The property containing the case's name is named `"Case"`.
        
        ```fsharp
        JsonSerializer.Serialize(NoArgs, options)
        // --> {"Case":"NoArgs"}
        
        JsonSerializer.Serialize(WithOneArg 3.14, options)
        // --> {"Case":"WithOneArg","aBool":3.14}

        JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
        // --> {"Case":"WithArgs","anInt":123,"aString":"Hello, world!"}
        ```

        The name `"Case"` can be customized (see [`unionTagName`](#uniontagname) below).
        
    * `JsonUnionEncoding.Untagged` is unchanged by `JsonUnionEncoding.NamedFields`.
    
* `JsonUnionEncoding.UnwrapFieldlessTags` represents cases that don't have any fields as a simple string.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> "NoArgs"
    
    JsonSerializer.Serialize(WithOneArg 3.14, options)
    // --> (same format as without UnwrapFiledlessTags)

    JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
    // --> (same format as without UnwrapFieldlessTags)
    ```
    
* `JsonUnionEncoding.UnwrapOption` is enabled by default.
    It causes the types `'T option` and `'T voption` (aka `ValueOption`) to be treated specially.

    * The value `None` or `ValueNone` is represented as `null`.
    * The value `Some x` or `ValueSome x` is represented the same as `x`, without wrapping it in the union representation for `Some`.
    
    Combined with the option `IgnoreNullValues` on `JsonSerializerOptions`, this can be used to represent optional fields: `Some` is a value that is present in the JSON object, and `None` is a value that is absent from the JSON object.
    Note that the same effect can also be achieved more explicitly and more safely using [the `Skippable` type](#skippable).
    
* `JsonUnionEncoding.UnwrapSingleCaseUnions` is enabled by default.
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
    
* `JsonUnionEncoding.UnwrapSingleFieldCases`: if a union case has a single field, it is not wrapped in an array or object.
    The exact effect depends on the base format:
    
    * `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`:
    
        ```fsharp
        JsonSerializer.Serialize(NoArgs, options)
        // --> (same format as without UnwrapSingleFieldCases)
        
        JsonSerializer.Serialize(WithOneArg 3.14, options)
        // --> {"Case":"WithOneArg","Fields":3.14}

        JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
        // --> (same format as without UnwrapSingleCaseUnions)
        ```
        
    * `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`:
    
        ```fsharp
        JsonSerializer.Serialize(NoArgs, options)
        // --> (same format as without UnwrapSingleFieldCases)
        
        JsonSerializer.Serialize(WithOneArg 3.14, options)
        // --> {"WithOneArg":3.14}

        JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
        // --> (same format as without UnwrapSingleCaseUnions)
        ```
        
    * `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapSingleFieldCases`: no effect.
    
    * `JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapSingleFieldCases`: no effect.
    
* `JsonUnionEncoding.UnwrapRecordCases` implicitly sets `NamedFields` (see above).
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
        
        JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 })
        // --> {"Case":"ExactLocation","Fields":{"lat":48.858,"long":2.295}}
        ```
        
    * `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapRecordCases`:
    
        ```fsharp
        JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
        // --> {"Address":{"address":"5 Avenue Anatole France"}}
        
        JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 })
        // --> {"ExactLocation":{"lat":48.858,"long":2.295}}
        ```
        
    * `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapRecordCases`:
    
        ```fsharp
        JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
        // --> {"Case":"Address","address":"5 Avenue Anatole France"}
        
        JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 })
        // --> {"Case":"ExactLocation","lat":48.858,"long":2.295}
        ```
        
    * `JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapRecordCases`:
    
        ```fsharp
        JsonSerializer.Serialize(Address "5 Avenue Anatole France", options)
        // --> {"address":"5 Avenue Anatole France"}
        
        JsonSerializer.Serialize(ExactLocation { lat = 48.858; long = 2.295 })
        // --> {"lat":48.858,"long":2.295}
        ```

<!-- TODO Other options -->