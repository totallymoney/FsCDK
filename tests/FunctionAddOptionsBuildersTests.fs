module FsCDK.Tests.FunctionAddOptionsBuildersTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM

[<Tests>]
let lambda_add_options_builders_tests =
    testList
        "Lambda add* options builders"
        [ test "app synth with addEventSourceMapping via builder" {
              let lambdaStack =
                  stack "LambdaESMBuilder" {
                      lambda "fn-esm-b" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (System.IO.Directory.GetCurrentDirectory())

                          eventSourceMapping
                              "SqsMapping"
                              (eventSourceMappingOptions {
                                  eventSourceArn "arn:aws:sqs:us-east-1:111122223333:my-queue"
                                  batchSize 5
                              })
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth with addPermission via builder" {
              let lambdaStack =
                  stack "LambdaPermBuilder" {
                      lambda "fn-perm-b" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (System.IO.Directory.GetCurrentDirectory())

                          permission
                              "ApiGwInvoke"
                              (Builders.permission {
                                  principal (ServicePrincipal("apigateway.amazonaws.com"))
                                  action "lambda:InvokeFunction"
                                  sourceArn "arn:aws:execute-api:us-east-1:111122223333:api-id/*/*/*"
                              })
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth with configureAsyncInvoke via builder" {
              let lambdaStack =
                  stack "LambdaAsyncBuilder" {
                      lambda "fn-async-b" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (System.IO.Directory.GetCurrentDirectory())

                          configureAsyncInvoke (
                              eventInvokeConfigOptions {
                                  maxEventAge (Duration.Minutes(1.0))
                                  retryAttempts 1
                              }
                          )
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
