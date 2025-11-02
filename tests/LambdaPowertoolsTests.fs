module FsCDK.Tests.LambdaPowertoolsTests

open Expecto
open FsCDK

[<Tests>]
let lambda_powertools_tests =
    testList
        "Lambda Powertools Helpers"
        [ test "provides Python layer ARN for us-east-1" {
              let arn = LambdaPowertools.LayerVersionArns.Python.python312 "us-east-1"
              Expect.stringContains arn "AWSLambdaPowertoolsPython" "ARN should contain Python Powertools"
              Expect.stringContains arn "us-east-1" "ARN should contain region"
          }

          test "provides Node.js layer ARN for eu-west-1" {
              let arn = LambdaPowertools.LayerVersionArns.NodeJs.node20 "eu-west-1"
              Expect.stringContains arn "AWSLambdaPowertoolsTypeScript" "ARN should contain TypeScript Powertools"
              Expect.stringContains arn "eu-west-1" "ARN should contain region"
          }

          test "provides Java layer ARN for ap-southeast-1" {
              let arn = LambdaPowertools.LayerVersionArns.Java.java17 "ap-southeast-1"
              Expect.stringContains arn "AWSLambdaPowertoolsJava" "ARN should contain Java Powertools"
              Expect.stringContains arn "ap-southeast-1" "ARN should contain region"
          }

          test "provides .NET NuGet package names" {
              let packages = LambdaPowertools.LayerVersionArns.DotNet.nugetPackages
              Expect.isNonEmpty packages "Should provide NuGet packages"
              Expect.contains packages "AWS.Lambda.Powertools.Logging" "Should include Logging package"
          }

          test "development config provides DEBUG log level" {
              let config = LambdaPowertools.StandardConfigs.development "MyService"
              let envVars = config
              Expect.isNonEmpty envVars "Should provide environment variables"
              let logLevel = envVars |> List.tryFind (fun (k, _) -> k = "LOG_LEVEL")
              match logLevel with
              | Some (_, level) -> Expect.equal level "DEBUG" "Development should use DEBUG level"
              | None -> failtest "Log level not found"
          }

          test "production config provides INFO log level" {
              let config = LambdaPowertools.StandardConfigs.production "MyService"
              let envVars = config
              Expect.isNonEmpty envVars "Should provide environment variables"
              let logLevel = envVars |> List.tryFind (fun (k, _) -> k = "LOG_LEVEL")
              match logLevel with
              | Some (_, level) -> Expect.equal level "INFO" "Production should use INFO level"
              | None -> failtest "Log level not found"
          } ]
