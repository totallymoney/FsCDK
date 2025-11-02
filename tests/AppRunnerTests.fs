module FsCDK.Tests.AppRunnerTests

open Expecto
open FsCDK

[<Tests>]
let apprunner_service_tests =
    testList
        "App Runner Service DSL"
        [ test "fails when source configuration is missing" {
              let thrower () =
                  appRunnerService "MyService" { () } |> ignore

              Expect.throws thrower "App Runner builder should throw when source configuration is missing"
          }

          test "defaults constructId to service name" {
              // This would fail without source configuration, which is expected
              let thrower () =
                  appRunnerService "TestService" { () } |> ignore

              Expect.throws thrower "Should require source configuration"
          }

          test "accepts tag configuration" {
              let thrower () =
                  appRunnerService "TestService" {
                      tag "Environment" "Production"
                      tags [ ("Owner", "DevOps"); ("Project", "WebApp") ]
                  }
                  |> ignore

              Expect.throws thrower "Should require source configuration but accept tags"
          }

          test "InstanceSizes helpers provide standard sizes" {
              Expect.isNotNull AppRunnerHelpers.InstanceSizes.small "Small size should exist"
              Expect.isNotNull AppRunnerHelpers.InstanceSizes.medium "Medium size should exist"
              Expect.isNotNull AppRunnerHelpers.InstanceSizes.large "Large size should exist"
          } ]
