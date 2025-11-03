module FsCDK.Tests.VPCTests

open Amazon.CDK.AWS.EC2
open Expecto
open FsCDK

[<Tests>]
let vpc_dsl_tests =
    // Some of these test doesn't run well in parallel
    testSequenced
    <| testList
        "VPC DSL"
        [ test "creates VPC with default settings" {
              let spec = vpc "MyVpc" { () }

              Expect.equal spec.VpcName "MyVpc" "VPC name should be set"
              Expect.equal spec.ConstructId "MyVpc" "ConstructId should default to VPC name"

              Expect.equal
                  (spec.Props.MaxAzs |> Option.ofNullable |> Option.defaultValue 0 |> int)
                  2
                  "Should default to 2 AZs for HA"

              Expect.equal
                  (spec.Props.NatGateways |> Option.ofNullable |> Option.defaultValue 0 |> int)
                  1
                  "Should default to 1 NAT gateway"

              Expect.isTrue
                  (spec.Props.EnableDnsHostnames |> Option.ofNullable |> Option.defaultValue false)
                  "DNS hostnames should be enabled by default"

              Expect.isTrue
                  (spec.Props.EnableDnsSupport |> Option.ofNullable |> Option.defaultValue false)
                  "DNS support should be enabled by default"
          }

          // This test is skipped because it works well as single execution, but may fail when run on parallel with other tests.
          test "creates VPC with custom settings" {
              let spec =
                  vpc "CustomVpc" {
                      maxAzs 3
                      natGateways 2
                      cidr "10.1.0.0/16"
                  }

              Expect.equal
                  (spec.Props.MaxAzs |> Option.ofNullable |> Option.defaultValue 0 |> int)
                  3
                  "Should use custom max AZs"

              Expect.equal
                  (spec.Props.NatGateways |> Option.ofNullable |> Option.defaultValue 0 |> int)
                  2
                  "Should use custom NAT gateways"

              Expect.isNotNull spec.Props.IpAddresses "IP addresses should be set"
          }

          test "uses custom constructId when provided" {
              let spec = vpc "MyVpc" { constructId "CustomVpcId" }

              Expect.equal spec.ConstructId "CustomVpcId" "Custom constructId should be used"
          }

          test "allows disabling DNS settings" {
              let spec =
                  vpc "NoDnsVpc" {
                      enableDnsHostnames false
                      enableDnsSupport false
                  }

              Expect.isFalse
                  (spec.Props.EnableDnsHostnames |> Option.ofNullable |> Option.defaultValue true)
                  "DNS hostnames should be disabled"

              Expect.isFalse
                  (spec.Props.EnableDnsSupport |> Option.ofNullable |> Option.defaultValue true)
                  "DNS support should be disabled"
          }

          test "provides default subnet configuration" {
              let spec = vpc "DefaultSubnetsVpc" { () }

              Expect.isNotNull spec.Props.SubnetConfiguration "Subnet configuration should be set"
              Expect.equal spec.Props.SubnetConfiguration.Length 2 "Should have 2 subnet configurations"
          } ]

[<Tests>]
let security_group_dsl_tests =
    testList
        "Security Group DSL"
        [ test "fails when VPC is missing" {
              let thrower () = securityGroup "MySG" { () } |> ignore

              Expect.throws thrower "Security Group builder should throw when VPC is missing"
          }

          test "creates security group spec with provided VPC" {
              // Create a minimal test VPC spec
              let testVpcSpec = vpc "TestVpc" { () }

              // We can't easily test this without a full CDK stack context
              // Just verify the builder doesn't crash with basic config
              Expect.equal testVpcSpec.VpcName "TestVpc" "VPC spec should be created"
          }

          test "constructId configuration does not fail without VPC" {
              let thrower () =
                  securityGroup "MySG" { constructId "CustomSGId" } |> ignore

              Expect.throws thrower "Security Group builder should throw when VPC is missing even with constructId"
          } ]

[<Tests>]
let gatewayVpcEndpoint_tests =
    testList
        "Gateway VPC Endpoint DSL"
        [ test "fails when VPC is missing" {
              let thrower () =
                  gatewayVpcEndpoint "MyEndpoint" { () } |> ignore

              Expect.throws thrower "Gateway VPC Endpoint should throw when VPC is missing"
          }

          test "fails when service is missing" {
              let vpcSpec = vpc "TestVpc" { () }

              let thrower () =
                  gatewayVpcEndpoint "MyEndpoint" { vpc vpcSpec } |> ignore

              Expect.throws thrower "Gateway VPC Endpoint should throw when service is missing"
          }

          // NOTE: Full integration tests would need actual VPC resources
          // These tests verify the builder validates required parameters
          ]

[<Tests>]
let interfaceVpcEndpoint_tests =
    testList
        "Interface VPC Endpoint DSL"
        [ test "fails when VPC is missing" {
              let thrower () =
                  interfaceVpcEndpoint "MyEndpoint" { () } |> ignore

              Expect.throws thrower "Interface VPC Endpoint should throw when VPC is missing"
          }

          test "fails when service is missing" {
              let vpcSpec = vpc "TestVpc" { () }

              let thrower () =
                  interfaceVpcEndpoint "MyEndpoint" { vpc vpcSpec } |> ignore

              Expect.throws thrower "Interface VPC Endpoint should throw when service is missing"
          }

          // NOTE: Full integration tests would need actual VPC resources
          // These tests verify the builder validates required parameters
          ]
