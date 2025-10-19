#!/usr/bin/env -S dotnet fsi

#r "nuget: Fun.Build, 1.0.3"

open System
open System.IO
open Fun.Build

let (</>) a b = Path.Combine(a, b)
let sln = __SOURCE_DIRECTORY__ </> "FsCDK.sln"
let config = "Release"
let nupkgs = __SOURCE_DIRECTORY__ </> "nupkgs"
let templateProj = __SOURCE_DIRECTORY__ </> "templates" </> "FsCDK.Templates.proj"

let nightlyVersion =
    Environment.GetEnvironmentVariable("NIGHTLY_VERSION")
    |> Option.ofObj
    |> Option.bind (fun nv -> if String.IsNullOrWhiteSpace(nv) then None else Some nv)

let versionProperty =
    match nightlyVersion with
    | None -> "-p:Version=0.1.0"
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
        run "npx aws-cdk --version --yes"
        run "npx cdk synth --yes"
    }

    stage "docs" {
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
        run $"dotnet publish src -c {config} -f net8.0 --no-build"
        run $"dotnet fsdocs build --properties Configuration={config} --eval --strict"
    }

    stage "pack" { run $"dotnet pack {sln} -c {config} -p:PackageOutputPath=\"%s{nupkgs}\" {versionProperty}" }

    stage "pack templates" {
        run
            $"dotnet pack %s{templateProj} -c {config} -p:IsNightlyBuild=true -p:PackageOutputPath=\"%s{nupkgs}\" {versionProperty}"
    }

    stage "test templates" {

        // Note: This will probably fail on Windows because there is no bash.
        // There could be bash under git directory (something like c:\Program Files\Git\bin\ that may or may not be in PATH variable).
        // But also "rm" is a bit different command.

        // Clean up NuGet sources
        run "bash -lc \"dotnet nuget remove source github >/dev/null 2>&1 || true\""

        run
            "bash -lc \"if [ -n \\\"$GITHUB_TOKEN\\\" ]; then dotnet nuget add source https://nuget.pkg.github.com/totallymoney/index.json -n github -u $GITHUB_ACTOR -p $GITHUB_TOKEN --store-password-in-clear-text || true; else echo 'GITHUB_TOKEN not set, skipping GitHub source add'; fi\""

        run "bash -lc \"dotnet nuget remove source local >/dev/null 2>&1 || true\""
        run $"dotnet nuget add source \"%s{nupkgs}\" --name local"

        // Install templates
        run
            "bash -lc \"if [ -n \\\"$NIGHTLY_VERSION\\\" ]; then dotnet new install FsCDK.Templates::$NIGHTLY_VERSION; else echo 'NIGHTLY_VERSION not set, attempting to install FsCDK.Templates without version'; dotnet new install FsCDK.Templates || true; fi\""

        // Test template
        run "rm -rf TemplateTest || true"

        // Create project with the appropriate FsCDK version
        run (
            match nightlyVersion with
            | Some nv -> $"dotnet new fscdk-lambda -n TemplateTest --FsCDKPkgVersion %s{nv}"
            | None -> "dotnet new fscdk-lambda -n TemplateTest --FsCDKPkgVersion 0.1.0"
        )

        run $"dotnet build TemplateTest -c {config}"
        run "rm -rf TemplateTest"
    }

    runIfOnlySpecified false
}

pipeline "docs" {
    description "Build the documentation (default)"

    stage "build" {
        run "dotnet tool restore"
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
        run $"dotnet publish src -c {config} -f net8.0 --no-build"
        run $"dotnet fsdocs build --properties Configuration={config} --eval --strict"
    }

    runIfOnlySpecified false
}

pipeline "docs:watch" {
    description "Watch and rebuild the documentation site"

    stage "build" {
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
        run $"dotnet publish src -c {config} -f net8.0 --no-build"
    }

    stage "watch" { run "dotnet fsdocs watch --eval --clean" }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
