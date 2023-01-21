#nowarn "52"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let ctx =
    match System.Environment.GetCommandLineArgs() |> List.ofArray with
    | cmd :: args -> Context.FakeExecutionContext.Create false cmd args
    | _ -> failwith "Impossible"

Context.setExecutionContext (Context.Fake ctx)

module Cli =
    let rec hasFlag f =
        function
        | [] -> false
        | x :: xs -> List.contains x f || hasFlag f xs

    let rec getOpt o =
        function
        | []
        | [ _ ] -> None
        | x :: y :: xs -> if List.contains x o then Some y else getOpt o xs

    let clean = hasFlag [ "-c"; "--clean" ] ctx.Arguments

module Paths =
    let root = Path.getDirectory __SOURCE_DIRECTORY__
    let sln = root </> "FSharp.SystemTextJson.sln"
    let src = root </> "src" </> "FSharp.SystemTextJson"
    let out = root </> "bin"
    let nugetOut = out </> "nuget"
    let test = root </> "tests" </> "FSharp.SystemTextJson.Tests"
    let benchmarks = root </> "benchmarks" </> "FSharp.SystemTextJson.Benchmarks"
    let trimTest = root </> "tests" </> "FSharp.SystemTextJson.TrimTest"

    let trimTestOut rti =
        trimTest
        </> "bin"
        </> "Release"
        </> "net7.0"
        </> rti
        </> "publish"
        </> "FSharp.SystemTextJson.TrimTest.dll"

Target.create "Clean" (fun _ -> !! "**/bin" ++ "**/obj" |> Shell.cleanDirs)

Target.create "Build" (fun _ -> DotNet.build id Paths.sln)

Target.create
    "Pack"
    (fun _ ->
        DotNet.pack
            (fun o ->
                { o with
                    OutputPath = Some Paths.nugetOut
                    MSBuildParams = { o.MSBuildParams with NoWarn = Some [ "NU5105" ] } }
            )
            Paths.src
    )

Target.create
    "Test"
    (fun _ ->
        DotNet.test
            (fun o ->
                { o with
                    Configuration = DotNet.BuildConfiguration.Release
                    Logger = Some "trx"
                    ResultsDirectory = Some Paths.out }
            )
            Paths.test
    )

let checkOk (name: string) (r: ProcessResult) =
    if not r.OK then
        failwithf "%s failed with code %d:\n%A" name r.ExitCode r.Errors

Target.create "TestTrim"
<| fun _ ->
    let rti = Environment.environVarOrDefault "DOTNET_RUNTIME_IDENTIFIER" "win-x64"
    DotNet.publish (fun o -> { o with SelfContained = Some true; Runtime = Some rti }) Paths.trimTest
    let dll = Paths.trimTestOut rti
    DotNet.exec id dll dll |> checkOk "Trim test"

/// This target doesn't need a dependency chain, because the benchmarks actually wrap and build the referenced
/// project(s) as part of the run.
Target.create
    "Benchmark"
    (fun _ ->
        DotNet.exec
            (fun o -> { o with WorkingDirectory = Paths.benchmarks })
            "run"
            "-c release --runtimes netcoreapp50 --filter \"*\""
        |> checkOk "Benchmarks"
    )

Target.create "All" ignore

"Build" ==> "Test" ==> "TestTrim" ==> "Pack" ==> "All"

"Clean" =?> ("Build", Cli.clean)

Target.runOrDefaultWithArguments "All"
