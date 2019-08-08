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

type SleepMarks () =
    [<Params(0, 1, 15, 100)>]
    member val public sleepTime = 0 with get, set

    // [<GlobalSetup>]
    // member self.GlobalSetup() =
    //     printfn "%s" "Global Setup"

    // [<GlobalCleanup>]
    // member self.GlobalCleanup() =
    //     printfn "%s" "Global Cleanup"

    // [<IterationSetup>]
    // member self.IterationSetup() =
    //     printfn "%s" "Iteration Setup"
    // [<IterationCleanup>]
    // member self.IterationCleanup() =
    //     printfn "%s" "Iteration Cleanup"

    [<Benchmark>]
    member this.Thread () = System.Threading.Thread.Sleep(this.sleepTime)

    [<Benchmark>]
    member this.Task () = System.Threading.Tasks.Task.Delay(this.sleepTime)


    [<Benchmark>]
    member this.AsyncToTask () = Async.Sleep(this.sleepTime) |> Async.StartAsTask
    [<Benchmark>]
    member this.AsyncToSync () = Async.Sleep(this.sleepTime) |> Async.RunSynchronously




let config =
     ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.ShortRun.With(Runtime.Mono))
            .With(Job.ShortRun.With(Runtime.Core))
            .With(MemoryDiagnoser.Default)
            .With(MarkdownExporter.GitHub)
            .With(ExecutionValidator.FailOnError)

let defaultSwitch () =
        Assembly.GetExecutingAssembly().GetTypes() |> Array.filter (fun t ->
            t.GetMethods ()|> Array.exists (fun m ->
                m.GetCustomAttributes (typeof<BenchmarkAttribute>, false) <> [||] ))
        |> BenchmarkSwitcher


[<EntryPoint>]
let main argv =
    defaultSwitch().Run(argv,config) |>ignore
    // BenchmarkRunner.Run<SleepMarks>(config) |> ignore
    0 // return an integer exit code
