#!/usr/bin/env -S dotnet fsi

#r "nuget: Fun.Build, 1.0.3"

open System
open System.IO
open Fun.Build

let (</>) a b = Path.Combine(a, b)
let root = __SOURCE_DIRECTORY__

let config =
    Environment.GetEnvironmentVariable("CONFIG")
    |> fun v -> if String.IsNullOrWhiteSpace(v) then "Release" else v

pipeline "ci" {
    description "Build, test, and synthesize the CDK app"

    stage "restore" { run "dotnet tool restore" }

    stage "build" {
        run (sprintf "dotnet build src%sc* -c %s" (string Path.DirectorySeparatorChar) config)
        run (sprintf "dotnet build cdk%sc* -c %s" (string Path.DirectorySeparatorChar) config)
    }

    stage "test" { run (sprintf "dotnet test tests -c %s --no-build" config) }

    stage "publish lambda" {
        workingDir (root </> "src" </> "NewApp")
        run (sprintf "dotnet publish -c %s -f net8.0 --nologo" config)
    }

    stage "cdk synth" {
        workingDir (root </> "cdk")
        run "npx aws-cdk --version"
        run "npx cdk synth"
    }

    runIfOnlySpecified false
}

// Documentation pipelines (optional)
// These enable building and watching fsdocs if you add a docs folder to your project.
pipeline "docs" {
    description "Build the documentation site with fsdocs"

    stage "build" {
        run "dotnet tool restore"
        // Build and publish app (so XML docs and binaries are available if referenced by docs)
        run (sprintf "dotnet build src%sc* -c %s" (string Path.DirectorySeparatorChar) config)
        run (sprintf "dotnet publish src%sc* -c %s -f net8.0 --no-build" (string Path.DirectorySeparatorChar) config)
        run (sprintf "dotnet fsdocs build --properties Configuration=%s --eval --strict" config)
    }

    // Only run when explicitly specified to avoid failures if no docs are present yet
    runIfOnlySpecified true
}

pipeline "docs:watch" {
    description "Watch and rebuild the documentation site"

    stage "build" {
        run (sprintf "dotnet build src%sc* -c %s --no-restore" (string Path.DirectorySeparatorChar) config)
        run (sprintf "dotnet publish src%sc* -c %s -f net8.0 --no-build" (string Path.DirectorySeparatorChar) config)
    }

    stage "watch" { run "dotnet fsdocs watch --eval --clean" }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
