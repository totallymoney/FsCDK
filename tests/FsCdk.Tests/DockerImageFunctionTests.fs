module FsCdk.Tests.DockerImageFunctionTests

open Expecto
open FsCDK

[<Tests>]
let docker_image_lambda_dsl_tests =
    testList
        "DockerImage Lambda DSL"
        [ test "fails when function name is missing" {
              let thrower () =
                  dockerImageFunction { code (System.IO.Directory.GetCurrentDirectory()) }
                  |> ignore

              Expect.throws thrower "Builder should throw when name is missing"
          }

          test "fails when image asset path is missing" {
              let thrower () =
                  dockerImageFunction { functionName "ImgFn" } |> ignore

              Expect.throws thrower "Builder should throw when image path is missing"
          }

          test "defaults constructId to function name" {
              let spec =
                  dockerImageFunction {
                      functionName "UsersImgFn"
                      code (System.IO.Directory.GetCurrentDirectory())
                  }

              Expect.equal spec.FunctionName "UsersImgFn" "FunctionName should be set"
              Expect.equal spec.ConstructId "UsersImgFn" "ConstructId should default to function name"
          }

          test "uses custom constructId when provided" {
              let spec =
                  dockerImageFunction {
                      functionName "UsersImgFn"
                      constructId "UsersImageFunction"
                      code (System.IO.Directory.GetCurrentDirectory())
                  }

              Expect.equal spec.ConstructId "UsersImageFunction" "Custom constructId should be used"
          }

          test "applies environment variables when configured" {
              let spec =
                  dockerImageFunction {
                      functionName "EnvImgFn"
                      code (System.IO.Directory.GetCurrentDirectory())
                      environment [ ("A", "1"); ("B", "2") ]
                  }

              Expect.isNotNull spec.Props.Environment "Environment should be set"
              Expect.equal spec.Props.Environment.Count 2 "Should have two env vars"
              Expect.equal spec.Props.Environment["A"] "1" "Env var A should be 1"
              Expect.equal spec.Props.Environment["B"] "2" "Env var B should be 2"
          }

          test "applies optional properties when configured" {
              let spec =
                  dockerImageFunction {
                      functionName "OptsImgFn"
                      code (System.IO.Directory.GetCurrentDirectory())
                      timeout 10.0
                      memorySize 512
                      description "My docker image function"
                  }

              // MemorySize can be int or Nullable<double> depending on CDK version; handle both
              let memObj = box spec.Props.MemorySize

              match memObj with
              | :? int as i -> Expect.equal i 512 "Memory size should be set to 512"
              | :? System.Nullable<double> as n when n.HasValue ->
                  Expect.equal (int n.Value) 512 "Memory size should be set to 512"
              | _ -> failtestf $"Unexpected MemorySize type/value: %A{memObj}"

              Expect.equal spec.Props.Description "My docker image function" "Description should match"
          } ]
