module FsCDK.Tests.NetworkLoadBalancerTests

open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Expecto
open FsCDK

[<Tests>]
let network_load_balancer_tests =
    testList
        "Network Load Balancer DSL"
        [ test "requires VPC configuration" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec = networkLoadBalancer "MyNLB" { vpc ivpc }

              Expect.equal nlbSpec.LoadBalancerName "MyNLB" "Should accept VPC spec"
          }

          test "defaults to internal (not internet-facing)" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec = networkLoadBalancer "MyNLB" { vpc ivpc }

              Expect.equal nlbSpec.Props.InternetFacing.Value false "Should default to internal"
          }

          test "defaults to cross-zone enabled" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec = networkLoadBalancer "MyNLB" { vpc ivpc }

              Expect.equal nlbSpec.Props.CrossZoneEnabled.Value true "Should default to cross-zone enabled"
          }

          test "allows internet-facing configuration" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec =
                  networkLoadBalancer "MyNLB" {
                      vpc ivpc
                      internetFacing true
                  }

              Expect.equal nlbSpec.Props.InternetFacing.Value true "Should allow internet-facing"
          }

          test "allows deletion protection" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec =
                  networkLoadBalancer "MyNLB" {
                      vpc ivpc
                      deletionProtection true
                  }

              Expect.equal nlbSpec.Props.DeletionProtection.Value true "Should allow deletion protection"
          }

          test "defaults constructId to load balancer name" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec = networkLoadBalancer "MyNLB" { vpc ivpc }

              Expect.equal nlbSpec.ConstructId "MyNLB" "ConstructId should default to name"
          }

          test "allows custom constructId" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec =
                  networkLoadBalancer "MyNLB" {
                      vpc ivpc
                      constructId "CustomNLB"
                  }

              Expect.equal nlbSpec.ConstructId "CustomNLB" "Should use custom constructId"
          }

          test "allows custom load balancer name" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec =
                  networkLoadBalancer "MyNLB" {
                      stack
                      vpc ivpc
                      loadBalancerName "production-nlb"
                  }

              Expect.equal nlbSpec.Props.LoadBalancerName "production-nlb" "Should use custom name"
          }

          test "defaults IP address type to IPv4" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let nlbSpec = networkLoadBalancer "MyNLB" { vpc ivpc }

              Expect.equal nlbSpec.Props.IpAddressType.Value IpAddressType.IPV4 "Should default to IPv4"
          } ]
    |> testSequenced
