#!/usr/bin/env -S dotnet fsi

#r "nuget: Fun.Build, 1.0.3"

open System
open System.IO
open Fun.Build

let root = __SOURCE_DIRECTORY__
let (</>) a b = Path.Combine(a, b)
let config = "Release"

let sln = __SOURCE_DIRECTORY__ </> "FsCdk.sln"
let nupkgs = __SOURCE_DIRECTORY__ </> "nupkgs"
let testProj = __SOURCE_DIRECTORY__ </> "tests/FsCdk.Tests/FsCdk.Tests.fsproj"

let nightlyVersion =
  Environment.GetEnvironmentVariable("NIGHTLY_VERSION")
  |> Option.ofObj
  |> Option.bind (fun nv -> if String.IsNullOrWhiteSpace(nv) then None else Some nv)

let versionProperty =
  match nightlyVersion with
  | None -> String.Empty
  | Some nv -> $"-p:Version=%s{nv}"

pipeline "ci" {
  description "Main pipeline used for CI"

  stage "build" {
    run (fun _ -> $"dotnet restore {sln}")
    run (fun _ -> $"dotnet build {sln} -c {config} --no-restore --nologo")
    run (fun _ -> $"dotnet test {testProj} -c {config} --no-build --nologo")
  }

  stage "pack" { run (fun _ -> $"dotnet pack {sln} -c {config} -p:PackageOutputPath=\"%s{nupkgs}\" {versionProperty}") }

  runIfOnlySpecified false
}

pipeline "docs" {
  description "Run the documentation website"
  stage "build" { run "dotnet publish src/FsCdk -f netstandard2.1 -c Release" }
  stage "watch" { run "dotnet fsdocs watch --eval --clean" }
  runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
