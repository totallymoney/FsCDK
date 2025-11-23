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
              let app = App()

              stack "LambdaFnUrl" {
                  scope app

                  lambda "fn-url" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      addUrlOption (functionUrl { authType FunctionUrlAuthType.NONE })
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addFunctionUrl + cors via builders" {
              let app = App()

              stack "LambdaFnUrlCors" {
                  scope app

                  lambda "fn-url-cors" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      addUrlOption (
                          functionUrl {
                              authType FunctionUrlAuthType.NONE

                              corsOptions (
                                  cors {
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

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
