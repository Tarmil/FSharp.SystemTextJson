# FSharp.SystemTextJson

[![Build status](https://ci.appveyor.com/api/projects/status/mf903n55gsiop0mh/branch/master?svg=true)](https://ci.appveyor.com/project/Tarmil/fsharp-systemtextjson/branch/master)
[![Nuget](https://img.shields.io/nuget/v/FSharp.SystemTextJson?logo=nuget)](https://nuget.org/packages/FSharp.SystemTextJson)

This library provides F# union and record support for [System.Text.Json](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/).

## Usage

The NuGet package is [`FSharp.SystemTextJson`](https://nuget.org/packages/FSharp.SystemTextJson). However the namespace is `System.Text.Json.Serialization` like the base library.

There are two ways to use `FSharp.SystemTextJson`: apply it to all F# types by passing `JsonSerializerOptions`, or apply it to specific types with an attribute.

### Using options

Add `JsonFSharpConverter` to the converters in `JsonSerializerOptions`, and the format will be applied to all F# types.

```fsharp
open System.Text.Json
open System.Text.Json.Serialization

let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter())

JsonSerializer.Serialize({| x = "Hello"; y = "world!" |}, options)
// --> {"x":"Hello","y":"world!"}
```

### Using attributes

Add `JsonFSharpConverterAttribute` to the type that needs to be serialized.

```fsharp
open System.Text.Json
open System.Text.Json.Serialization

[<JsonFSharpConverter>]
type Example = { x: string; y: string }

JsonSerializer.Serialize({ x = "Hello"; y = "world!" })
// --> {"x":"Hello","y":"world!"}
```

### Advantages and inconvenients

The options way is generally recommended because it applies the format to all F# types. In addition to your defined types, this also includes:

* Types defined in referenced libraries that you can't modify to add an attribute. This includes standard library types such as `option` and `Result`.
* Anonymous records.

The attribute way cannot handle the above cases.

The advantage of the attribute way is that it allows calling `Serialize` and `Deserialize` without having to pass options every time. This is particularly useful if you are passing your own data to a library that calls these functions itself and doesn't take options.

## Using with ASP.NET Core

ASP.NET Core can be easily configured to use FSharp.SystemTextJson.

### ASP.NET Core MVC

To use F# types in MVC controllers, add the following to your startup `ConfigureServices`:

```fsharp
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddControllersWithViews() // or whichever method you're using to get an IMvcBuilder
            .AddJsonOptions(fun options ->
                options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter()))
        |> ignore
```

And you can then just do:

```fsharp
type MyTestController() =
    inherit Controller()

    member this.AddOne([<FromBody>] msg: {| value: int |}) =
        {| value = msg.value + 1 |}
```

### SignalR

To use F# types in SignalR hubs, add the following to your startup `ConfigureServices`:

```fsharp
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddSignalR()
            .AddJsonProtocol(fun options ->
                options.PayloadSerializerOptions.Converters.Add(JsonFSharpConverter()))
        |> ignore
```

And you can then just do:

```fsharp
type MyHub() =
    inherit Hub()

    member this.AddOne(msg: {| value: int |})
        this.Clients.All.SendAsync("AddedOne", {| value = msg.value + 1 |})
```

## Format

### Lists

F# lists are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize([1; 2; 3], options)
// --> [1,2,3]
```

### Sets

F# sets are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize(Set [1; 2; 3], options)
// --> [1,2,3]
```

### Maps

F# string-keyed maps are serialized as JSON objects.

```fsharp
JsonSerializer.Serialize(Map [("a", 1); ("b", 2); ("c", 3)], options)
// --> {"a":1,"b":2,"c":3}
```

Maps with other types as keys are serialized as JSON arrays, where each item is a `[key,value]` array.

```fsharp
JsonSerializer.Serialize(Map [(1, "a"); (2, "b"); (3, "c")], options)
// --> [[1,"a"],[2,"b"],[3,"c"]]
```

### Tuples

Tuples and struct tuples are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize((1, "abc"), options)
// --> [1,"abc"]

JsonSerializer.Serialize(struct (1, "abc"), options)
// --> [1,"abc"]
```

### Records and anonymous records

Records and anonymous records are serialized as JSON objects.

```fsharp
type Example = { x: string; y: string }

JsonSerializer.Serialize({ x = "Hello"; y = "world!" }, options)
// --> {"x":"Hello","y":"world!"}

JsonSerializer.Serialize({| x = "Hello"; y = "world!" |}, options)
// --> {"x":"Hello","y":"world!"}
```

Named record fields are serialized in the order in which they were declared in the type declaration.

Anonymous record fields are serialized in alphabetical order.

### Unions

Unions can be serialized in a number of formats. The enum `JsonUnionEncoding` defines the format to use; you can pass a value of this type to the constructor of `JsonFSharpConverter` or to the `JsonFSharpConverter` attribute.

```fsharp
// Using options:
let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.BareFieldlessTags))

// Using attributes:
[<JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.BareFieldlessTags)>]
type MyUnion = // ...
```

Some of the encoding styles for Unions write their union case name to a configurable tag. To configure this, `JsonFSharpConverter` accepts an argument called `unionTagName`. This tag defaults to `"Case"`, and can be configured like so:

```fsharp
// Using options:
let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields, "alternative-case-tag"))

// Using attributes:
[<JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields, "alternative-case-tag")>]
type MyUnion = // ...
```

Note that when using the default `JsonUnionEncoding`, `unionTagName` needs to be passed by key, rather than by position, e.g.:

``` fsharp
[<JsonFSharpConverter(unionTagName="alternative-case-tag")>]
type MyUnion = // ...
```

Here are the possible values:

* `JsonUnionEncoding.AdjacentTag` is the default format. It represents unions as a JSON object with two fields:

    * A tag, defaulting to `"Case"`: a string whose value is the name of the union case.
    * `"Fields"`: an array whose items are the arguments of the union case. This field is absent if the union case has no arguments.

    This is the same format used by Newtonsoft.Json.

    ```fsharp
    type Example =
        | WithArgs of anInt: int * aString: string
        | NoArgs

    JsonSerializer.Serialize(NoArgs, options)
    // --> {"Case":"NoArgs"}

    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"Case":"WithArgs","Fields":[123,"Hello world!"]}
    ```

* `JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields` is similar, except that the fields are represented as an object instead of an array. The field names on this object are the names of the arguments.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {"Case":"NoArgs"}

    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"Case":"WithArgs","Fields":{"anInt":123,"aString":"Hello world!"}}
    ```

    Note that if an argument doesn't have an explicit name, F# automatically gives it the name `Item` (if it's the only argument of its case) or `Item1`/`Item2`/etc (if the case has multiple arguments).

* `JsonUnionEncoding.ExternalTag` represents unions as a JSON object with one field, whose name is the name of the union case, and whose value is an array whose items are the arguments of the union case.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {"NoArgs":[]}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"WithArgs":[123,"Hello world!"]}
    ```
    
* `JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields` is similar, except that the fields are represented as an object instead of an array.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {"NoArgs":{}}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"WithArgs":{"anInt":123,"aString":"Hello world!"}}
    ```

* `JsonUnionEncoding.InternalTag` represents unions as an array whose first item is the name of the case, and the rest of the items are the arguments.

    This is the same format used by Thoth.Json.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> ["NoArgs"]
    
    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> ["WithArgs",123,"Hello world!"]
    ```

* `JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields` represents unions as an object whose first field is `unionTagName` (defaulting to  `"Case"`), and whose value is the name of the case. The rest of the fields have the names and values of the arguments.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {"Case":"NoArgs"}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"Case":"WithArgs","anInt":123,"aString":"Hello world!"}
    ```

* `JsonUnionEncoding.Untagged` represents unions as an object whose fields have the names and values of the arguments. The name of the case is not encoded at all. Deserialization is only possible if the fields of all cases have different names.


    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> {}
    
    JsonSerializer.Serialize(WithArgs (123, "Hello world!"), options)
    // --> {"anInt":123,"aString":"Hello world!"}
    ```

* Additionally, or-ing `||| JsonUnionEncoding.BareFieldlessTags` to any of the previous formats represents cases that don't have any arguments as a simple string.

    ```fsharp
    JsonSerializer.Serialize(NoArgs, options)
    // --> "NoArgs"
    
    JsonSerializer.Serialize(WithArgs (123, "HelloWorld!"), options)
    // --> (same format as without BareFieldlessTags)
    ```

Union cases that are represented as `null` in .NET using `CompilationRepresentationFlags.UseNullAsTrueValue`, such as `Option.None`, are serialized as `null`.

### Options

By default, the type `'T option` is treated specially.

* The value `None`, as mentioned above, is represented as `null`.

* The value `Some x` is represented the same as `x`, without wrapping it in the union representation for `Some`.

When using a custom `JsonUnionEncoding`, this behavior is enabled by or-ing `||| JsonUnionEncoding.SuccintOption`.

## FAQ

* Does FSharp.SystemTextJson support struct records and unions?

Yes!

* Does FSharp.SystemTextJson support anonymous records?

Yes!

* Does FSharp.SystemTextJson support alternative formats for unions?

[Yes!](#unions)

* Does FSharp.SystemTextJson support representing `'T option` as either just `'T` or `null` (or an absent field)?

Yes! Starting with v0.6, this is the default behavior.
To supersede it, use an explicit `JsonUnionEncoding` that does not include `SuccintOption`.

* Does FSharp.SystemTextJson support `JsonPropertyNameAttribute` and `JsonIgnoreAttribute` on record fields?

Yes!

* Does FSharp.SystemTextJson support options such as `PropertyNamingPolicy` and `IgnoreNullValues`?

Not yet. ([issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/3), [issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/4), [issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/5))

* Does FSharp.SystemTextJson allocate memory?

As little as possible, but unfortunately the `FSharp.Reflection` API requires some allocations. In particular, an array is allocated for as many items as the record fields or union arguments, and structs are boxed. There is [work in progress](https://github.com/Tarmil/FSharp.SystemTextJson/pull/15) to improve this.

* Are there any benchmarks, e.g. against Newtonsoft.Json?

[Yes!](https://github.com/Tarmil/FSharp.SystemTextJson/pull/11)
