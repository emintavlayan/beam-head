open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders

open Helpers

initializeContext ()

let serverPath = Path.getFullName "src/Server"
let clientPath = Path.getFullName "src/Client"
let deployPath = Path.getFullName "deploy"
let domainTestsPath = Path.getFullName "tests/Domain"

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet [ "fable"; "clean"; "--yes" ] clientPath // Delete *.fs.js files created by Fable
)

Target.create "RestoreClientDependencies" (fun _ -> run npm [ "ci" ] clientPath)

Target.create "Bundle" (fun _ ->
    [
        "server", dotnet [ "publish"; "-c"; "Release"; "-o"; deployPath ] serverPath
        "client", dotnet [ "fable"; "-o"; "output"; "-s"; "--run"; "npx"; "vite"; "build" ] clientPath
    ]
    |> runParallel)

Target.create "Azure" (fun _ ->
    let web = webApp {
        name "beam-head"
        operating_system OS.Linux
        runtime_stack (DotNet "8.0")
        zip_deploy "deploy"
    }

    let deployment = arm {
        location Location.WestEurope
        add_resource web
    }

    deployment |> Deploy.execute "BeamHead" Deploy.NoParameters |> ignore)

Target.create "Build" (fun _ -> run dotnet [ "build"; "BeamHead.sln" ] "."

)

Target.create "Run" (fun _ ->
    [
        "server", dotnet [ "watch"; "run"; "--no-restore" ] serverPath
        "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "--run"; "npx"; "vite" ] clientPath
    ]
    |> runParallel)

Target.create "RunTestsHeadless" (fun _ -> run dotnet [ "test" ] domainTestsPath)

Target.create "WatchRunTests" (fun _ ->
    [ "domain", dotnet [ "watch"; "test"; "--no-restore" ] domainTestsPath ]
    |> runParallel)

Target.create "Format" (fun _ -> run dotnet [ "fantomas"; "." ] ".")

open Fake.Core.TargetOperators

let dependencies = [
    "Clean" ==> "RestoreClientDependencies" ==> "Bundle" ==> "Azure"
    "Clean" ==> "RestoreClientDependencies" ==> "Build" ==> "Run"

    "RestoreClientDependencies" ==> "Build" ==> "RunTestsHeadless"
    "RestoreClientDependencies" ==> "Build" ==> "WatchRunTests"
]

[<EntryPoint>]
let main args = runOrDefault args