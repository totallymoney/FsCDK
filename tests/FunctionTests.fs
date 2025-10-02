module FsCDK.Tests.FunctionTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM
open FsCdk.Tests.TestHelpers

[<Tests>]
let lambda_function_dsl_tests =
    testList
        "Lambda Function DSL"
        [ test "fails when handler is missing" {
              let thrower () =
                  lambda "MyFn" {
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                  }
                  |> ignore

              Expect.throws thrower "Function builder should throw when handler is missing"
          }

          test "fails when runtime is missing" {
              let thrower () =
                  lambda "MyFn" {
                      handler "Program::Handler"
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                  }
                  |> ignore

              Expect.throws thrower "Function builder should throw when runtime is missing"
          }

          test "fails when code path is missing" {
              let thrower () =
                  lambda "MyFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                  }
                  |> ignore

              Expect.throws thrower "Function builder should throw when code path is missing"
          }

          test "defaults constructId to function name" {
              let spec =
                  lambda "UsersFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                  }

              Expect.equal spec.FunctionName "UsersFn" "FunctionName should be set"
              Expect.equal spec.ConstructId "UsersFn" "ConstructId should default to function name"
          }

          test "uses custom constructId when provided" {
              let spec =
                  lambda "UsersFn" {
                      constructId "UsersFunction"
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                  }

              Expect.equal spec.ConstructId "UsersFunction" "Custom constructId should be used"
          }

          test "applies environment variables when configured" {
              let spec =
                  lambda "EnvFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      environment [ ("A", "1"); ("B", "2") ]
                  }

              Expect.isNotNull spec.Props.Environment "Environment should be set"
              Expect.equal spec.Props.Environment.Count 2 "Should have two env vars"
              Expect.equal spec.Props.Environment["A"] "1" "Env var A should be 1"
              Expect.equal spec.Props.Environment["B"] "2" "Env var B should be 2"
          }

          test "applies optional properties when configured" {
              let spec =
                  lambda "OptsFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      timeout 10.0
                      memory 512
                      description "My function"
                  }

              // MemorySize can be int or Nullable<double> depending on the CDK version; handle both
              let memObj = box spec.Props.MemorySize

              match memObj with
              | :? int as i -> Expect.equal i 512 "Memory size should be set to 512"
              | :? System.Nullable<double> as n when n.HasValue ->
                  Expect.equal (int n.Value) 512 "Memory size should be set to 512"
              | _ -> failtestf $"Unexpected MemorySize type/value: %A{memObj}"

              Expect.equal spec.Props.Description "My function" "Description should match"
          }

          test "app synth succeeds with function all-common-properties" {
              let lambdaStack =
                  stack "LambdaStack" {
                      lambda "my-fn" {
                          constructId "MyFunction"
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                          environment [ ("A", "1"); ("B", "2") ]
                          timeout 5.0
                          memory 256
                          description "Test function"

                          // Post-creation operations that don't require extra packages
                          functionUrl (FunctionUrlOptions(AuthType = FunctionUrlAuthType.NONE))
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()

              // Basic assertion: we produced exactly one stack in this app
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addEventSourceMapping" {
              let lambdaStack =
                  stack "LambdaESM" {
                      lambda "fn-esm" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          eventSourceMapping
                              "SqsMapping"
                              (EventSourceMappingOptions(
                                  EventSourceArn = "arn:aws:sqs:us-east-1:111122223333:my-queue",
                                  BatchSize = 5
                              ))
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addPermission" {
              let lambdaStack =
                  stack "LambdaPerm" {
                      lambda "fn-perm" {
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

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addToRolePolicy" {
              let lambdaStack =
                  stack "LambdaPolicy" {
                      lambda "fn-policy" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          toRolePolicy (
                              let props = PolicyStatementProps(Effect = Effect.ALLOW)
                              props.Actions <- [| "logs:CreateLogGroup"; "logs:CreateLogStream"; "logs:PutLogEvents" |]
                              props.Resources <- [| "*" |]
                              PolicyStatement(props)
                          )
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with configureAsyncInvoke" {
              let lambdaStack =
                  stack "LambdaAsync" {
                      lambda "fn-async" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          configureAsyncInvoke (
                              EventInvokeConfigOptions(MaxEventAge = Duration.Minutes(1.0), RetryAttempts = 1)
                          )
                      }
                  }

              let application = app { lambdaStack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "builder captures addEventSource action" {
              // Use a fake IEventSource to avoid extra dependencies
              let dummyEventSource =
                  { new IEventSource with
                      member _.Bind(_target: IFunction) = () }

              let spec =
                  lambda "fn-dummy" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (System.IO.Directory.GetCurrentDirectory()) S3.excludeCommonAssetDirs
                      eventSource dummyEventSource
                  }

              // Ensure an action was captured for AddEventSource
              Expect.isGreaterThan spec.Actions.Length 0 "Actions should include AddEventSource"
          }

          test "app synth succeeds with addToRolePolicy via builders" {
              let lambdaStack =
                  stack "LambdaPolicyBuilders" {
                      lambda "fn-policy-b" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          toRolePolicy (
                              policyStatement {
                                  withProps (
                                      policyStatementProps {
                                          effect Effect.ALLOW
                                          actions [ "logs:CreateLogGroup"; "logs:CreateLogStream"; "logs:PutLogEvents" ]
                                          resources [ "*" ]
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
