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

### Records and anonymous records

Records and anonymous records are serialized as JSON objects.

```fsharp
type Example = { x: string; y: string }

JsonSerializer.Serialize { x = "Hello"; y = "world!" }
// --> {"x":"Hello","y":"world!"}

JsonSerializer.Serialize {| x = "Hello"; y = "world!" |}
// --> {"x":"Hello","y":"world!"}
```

Named record fields are serialized in the order in which they were declared in the type declaration.

Anonymous record fields are serialized in alphabetical order.

### Unions

Unions are serialized with the same format as `Newtonsoft.Json`, that is, a JSON object with two fields:

* `"Case"`: a string whose value is the name of the union case.
* `"Fields"`: an array whose items are the arguments of the union case. This field is absent if the union case has no arguments.

```fsharp
type Example =
    | WithArgs of int * string
    | NoArgs

JsonSerializer.Serialize NoArgs
// --> {"Case":"NoArgs"}

JsonSerializer.Serialize (WithArgs (123, "Hello world!"))
// --> {"Case":"WithArgs","Fields":[123,"Hello world!"]}
```

Union cases that are represented as `null` in .NET using `CompilationRepresentationFlags.UseNullAsTrueValue`, such as `Option.None`, are serialized as `null`.

## FAQ

* Does FSharp.SystemTextJson support struct records and unions?

Yes!

* Does FSharp.SystemTextJson support anonymous records?

Yes!

* Does FSharp.SystemTextJson support alternative formats for unions? I find `"Case"`/`"Fields"` ugly.

Not yet. ([issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/6))

* Does FSharp.SystemTextJson support `JsonPropertyNameAttribute` and `JsonIgnoreAttribute`?

Not yet. ([issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/1), [issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/2))

* Does FSharp.SystemTextJson support options such as `PropertyNamingPolicy` and `IgnoreNullValues`?

Not yet. ([issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/3), [issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/4), [issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/5))

* Does FSharp.SystemTextJson allocate memory?

As little as possible, but unfortunately the `FSharp.Reflection` API it uses requires some allocations. In particular, an array is allocated for as many items as the record fields or union arguments, and structs are boxed.

* Are there any benchmarks, eg. against Newtonsoft.Json?

Not yet. ([issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/7))
