module FsCDK.Tests.DocumentDBTests

open Expecto
open FsCDK

[<Tests>]
let documentdb_cluster_tests =
    testList
        "DocumentDB Cluster DSL"
        [ test "fails when VPC is missing" {
              let thrower () =
                  documentDBCluster "MyDocDB" { () } |> ignore

              Expect.throws thrower "DocumentDB builder should throw when VPC is missing"
          }

          test "fails when master password is missing" {
              stack "TestStack" {
                  let! vpcSpec = vpc "TestVpc" { () }

                  let thrower () =
                      documentDBCluster "MyDocDB" { vpc vpcSpec } |> ignore

                  Expect.throws thrower "DocumentDB builder should throw when master password is missing"
              }
          }

          test "defaults constructId to cluster name" {
              let thrower () =
                  documentDBCluster "TestDocDB" { () } |> ignore

              Expect.throws thrower "Should require VPC"
          }

          test "InstanceTypes helpers provide standard sizes" {
              Expect.equal DocumentDBHelpers.InstanceTypes.t3_medium "db.t3.medium" "t3.medium type should match"
              Expect.equal DocumentDBHelpers.InstanceTypes.r5_large "db.r5.large" "r5.large type should match"
          } ]
    |> testSequenced
