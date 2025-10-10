#!/usr/bin/env -S dotnet fsi

#r "nuget: Fun.Build, 1.0.3"

open System
open System.IO
open Fun.Build

let (</>) a b = Path.Combine(a, b)
let sln = __SOURCE_DIRECTORY__ </> "FsCDK.sln"
let config = "Release"
let nupkgs = __SOURCE_DIRECTORY__ </> "nupkgs"

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

  stage "lint" {
    run "dotnet tool restore"
    run $"dotnet fantomas --check {__SOURCE_FILE__} src docs"
  }

  stage "build" {
    run $"dotnet restore {sln}"
    run $"dotnet build {sln} -c {config} --no-restore"
    run $"dotnet test {sln} -c {config} --no-build"
  }

  stage "cdk synth tests" {
    workingDir (__SOURCE_DIRECTORY__ </> "tests")
    run "npx aws-cdk --version"
    run "npx cdk synth"
  }

  stage "cdk synth samples" {
    workingDir (__SOURCE_DIRECTORY__ </> "samples/playground/Playground.CDK")
    run "npx aws-cdk --version"
    run $"dotnet publish ../Playground -c {config} -f net8.0"
    run "npx cdk synth"
  }

  stage "docs" {
    run $"dotnet publish src -f net8.0 -c {config}"
    run $"dotnet fsdocs build --properties Configuration={config} --eval --strict"
  }

  stage "pack" { run $"dotnet pack {sln} -c {config} -p:PackageOutputPath=\"%s{nupkgs}\" {versionProperty}" }

  runIfOnlySpecified true
}

pipeline "docs" {
  description "Build the documentation (default)"
  stage "build" {
    run "dotnet tool restore"
    run $"dotnet publish src -f net8.0 -c {config}"
    run $"dotnet fsdocs build --properties Configuration={config} --eval --strict"
  }
  runIfOnlySpecified false
}

pipeline "docs:watch" {
  description "Watch and rebuild the documentation site"
  stage "build" { run $"dotnet publish src -f net8.0 -c {config}" }
  stage "watch" { run "dotnet fsdocs watch --eval --clean" }
  runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
