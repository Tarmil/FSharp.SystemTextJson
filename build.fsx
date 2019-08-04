#r "paket:
nuget FSharp.Core ~> 4.6.0
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget System.Text.Json preview7
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
open System.Text.Json

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
    let avToken = getOpt ["--av-token"] ctx.Arguments
    let nugetKey = getOpt ["--nuget-key"] ctx.Arguments

module Paths =
    let root = __SOURCE_DIRECTORY__
    let sln = root </> "FSharp.SystemTextJson.sln"
    let out = root </> "bin"
    let nugetTemp = out </> "from-av"

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
                Logger = Some "trx"
                ResultsDirectory = Some Paths.out
            }
        ) Paths.sln
    finally
        Option.iter uploadTests Cli.pushTestsUrl
)

Target.create "All" ignore

let downloadAppVeyorArtifacts (project: string) (targetDir: string) (token: string) =
    use c = new WebClient()
    let setAuth() =
        c.Headers.Add("Authorization", "Bearer " + token)

    setAuth()
    c.Headers.Add("Content-type", "application/json")
    let s = c.DownloadString(sprintf "https://ci.appveyor.com/api/projects/%s" project)
    let jobId = JsonDocument.Parse(s).RootElement.GetProperty("build").GetProperty("jobs").[0].GetProperty("jobId").GetString()

    setAuth()
    c.Headers.Add("Content-type", "application/json")
    let s = c.DownloadString(sprintf "https://ci.appveyor.com/api/buildjobs/%s/artifacts" jobId)
    let artifacts = JsonDocument.Parse(s).RootElement

    artifacts.EnumerateArray()
    |> Seq.map (fun artifact ->
        let fileName = artifact.GetProperty("fileName").GetString()
        printfn "Downloading %s from AppVeyor..." fileName
        setAuth()
        let url = sprintf "https://ci.appveyor.com/api/buildjobs/%s/artifacts/%s" jobId fileName
        let path = targetDir </> fileName
        Directory.create (Path.getDirectory path)
        c.DownloadFile(url, path)
        printfn "Done."
        path
    )
    |> Array.ofSeq

Target.description "Push the latest CI build to nuget.org"
Target.create "Push-Latest" (fun _ ->
    Shell.cleanDir Paths.nugetTemp
    Cli.avToken
    |> Option.defaultWith (fun () -> failwith "Missing argument: --av-token <appveyor-token>")
    |> downloadAppVeyorArtifacts "Tarmil/fsharp-systemtextjson" Paths.nugetTemp
    |> Array.iter (DotNet.nugetPush (fun o ->
        o.WithPushParams(
            { o.PushParams with
                ApiKey = Cli.nugetKey
                Source = Some "https://api.nuget.org/v3/index.json"
            })
    ))
)

"Build"
==> "Test"
==> "Pack"
==> "All"

"Clean" =?> ("Build", Cli.clean)

Target.runOrDefaultWithArguments "All"
