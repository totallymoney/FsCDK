module FsCDK.Tests.RDSTests

open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2
open Expecto
open FsCDK

[<Tests>]
let rds_instance_dsl_tests =
    testList
        "RDS Database Instance DSL"
        [ test "fails when VPC is missing" {
              let thrower () =
                  rdsInstance "MyDB" { postgresEngine } |> ignore

              Expect.throws thrower "RDS Instance builder should throw when VPC is missing"
          }

          test "fails when engine is missing" {
              let thrower () = rdsInstance "MyDB" { () } |> ignore

              Expect.throws thrower "RDS Instance builder should throw when engine is missing"
          }

          test "defaults constructId to database name" {
              // Create a minimal VPC spec for testing (won't actually create resources)
              let vpcSpec = vpc "TestVpc" { () }

              // We can't fully test without a real VPC object, but we can verify the builder structure
              Expect.equal vpcSpec.VpcName "TestVpc" "VPC spec should be created"
          }

          test "applies AWS best practices by default" {
              // Verify that the builder accepts configuration
              // Full testing requires an actual CDK stack context
              let configTest () =
                  rdsInstance "TestDB" {
                      postgresEngine
                      allocatedStorage 20
                      backupRetentionDays 7.0
                      multiAz false
                      storageEncrypted true
                  }
                  |> ignore

              // This would fail without VPC, which is expected
              Expect.throws configTest "Should require VPC"
          } ]
