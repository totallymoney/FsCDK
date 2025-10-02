module FsCDK.Tests.FunctionUrlOptionsTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open FsCdk.Tests.TestHelpers

[<Tests>]
let function_url_options_builder_tests =
    testList
        "FunctionUrlOptions builder"
        [ test "app synth succeeds with addFunctionUrl via builder" {
              let lambdaStack =
                  stack "LambdaFnUrl" {
                      lambda "fn-url" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                          functionUrl (functionUrlOptions { authType FunctionUrlAuthType.NONE })
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addFunctionUrl + cors via builders" {
              let lambdaStack =
                  stack "LambdaFnUrlCors" {
                      lambda "fn-url-cors" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          functionUrl (
                              functionUrlOptions {
                                  authType FunctionUrlAuthType.NONE

                                  cors (
                                      functionUrlCorsOptions {
                                          allowedOrigins [ "https://example.com" ]
                                          allowedMethods [ HttpMethod.GET; HttpMethod.OPTIONS ]
                                          allowedHeaders [ "*" ]
                                          allowCredentials true
                                          maxAge (Duration.Seconds(60.0))
                                      }
                                  )
                              }
                          )
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
