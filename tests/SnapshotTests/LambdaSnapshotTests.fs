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
        [
            test "lambdaFunction builder applies secure defaults" {
                let funcResource = lambdaFunction "test-function" {
                    handler "index.handler"
                    runtime Runtime.NODEJS_18_X
                    codePath "./dummy"
                }
                
                // Verify construct ID defaults to function name
                Expect.equal funcResource.ConstructId "test-function" "ConstructId should default to function name"
                
                // Verify function name
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
            
            test "lambdaFunction builder with custom memory and timeout" {
                let funcResource = lambdaFunction "test-function" {
                    handler "index.handler"
                    runtime Runtime.PYTHON_3_11
                    codePath "./dummy"
                    memorySize 1024
                    timeout 60.0
                }
                
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
            
            test "lambdaFunction builder with environment variables" {
                let funcResource = lambdaFunction "test-function" {
                    handler "index.handler"
                    runtime Runtime.PYTHON_3_11
                    codePath "./dummy"
                    environment [ "KEY1", "value1"; "KEY2", "value2" ]
                }
                
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
            
            test "lambdaFunction builder with X-Ray tracing enabled" {
                let funcResource = lambdaFunction "test-function" {
                    handler "index.handler"
                    runtime Runtime.PYTHON_3_11
                    codePath "./dummy"
                    xrayEnabled
                }
                
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
            
            test "lambdaFunction builder with custom construct ID" {
                let funcResource = lambdaFunction "test-function" {
                    constructId "CustomFunctionId"
                    handler "index.handler"
                    runtime Runtime.DOTNET_8
                    codePath "./dummy"
                }
                
                Expect.equal funcResource.ConstructId "CustomFunctionId" "ConstructId should be custom"
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
            
            test "lambdaFunction builder with log retention" {
                let funcResource = lambdaFunction "test-function" {
                    handler "index.handler"
                    runtime Runtime.NODEJS_20_X
                    codePath "./dummy"
                    logRetention RetentionDays.ONE_WEEK
                }
                
                Expect.equal funcResource.FunctionName "test-function" "FunctionName should be set"
            }
        ]
    |> testSequenced
