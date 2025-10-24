module FsCDK.Tests.BastionHostTests

open Amazon.CDK.AWS.EC2
open Expecto
open FsCDK

[<Tests>]
let bastion_host_tests =
    testSequenced
    <| testList
        "Bastion Host DSL"
        [ test "requires VPC configuration" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let bastionSpec = bastionHost "MyBastion" { vpc ivpc }

              Expect.equal bastionSpec.BastionName "MyBastion" "Should accept VPC spec"
          }

          test "defaults to require IMDSv2" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let bastionSpec = bastionHost "MyBastion" { vpc ivpc }

              Expect.equal bastionSpec.Props.RequireImdsv2.Value true "Should require IMDSv2 by default"
          }

          test "allows custom instance type" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let instance = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO)

              let bastionSpec =
                  bastionHost "MyBastion" {
                      vpc ivpc
                      instanceType instance
                  }

              Expect.equal bastionSpec.Props.InstanceType instance "Should use custom instance type"
          }

          test "allows custom instance name" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let bastionSpec =
                  bastionHost "MyBastion" {
                      vpc ivpc
                      instanceName "production-bastion"
                  }

              Expect.equal bastionSpec.Props.InstanceName "production-bastion" "Should use custom name"
          }

          test "defaults constructId to bastion name" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let bastionSpec = bastionHost "MyBastion" { vpc ivpc }

              Expect.equal bastionSpec.ConstructId "MyBastion" "ConstructId should default to name"
          }

          test "allows disabling IMDSv2" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let bastionSpec =
                  bastionHost "MyBastion" {
                      vpc ivpc
                      requireImdsv2 false
                  }

              Expect.equal bastionSpec.Props.RequireImdsv2.Value false "Should allow disabling IMDSv2"
          } ]
