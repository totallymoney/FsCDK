module FsCDK.Tests.ECSTaskDefinitionTests

open Amazon.CDK
open Amazon.CDK.AWS.ECS
open Expecto
open FsCDK

[<Tests>]
let fargateTaskDefinition_tests =
    testList
        "Fargate Task Definition DSL"
        [ test "creates task definition with default settings" {
              let spec = fargateTaskDefinition "MyTask" { () }

              Expect.equal spec.TaskDefinitionName "MyTask" "Task definition name should be set"

              Expect.equal spec.ConstructId "MyTask" "ConstructId should default to task definition name"

              Expect.equal
                  (spec.Props.Cpu |> Option.ofNullable |> Option.map int |> Option.defaultValue 0)
                  256
                  "Should default to 256 CPU units"

              Expect.equal
                  (spec.Props.MemoryLimitMiB
                   |> Option.ofNullable
                   |> Option.map int
                   |> Option.defaultValue 0)
                  512
                  "Should default to 512 MB memory"
          }

          test "creates task definition with custom CPU and memory" {
              let spec =
                  fargateTaskDefinition "CustomTask" {
                      cpu 1024
                      memory 2048
                  }

              Expect.equal
                  (spec.Props.Cpu |> Option.ofNullable |> Option.map int |> Option.defaultValue 0)
                  1024
                  "Should use custom CPU"

              Expect.equal
                  (spec.Props.MemoryLimitMiB
                   |> Option.ofNullable
                   |> Option.map int
                   |> Option.defaultValue 0)
                  2048
                  "Should use custom memory"
          }

          test "uses custom constructId when provided" {
              let spec = fargateTaskDefinition "MyTask" { constructId "CustomTaskId" }

              Expect.equal spec.ConstructId "CustomTaskId" "Custom constructId should be used"
          }


          test "supports family name" {
              let spec = fargateTaskDefinition "MyTask" { family "my-task-family" }

              Expect.equal (spec.Props.Family |> Option.ofObj) (Some "my-task-family") "Family name should be set"
          }

          test "supports ephemeral storage configuration" {
              let spec = fargateTaskDefinition "StorageTask" { ephemeralStorageGiB 30 }

              Expect.equal
                  (spec.Props.EphemeralStorageGiB
                   |> Option.ofNullable
                   |> Option.map int
                   |> Option.defaultValue 0)
                  30
                  "Ephemeral storage should be set"
          }

          test "supports volumes" {
              let mvolume = Volume(Name = "my-volume")

              let spec = fargateTaskDefinition "VolumeTask" { volume mvolume }

              Expect.equal spec.Props.Volumes.Length 1 "Should have 1 volume"
              Expect.equal spec.Props.Volumes.[0].Name "my-volume" "Volume name should match"
          }

          test "supports multiple volumes" {
              let volume1 = Volume(Name = "volume1")
              let volume2 = Volume(Name = "volume2")

              let spec = fargateTaskDefinition "MultiVolumeTask" { volumes [ volume1; volume2 ] }

              Expect.equal spec.Props.Volumes.Length 2 "Should have 2 volumes"
          } ]
    |> testSequenced
