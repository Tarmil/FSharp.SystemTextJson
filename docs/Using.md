# How to use FSharp.SystemTextJson

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [Installing](#installing)
- [Using directly](#using-directly)
  - [Using options](#using-options)
  - [Using attributes](#using-attributes)
  - [Advantages and inconvenients](#advantages-and-inconvenients)
- [Using with ASP.NET Core MVC](#using-with-aspnet-core-mvc)
- [Using with SignalR](#using-with-signalr)
- [Using with Bolero](#using-with-bolero)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Installing

To use FSharp.SystemTextJson, install the [NuGet package](https://nuget.org/packages/FSharp.SystemTextJson) in your project.
The namespace to open is `System.Text.Json.Serialization`.

## Using directly

There are two ways to use `FSharp.SystemTextJson`.
The recommended way is to apply it to all F# types by passing `JsonSerializerOptions`.
You can also apply it to specific types with an attribute.

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

The options way is generally recommended because it applies the format to all F# types.
In addition to your defined types, this also includes:

* Types defined in referenced libraries that you can't modify to add an attribute.  
    This includes standard library types such as `option` and `Result`, `list`, `Map` and `Set`.
* Anonymous records.

The attribute way cannot handle the above cases.

The advantage of the attribute way is that it allows calling `Serialize` and `Deserialize` without having to pass options every time.
This may be useful if you are passing your own data to a library that calls these functions itself and doesn't take options.

## Using with ASP.NET Core MVC

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

## Using with SignalR

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

## Using with Bolero

Since version 0.14, [Bolero](https://fsbolero.io) uses System.Text.Json and FSharp.SystemTextJson for its Remoting.

To use FSharp.SystemTextJson with its default options, there is nothing to do.

To customize FSharp.SystemTextJson (see [Customizing](Customizing.md)), pass a function to the `AddRemoting` method in both the client-side and server-side setup.
You can use the same function to ensure that both use the exact same options:

```fsharp
// src/MyApp.Client/Startup.fs:

module Program =

    // Customize here
    let serializerOptions (options: JsonSerializerOptions) =
        let converter = JsonFSharpConverter(JsonUnionEncoding.ThothLike)
        options.Converters.Add(converter)

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.MyApp>("#main")
        builder.Services.AddRemoting(builder.HostEnvironment, serializerOptions) |> ignore
        builder.Build().RunAsync() |> ignore
        0


// src/MyApp.Server/Startup.fs:

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddRemoting<MyRemoteService>(MyApp.Client.Program.serializerOptions) |> ignore
        // ...
```
