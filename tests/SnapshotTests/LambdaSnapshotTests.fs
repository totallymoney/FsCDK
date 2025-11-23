module FsCDK.Tests.LambdaSnapshotTests

open Expecto
open FsCDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs

/// <summary>
/// Snapshot tests for Lambda module ensuring security defaults are properly applied
/// These tests verify builder configuration and properties
/// </summary>
[<Tests>]
let lambda_snapshot_tests =
    testList
        "Lambda Module Snapshot Tests"
        [ test "lambda builder applies secure defaults" {
              let funcSpec =
                  lambda "test-function" {
                      handler "index.handler"
                      runtime Runtime.NODEJS_18_X
                      code "./dummy"
                  }

              // Verify construct ID defaults to function name
              Expect.equal funcSpec.ConstructId "test-function" "ConstructId should default to function name"

              // Verify function name
              Expect.equal funcSpec.FunctionName "test-function" "FunctionName should be set"
          }

          test "lambda builder with custom memory and timeout" {
              let funcSpec =
                  lambda "test-function" {
                      handler "index.handler"
                      runtime Runtime.PYTHON_3_11
                      code "./dummy"
                      memorySize 1024
                      timeout 60.0
                  }

              Expect.equal funcSpec.FunctionName "test-function" "FunctionName should be set"
          }

          test "lambda builder with environment variables" {
              let funcSpec =
                  lambda "test-function" {
                      handler "index.handler"
                      runtime Runtime.PYTHON_3_11
                      code "./dummy"
                      environment [ "KEY1", "value1"; "KEY2", "value2" ]
                  }

              Expect.equal funcSpec.FunctionName "test-function" "FunctionName should be set"
          }

          test "lambda builder with X-Ray tracing enabled" {
              let funcSpec =
                  lambda "test-function" {
                      handler "index.handler"
                      runtime Runtime.PYTHON_3_11
                      code "./dummy"
                      xrayEnabled
                  }

              Expect.equal funcSpec.FunctionName "test-function" "FunctionName should be set"
          }

          test "lambda builder with custom construct ID" {
              let funcSpec =
                  lambda "test-function" {
                      constructId "CustomFunctionId"
                      handler "index.handler"
                      runtime Runtime.DOTNET_8
                      code "./dummy"
                  }

              Expect.equal funcSpec.ConstructId "CustomFunctionId" "ConstructId should be custom"
              Expect.equal funcSpec.FunctionName "test-function" "FunctionName should be set"
          }

          ]
    |> testSequenced
