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

type Serialization () =
    
    let instance = 
        { name = "sample"
          thing = Some true
          time = System.DateTimeOffset.UnixEpoch.AddDays(200.) }

    let systemTextOptions = 
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options


    [<Params(1,10,100,1000)>]
    member val ArrayLength = 0 with get, set
    
    member val InstanceArray = [||] with get, set
    
    [<GlobalSetup>]
    member this.InitArray () = 
        this.InstanceArray <- Array.replicate this.ArrayLength instance

    [<Benchmark>]
    member this.Newtonsoft () = JsonConvert.SerializeObject this.InstanceArray |> ignore

    [<Benchmark>]
    member this.SystemTextJson () = System.Text.Json.JsonSerializer.Serialize(this.InstanceArray, systemTextOptions) |> ignore

let config =
     ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.ShortRun.With(Runtime.Core))
            .With(MemoryDiagnoser.Default)
            .With(MarkdownExporter.GitHub)
            .With(ExecutionValidator.FailOnError)

let defaultSwitch () =
    BenchmarkSwitcher([| typeof<Serialization> |])


[<EntryPoint>]
let main _ =
    let _summary = BenchmarkRunner.Run<Serialization>(config)
    0
