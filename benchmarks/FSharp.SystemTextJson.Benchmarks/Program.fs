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

type TestBase<'t>(instance: 't) = 
    let systemTextOptions = 
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options


    [<Params(1,10,100)>]
    member val ArrayLength = 0 with get, set
    
    member val InstanceArray = [||] with get, set
    
    [<GlobalSetup>]
    member this.InitArray () = 
        this.InstanceArray <- Array.replicate this.ArrayLength instance

    [<Benchmark>]
    member this.Newtonsoft () = JsonConvert.SerializeObject this.InstanceArray

    [<Benchmark>]
    member this.SystemTextJson () = System.Text.Json.JsonSerializer.Serialize(this.InstanceArray, systemTextOptions)

let recordInstance = 
    { name = "sample"
      thing = Some true
      time = System.DateTimeOffset.UnixEpoch.AddDays(200.) }


type Records () =
    inherit TestBase<TestRecord>(recordInstance)

type Classes() =
    inherit TestBase<SimpleClass>(SimpleClass(Name = "sample", Thing = Some true, Time = DateTimeOffset.UnixEpoch.AddDays(200.)))
    
let config =
     ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.ShortRun.With(Runtime.Core))
            .With(MemoryDiagnoser.Default)
            .With(MarkdownExporter.GitHub)
            .With(ExecutionValidator.FailOnError)

let defaultSwitch () =
    BenchmarkSwitcher([| typeof<Records>; typeof<Classes> |])


[<EntryPoint>]
let main argv =
    let _summary = defaultSwitch().Run(argv, config)
    0
