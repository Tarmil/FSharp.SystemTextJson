module FSharp.SystemTextJson.Benchmarks

open FSharp.Reflection

open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Validators
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Environments
open System.Reflection
open BenchmarkDotNet.Configs

open System.Text.Json
open System.Text.Json.Serialization
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type TestRecord =
    { name: string
      thing: bool option
      time: System.DateTimeOffset }

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
    inherit ArrayTestBase<SimpleClass>(SimpleClass(
        Name = "sample",
        Thing = Some true,
        Time = DateTimeOffset.UnixEpoch.AddDays(200.)
    ))


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
           typeof<ReflectionComparison> |]
    )


[<EntryPoint>]
let main argv =
    let _summary = defaultSwitch().Run(argv, config)
    0
