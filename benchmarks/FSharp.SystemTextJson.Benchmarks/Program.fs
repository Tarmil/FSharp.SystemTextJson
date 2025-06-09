module FSharp.SystemTextJson.Benchmarks

open FSharp.Reflection

open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Validators
open BenchmarkDotNet.Exporters

open System.Text.Json
open System.Text.Json.Serialization
open Newtonsoft.Json

type TestRecord = { name: string; thing: bool option; time: DateTimeOffset }

type SimpleClass() =
    member val Name: string = null with get, set
    member val Thing: bool option = None with get, set
    member val Time: DateTimeOffset = DateTimeOffset.MinValue with get, set

type ArrayTestBase<'t>(instance: 't) =
    let systemTextOptions =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options


    [<Params(10, 100)>]
    member val ArrayLength = 0 with get, set

    member val InstanceArray = [||] with get, set

    [<GlobalSetup>]
    member this.InitArray() =
        this.InstanceArray <- Array.replicate this.ArrayLength instance

    [<Benchmark>]
    member this.Newtonsoft() =
        JsonConvert.SerializeObject this.InstanceArray

    [<Benchmark>]
    member this.SystemTextJson() =
        System.Text.Json.JsonSerializer.Serialize(this.InstanceArray, systemTextOptions)

let recordInstance =
    { name = "sample"
      thing = Some true
      time = System.DateTimeOffset.UnixEpoch.AddDays(200.) }


type Records() =
    inherit ArrayTestBase<TestRecord>(recordInstance)

type Classes() =
    inherit
        ArrayTestBase<SimpleClass>(
            SimpleClass(Name = "sample", Thing = Some true, Time = DateTimeOffset.UnixEpoch.AddDays(200.))
        )


type ReflectionComparison() =

    [<Params(10, 100, 1000)>]
    member val Iterations = 0 with get, set

    [<Benchmark>]
    member this.FSharpUnion() =
        for i in 0 .. this.Iterations do
            FSharpType.IsUnion(typeof<bool option>, true) |> ignore

    [<Benchmark>]
    member this.FSharpUnionCached() =
        for i in 0 .. this.Iterations do
            TypeCache.isUnion typeof<bool option> |> ignore

    [<Benchmark>]
    member this.FSharpRecord() =
        for i in 0 .. this.Iterations do
            Reflection.FSharpType.IsRecord(typeof<TestRecord>, true) |> ignore

    [<Benchmark>]
    member this.FSharpRecordCached() =
        for i in 0 .. this.Iterations do
            TypeCache.isRecord typeof<TestRecord> |> ignore

type TupleComparison() =

    let fsharpOptions = JsonFSharpOptions.Default()
    let specializedOptions = fsharpOptions.ToJsonSerializerOptions()
    let genericOptions =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonTupleConverter(fsharpOptions, true))
        o

    [<Benchmark>]
    member this.StructGeneric() =
        System.Text.Json.JsonSerializer.Deserialize<struct (int * bool)>("[1,true]", genericOptions)

    [<Benchmark>]
    member this.StructSpecialized() =
        System.Text.Json.JsonSerializer.Deserialize<struct (int * bool)>("[1,true]", specializedOptions)

    [<Benchmark>]
    member this.RefGeneric() =
        System.Text.Json.JsonSerializer.Deserialize<int * bool>("[1,true]", genericOptions)

    [<Benchmark>]
    member this.RefSpecialized() =
        System.Text.Json.JsonSerializer.Deserialize<int * bool>("[1,true]", specializedOptions)

type ListDeserialization<'t>(instance: 't) =
    let options = JsonFSharpOptions().ToJsonSerializerOptions()

    [<Params(10, 100)>]
    member val ListLength = 0 with get, set

    member val String = "" with get, set

    [<GlobalSetup>]
    member this.InitList() =
        this.String <- System.Text.Json.JsonSerializer.Serialize(Array.replicate this.ListLength instance, options)

    [<Benchmark>]
    member this.ListCollector() =
        System.Text.Json.JsonSerializer.Deserialize<'t list>(this.String, options)

    [<Benchmark>]
    member this.AsArray() =
        System.Text.Json.JsonSerializer.Deserialize<'t array>(this.String, options)
        |> List.ofArray

type ListDeserialization() =
    inherit ListDeserialization<int>(42)

let config =
    ManualConfig
        .Create(DefaultConfig.Instance)
        .AddDiagnoser(MemoryDiagnoser.Default)
        .AddExporter(MarkdownExporter.GitHub)
        .AddValidator(ExecutionValidator.FailOnError)

let defaultSwitch () =
    BenchmarkSwitcher(
        [| typeof<Records>
           typeof<Classes>
           typeof<ReflectionComparison>
           typeof<TupleComparison>
           typeof<ListDeserialization> |]
    )


[<EntryPoint>]
let main argv =
    let _summary = defaultSwitch().Run(argv, config)
    0
