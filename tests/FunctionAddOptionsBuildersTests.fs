module FsCDK.Tests.FunctionAddOptionsBuildersTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM
open FsCdk.Tests.TestHelpers

[<Tests>]
let lambda_add_options_builders_tests =
    testList
        "Lambda add* options builders"
        [ test "app synth with addEventSourceMapping via builder" {
              let application = App()

              stack "LambdaESMBuilder" application {
                  lambda "fn-esm-b" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      eventSourceMapping "SqsMapping" {
                          eventSourceArn "arn:aws:sqs:us-east-1:111122223333:my-queue"
                          batchSize 5
                      }
                  }
              }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth with addPermission via builder" {
              let application = App()

              stack "LambdaPermBuilder" application {
                  lambda "fn-perm-b" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      permission "ApiGwInvoke" {
                          principal (ServicePrincipal("apigateway.amazonaws.com"))
                          action "lambda:InvokeFunction"
                          sourceArn "arn:aws:execute-api:us-east-1:111122223333:api-id/*/*/*"
                      }
                  }
              }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth with configureAsyncInvoke via builder" {
              let application = App()

              stack "LambdaAsyncBuilder" application {
                  lambda "fn-async-b" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      configureAsyncInvoke {
                          maxEventAge (Duration.Minutes(1.0))
                          retryAttempts 1
                      }
                  }
              }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
