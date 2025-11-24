module FsCDK.Tests.FunctionTests

open Amazon.CDK
open Amazon.CDK.AWS.EFS
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.Logs
open Expecto
open FsCDK
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
                      memorySize 512
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
              let app = App()

              stack "LambdaStack" {
                  scope app

                  lambda "my-fn" {
                      constructId "MyFunction"
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      environment [ ("A", "1"); ("B", "2") ]
                      timeout 5.0
                      memorySize 256
                      description "Test function"

                      // Post-creation operations that don't require extra packages
                      addUrlOption (
                          functionUrl {
                              authType FunctionUrlAuthType.NONE

                              corsOptions (
                                  cors {
                                      allowedOrigins [ "*" ]
                                      allowedMethods [ HttpMethod.ALL ]
                                      allowedHeaders [ "*" ]
                                      allowCredentials false
                                      maxAge (Duration.Seconds(300.0))
                                  }
                              )
                          }
                      )
                  }
              }

              let cloudAssembly = app.Synth()

              // Basic assertion: we produced exactly one stack in this app
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addEventSourceMapping" {
              let app = App()

              stack "LambdaESM" {
                  scope app

                  let eventSourceMapping =
                      eventSourceMapping {
                          eventSourceArn "arn:aws:sqs:us-east-1:111122223333:my-queue"
                          batchSize 5
                      }

                  lambda "fn-esm" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      addEventSourceMapping ("SqsMapping", eventSourceMapping)
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addPermission" {
              let app = App()

              stack "LambdaPerm" {
                  scope app

                  lambda "fn-perm" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      addPermission (
                          permission "ApiGwInvoke" {
                              principal (ServicePrincipal("apigateway.amazonaws.com"))
                              action "lambda:InvokeFunction"
                              sourceArn "arn:aws:execute-api:us-east-1:111122223333:api-id/*/*/*"
                          }
                      )
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with addToRolePolicy" {
              let app = App()

              stack "LambdaPolicy" {
                  scope app

                  lambda "fn-policy" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      addRolePolicyStatement (
                          policyStatement {
                              effect Effect.ALLOW

                              actions
                                  [ "logs:CreateLogGroup"
                                    "logs:CreateLogStream"
                                    "logs:PutLogEvents"
                                    "dynamodb:Query"
                                    "dynamodb:Scan" ]

                              resources [ "arn:aws:dynamodb:us-east-1:111122223333:table/my-table" ]
                          }
                      )
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with configureAsyncInvoke" {
              let app = App()

              stack "LambdaAsync" {
                  scope app

                  lambda "fn-async" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      asyncInvokeOption (
                          eventInvokeConfigOptions {
                              maxEventAge (Duration.Minutes(1.0))
                              retryAttempts 1
                          }
                      )
                  }
              }

              let cloudAssembly = app.Synth()
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
                      addEventSource dummyEventSource
                  }

              // Ensure an action was captured for AddEventSource
              Expect.isGreaterThan spec.EventSources.Length 0 "Actions should include AddEventSource"
          }

          test "app synth succeeds with addToRolePolicy via builders" {
              let app = App()

              stack "LambdaPolicyBuilders" {
                  scope app

                  lambda "fn-policy-b" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))


                      addRolePolicyStatement (
                          policyStatement {
                              effect Effect.ALLOW

                              actions
                                  [ "logs:CreateLogGroup"
                                    "logs:CreateLogStream"
                                    "logs:PutLogEvents"
                                    "dynamodb:Query"
                                    "dynamodb:Scan" ]

                              resources [ "arn:aws:dynamodb:us-east-1:111122223333:table/my-table" ]
                          }
                      )
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "implicit yield sets Architecture correctly" {
              let spec =
                  lambda "fn-arch" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      architecture Architecture.ARM_64
                  }

              Expect.equal spec.Props.Architecture Architecture.ARM_64 "Architecture should be set correctly"
          }

          test "implicit yield sets Tracing correctly" {
              let spec =
                  lambda "fn-trace" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      tracing Tracing.ACTIVE
                  }

              // Tracing might be Nullable depending on version
              let tracingObj = box spec.Props.Tracing

              match tracingObj with
              | :? System.Nullable<Tracing> as n when n.HasValue ->
                  Expect.equal n.Value Tracing.ACTIVE "Tracing should be set correctly"
              | :? Tracing as t -> Expect.equal t Tracing.ACTIVE "Tracing should be set correctly"
              | _ -> failtestf $"Unexpected Tracing type/value: %A{tracingObj}"
          }

          test "file system CE yields correctly" {
              stack "LambdaFS" {

                  let! vpc1 = vpc "vpc" { maxAzs 2 }

                  let! efsFs = efsFileSystem "efs" { vpc vpc1 }

                  let! ap =
                      accessPoint "ap" {
                          fileSystem efsFs
                          path "/export/lambda"
                          posixUser "1000" "1000"
                          createAcl "1000" "1000" "750"
                      }

                  let spec =
                      lambda "fn-fs" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          fileSystem (
                              lambdaFileSystem {
                                  arn ap.AccessPointArn
                                  localMountPath "/mnt/data"
                              }
                          )
                      }

                  // Verify FileSystem is set correctly
                  Expect.isNotNull spec.Props.Filesystem "FileSystem should be set"
              }
          }

          test "file system CE validates required properties" {
              stack "LambdaFS" {
                  let! vpcFs = vpc "vpc" { maxAzs 2 }

                  let! efsFs = efsFileSystem "efs" { vpc vpcFs }

                  let! ap =
                      accessPoint "ap" {
                          fileSystem efsFs
                          path "/export/lambda"
                          posixUser "1000" "1000"
                          createAcl "1000" "1000" "750"
                      }

                  // Empty configuration should throw
                  let thrower1 () =
                      lambda "fn-fs1" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                          fileSystem (lambdaFileSystem { () })
                      }
                      |> ignore

                  // Missing localMountPath should throw
                  let thrower2 () =
                      lambda "fn-fs2" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                          fileSystem (lambdaFileSystem { arn ap.AccessPointArn })
                      }
                      |> ignore

                  // Missing accessPoint should throw
                  let thrower3 () =
                      lambda "fn-fs3" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                          fileSystem (lambdaFileSystem { localMountPath "/mnt/data" })
                      }
                      |> ignore

                  Expect.throws thrower1 "Empty FileSystem configuration should throw"
                  Expect.throws thrower2 "FileSystem config missing localMountPath should throw"
                  Expect.throws thrower3 "FileSystem config missing accessPoint should throw"
              }

          }

          test "access point CE creates correct resource" {
              // let stack = Stack(App())
              // // Create a VPC since it's required for EFS
              // let vpc = Vpc(stack, "vpc", VpcProps())
              // let efsFs = Amazon.CDK.AWS.EFS.FileSystem(stack, "efs", FileSystemProps(Vpc = vpc))

              stack "LambdaAP" {

                  let! vpcFs = vpc "vpc" { maxAzs 2 }

                  let! efsFs = efsFileSystem "efs" { vpc vpcFs }

                  let! ap =
                      accessPoint "ap" {
                          fileSystem efsFs
                          path "/export/lambda"
                          posixUser "1000" "1000"
                          createAcl "1000" "1000" "750"
                      }

                  // Check construction result (basic sanity: id and stack)
                  Expect.isNotNull ap "AccessPoint should be created"
                  Expect.equal ap.Node.Id "ap" "AccessPoint id should match"
              }
          }

          test "access point CE validates required properties" {
              stack "LambdaAP" {
                  let! vpcFs = vpc "vpc" { maxAzs 2 }

                  let! efsFs = efsFileSystem "efs" { vpc vpcFs }

                  // Only FileSystem is required
                  let! ap = accessPoint "minimal" { fileSystem efsFs }

                  // Verify minimal configuration creates a valid resource
                  Expect.isNotNull ap "AccessPoint should be created"
                  Expect.equal ap.Node.Id "minimal" "AccessPoint id should match"
                  Expect.isNotNull ap "AccessPoint should be created"

              }
          }

          test "file system CE properties are immutable" {
              stack "LambdaFS" {
                  // Create a VPC since it's required for EFS
                  let! vpcFs = vpc "vpc" { maxAzs 2 }

                  let! efsFs = efsFileSystem "efs" { vpc vpcFs }

                  let! ap =
                      accessPoint "ap" {
                          fileSystem efsFs
                          path "/export/lambda"
                          posixUser "1000" "1000"
                          createAcl "1000" "1000" "750"
                      }

                  // First configuration
                  let spec1 =
                      lambda "fn-fs1" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          fileSystem (
                              lambdaFileSystem {
                                  arn ap.AccessPointArn
                                  localMountPath "/mnt/data1"
                              }
                          )
                      }

                  // Second configuration with same access point but different mount path
                  let spec2 =
                      lambda "fn-fs2" {
                          handler "Program::Handler"
                          runtime Runtime.DOTNET_8
                          code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                          fileSystem (
                              lambdaFileSystem {
                                  arn ap.AccessPointArn
                                  localMountPath "/mnt/data2"
                              }
                          )
                      }

                  Expect.isNotNull spec1.Props.Filesystem "FileSystem should be set in first config"
                  Expect.isNotNull spec2.Props.Filesystem "FileSystem should be set in second config"
              }
          }

          test "implicit yield sets VPC subnet selection correctly" {
              let spec =
                  lambda "fn-vpc" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      vpcSubnets (subnetSelection { subnetType SubnetType.PRIVATE_WITH_EGRESS })
                  }

              let subnetTypeObj = box spec.Props.VpcSubnets.SubnetType

              match subnetTypeObj with
              | :? System.Nullable<SubnetType> as n when n.HasValue ->
                  Expect.equal n.Value SubnetType.PRIVATE_WITH_EGRESS "VPC subnet type should be set correctly"
              | :? SubnetType as t ->
                  Expect.equal t SubnetType.PRIVATE_WITH_EGRESS "VPC subnet type should be set correctly"
              | _ -> failtestf $"Unexpected SubnetType type/value: %A{subnetTypeObj}"
          }
          test "multiple implicit yields combine correctly" {
              let spec =
                  lambda "fn-multi" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      architecture Architecture.ARM_64
                      tracing Tracing.ACTIVE

                      vpcSubnets (
                          subnetSelection {
                              subnetType SubnetType.PUBLIC
                              availabilityZones [ "us-east-1a"; "us-east-1b" ]
                          }
                      )
                  }

              Expect.equal spec.Props.Architecture Architecture.ARM_64 "Architecture should be set correctly"
              let subnetTypeObj = box spec.Props.VpcSubnets.SubnetType

              match subnetTypeObj with
              | :? System.Nullable<SubnetType> as n when n.HasValue ->
                  Expect.equal n.Value SubnetType.PUBLIC "VPC subnet type should be set correctly"
              | :? SubnetType as t -> Expect.equal t SubnetType.PUBLIC "VPC subnet type should be set correctly"
              | _ -> failtestf $"Unexpected SubnetType type/value: %A{subnetTypeObj}"

              let az = spec.Props.VpcSubnets.AvailabilityZones

              Expect.equal
                  (az |> Array.toList)
                  [ "us-east-1a"; "us-east-1b" ]
                  "Availability zones should be set correctly"

              let tracingObj = box spec.Props.Tracing

              match tracingObj with
              | :? System.Nullable<Tracing> as n when n.HasValue ->
                  Expect.equal n.Value Tracing.ACTIVE "Tracing should be set correctly"
              | :? Tracing as t -> Expect.equal t Tracing.ACTIVE "Tracing should be set correctly"
              | _ -> failtestf $"Unexpected Tracing type/value: %A{tracingObj}"
          }

          test "version options CE yields correctly" {
              let spec =
                  lambda "fn-version" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))

                      currentVersionOptions (
                          versionOptions {
                              description "v1.0.0"
                              removalPolicy RemovalPolicy.RETAIN
                              codeSha256 "abc123"
                          }
                      )
                  }

              Expect.equal
                  spec.Props.CurrentVersionOptions.Description
                  "v1.0.0"
                  "Version description should be set correctly"

              let removalPolicyObj = box spec.Props.CurrentVersionOptions.RemovalPolicy

              match removalPolicyObj with
              | :? System.Nullable<RemovalPolicy> as n when n.HasValue ->
                  Expect.equal n.Value RemovalPolicy.RETAIN "Version removal policy should be set correctly"
              | :? RemovalPolicy as p ->
                  Expect.equal p RemovalPolicy.RETAIN "Version removal policy should be set correctly"
              | _ -> failtestf $"Unexpected RemovalPolicy type/value: %A{removalPolicyObj}"

              Expect.equal
                  spec.Props.CurrentVersionOptions.CodeSha256
                  "abc123"
                  "Version code SHA256 should be set correctly"
          }

          test "ephemeralStorageSize sets storage correctly" {
              let spec =
                  lambda "StorageFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory(), S3.excludeCommonAssetDirs))
                      ephemeralStorageSize 1024
                  }

              Expect.isNotNull spec.Props.EphemeralStorageSize "EphemeralStorageSize should be set"

              Expect.equal
                  (spec.Props.EphemeralStorageSize.ToString())
                  (Size.Mebibytes(1024.0).ToString())
                  "EphemeralStorageSize should be 1024 MB"
          } ]
    |> testSequenced
