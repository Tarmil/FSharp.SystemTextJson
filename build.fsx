#r "paket:
nuget FSharp.Core ~> 4.6.0
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
//"
#load ".fake/build.fsx/intellisense.fsx"
#nowarn "52"
open System.IO
open System.Net
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let ctx = Context.forceFakeContext()

module Cli =
    let rec hasFlag f = function
        | [] -> false
        | x :: xs -> List.contains x f || hasFlag f xs

    let rec getOpt o = function
        | [] | [_] -> None
        | x :: y :: xs -> if List.contains x o then Some y else getOpt o xs

    let clean = hasFlag ["-c"; "--clean"] ctx.Arguments
    let pushTestsUrl = getOpt ["--push-tests"] ctx.Arguments

module Paths =
    let root = __SOURCE_DIRECTORY__
    let sln = root </> "FSharp.SystemTextJson.sln"
    let out = root </> "bin"
    let benchmarks = root </> "benchmarks" </> "FSharp.SystemTextJson.Benchmarks"

Target.create "Clean" (fun _ ->
    !! "**/bin"
    ++ "**/obj"
    |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    DotNet.build id Paths.sln
)

Target.create "Pack" (fun _ ->
    DotNet.pack (fun o ->
        { o with OutputPath = Some Paths.out }
    ) Paths.sln
)

let uploadTests (url: string) =
    let resultsFile =
        !! (Paths.out </> "*.trx")
        |> Seq.maxBy File.GetCreationTimeUtc
    use c = new WebClient()
    c.UploadFile(url, resultsFile) |> ignore

Target.create "Test" (fun _ ->
    try
        DotNet.test (fun o ->
            { o with
                Configuration = DotNet.BuildConfiguration.Release
                Logger = Some "trx"
                ResultsDirectory = Some Paths.out
            }
        ) Paths.sln
    finally
        Option.iter uploadTests Cli.pushTestsUrl
)

/// This target doesn't need a dependency chain, because the benchmarks actually wrap and build the referenced
/// project(s) as part of the run.
Target.create "Benchmark" (fun p ->
    let args = p.Context.Arguments
    seq {
        yield! ["-p"; Paths.benchmarks; "-c"; "release"; "--"]
        if not (List.contains "-f" args || List.contains "--filter" args) then
            yield! ["--filter"; "*"]
        yield! args
    }
    |> Args.toWindowsCommandLine
    |> DotNet.exec id "run"
    |> fun r -> 
        if not r.OK then
            failwithf "Benchmarks failed with code %d:\n%A" r.ExitCode r.Errors
)

Target.create "All" ignore

"Build"
==> "Test"
==> "Pack"
==> "All"

"Clean" =?> ("Build", Cli.clean)

Target.runOrDefaultWithArguments "All"
