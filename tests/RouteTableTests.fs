module FsCDK.Tests.RouteTableTests

open Amazon.CDK.AWS.EC2
open Expecto
open FsCDK

[<Tests>]
let route_table_tests =
    testList
        "Route Table DSL"
        [ test "requires VPC configuration" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let rtSpec = routeTable "MyRouteTable" { vpc ivpc }

              Expect.equal rtSpec.RouteTableName "MyRouteTable" "Should accept VPC spec"
          }

          test "accepts tags" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let rtSpec =
                  routeTable "MyRouteTable" {
                      vpc ivpc
                      tag "Name" "custom-route-table"
                      tag "Environment" "production"
                  }

              Expect.equal rtSpec.Props.Tags.Length 2 "Should have 2 tags"
          }

          test "defaults constructId to route table name" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let rtSpec = routeTable "MyRouteTable" { vpc ivpc }

              Expect.equal rtSpec.ConstructId "MyRouteTable" "ConstructId should default to name"
          }

          test "allows custom constructId" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let rtSpec =
                  routeTable "MyRouteTable" {
                      vpc ivpc
                      constructId "CustomRouteTable"
                  }

              Expect.equal rtSpec.ConstructId "CustomRouteTable" "Should use custom constructId"
          }

          test "tag names and values are correct" {
              let stack = Amazon.CDK.Stack(Amazon.CDK.App(), "Test")

              let ivpc =
                  Amazon.CDK.AWS.EC2.Vpc(stack, "vpc", Amazon.CDK.AWS.EC2.VpcProps()) :> Amazon.CDK.AWS.EC2.IVpc

              let rtSpec =
                  routeTable "MyRouteTable" {
                      vpc ivpc
                      tag "Environment" "dev"
                  }

              let tag = rtSpec.Props.Tags.[0]
              Expect.equal tag.Key "Environment" "Tag key should be correct"
              Expect.equal tag.Value "dev" "Tag value should be correct"
          } ]
    |> testSequenced
