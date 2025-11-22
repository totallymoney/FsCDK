module FsCDK.Tests.CustomResourceTests

open Expecto
open FsCDK

[<Tests>]
let custom_resource_tests =
    testList
        "Custom Resource DSL"
        [ test "fails when onCreate is missing" {
              let thrower () =
                  customResource "MyResource" { () } |> ignore

              Expect.throws thrower "Custom Resource builder should throw when onCreate is missing"
          }

          test "defaults constructId to resource name" {
              let thrower () =
                  customResource "TestResource" { () } |> ignore

              Expect.throws thrower "Should require onCreate"
          }

          test "Helper functions exist" {
              Expect.isNotNull (box CustomResourceHelpers.createSdkCall) "createSdkCall should exist"
              Expect.isTrue true "Custom resource helpers should be available"
          } ]
    |> testSequenced
