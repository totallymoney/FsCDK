module FsCDK.Tests.ApiGatewayTests

open Amazon.CDK
open Amazon.CDK.AWS.APIGateway
open Amazon.CDK.AWS.Lambda
open Expecto
open FsCDK

[<Tests>]
let restApi_tests =
    testList
        "REST API DSL"
        [ test "creates REST API with default settings" {
              let spec = restApi "MyApi" { () }

              Expect.equal spec.ApiName "MyApi" "API name should be set"
              Expect.equal spec.ConstructId "MyApi" "ConstructId should default to API name"

              Expect.equal spec.Props.RestApiName "MyApi" "RestApiName should be set in props"

              Expect.isTrue
                  (spec.Props.Deploy |> Option.ofNullable |> Option.defaultValue false)
                  "Deploy should be enabled by default"

              Expect.isTrue
                  (spec.Props.CloudWatchRole |> Option.ofNullable |> Option.defaultValue false)
                  "CloudWatch role should be enabled by default"

              Expect.equal spec.Props.EndpointTypes.Length 1 "Should have 1 endpoint type"
              Expect.equal spec.Props.EndpointTypes.[0] EndpointType.REGIONAL "Should default to REGIONAL"
          }

          test "creates REST API with custom settings" {
              let spec =
                  restApi "CustomApi" {
                      description "My Custom API"
                      endpointType EndpointType.EDGE
                      deploy false
                      cloudWatchRole false
                  }

              Expect.equal (spec.Props.Description |> Option.ofObj) (Some "My Custom API") "Description should be set"

              Expect.equal spec.Props.EndpointTypes.[0] EndpointType.EDGE "Should use EDGE endpoint"

              Expect.isFalse
                  (spec.Props.Deploy |> Option.ofNullable |> Option.defaultValue true)
                  "Deploy should be disabled"

              Expect.isFalse
                  (spec.Props.CloudWatchRole |> Option.ofNullable |> Option.defaultValue true)
                  "CloudWatch role should be disabled"
          }

          test "uses custom constructId when provided" {
              let spec = restApi "MyApi" { constructId "CustomApiId" }

              Expect.equal spec.ConstructId "CustomApiId" "Custom constructId should be used"
          }

          test "allows multiple endpoint types" {
              let spec =
                  restApi "MultiEndpointApi" { endpointTypes [ EndpointType.REGIONAL; EndpointType.EDGE ] }

              Expect.equal spec.Props.EndpointTypes.Length 2 "Should have 2 endpoint types"
          }

          test "supports binary media types" {
              let spec =
                  restApi "BinaryApi" {
                      binaryMediaType "image/png"
                      binaryMediaType "application/pdf"
                  }

              Expect.equal spec.Props.BinaryMediaTypes.Length 2 "Should have 2 binary media types"
              Expect.contains spec.Props.BinaryMediaTypes "image/png" "Should contain image/png"
          }

          test "supports disabling execute-api endpoint" {
              let spec = restApi "PrivateApi" { disableExecuteApiEndpoint true }

              Expect.isTrue
                  (spec.Props.DisableExecuteApiEndpoint
                   |> Option.ofNullable
                   |> Option.defaultValue false)
                  "Execute-api endpoint should be disabled"
          } ]

[<Tests>]
let tokenAuthorizer_tests =
    testList
        "Token Authorizer DSL"
        [ test "fails when handler is missing" {
              let thrower () =
                  tokenAuthorizer "MyAuth" { () } |> ignore

              Expect.throws thrower "Token Authorizer should throw when handler is missing"
          }

          ]

[<Tests>]
let vpcLink_tests =
    testList
        "VPC Link DSL"
        [ test "creates VPC Link with default settings" {
              let spec = vpcLink "MyVpcLink" { () }

              Expect.equal spec.VpcLinkName "MyVpcLink" "VPC Link name should be set"
              Expect.equal spec.ConstructId "MyVpcLink" "ConstructId should default to VPC Link name"
          }

          test "uses custom constructId when provided" {
              let spec = vpcLink "MyVpcLink" { constructId "CustomLinkId" }

              Expect.equal spec.ConstructId "CustomLinkId" "Custom constructId should be used"
          }

          test "supports description" {
              let spec = vpcLink "DescribedLink" { description "Link for private resources" }

              Expect.equal
                  (spec.Props.Description |> Option.ofObj)
                  (Some "Link for private resources")
                  "Description should be set"
          }

          test "supports custom VPC link name" {
              let spec = vpcLink "MyLink" { vpcLinkName "custom-vpc-link-name" }

              Expect.equal
                  (spec.Props.VpcLinkName |> Option.ofObj)
                  (Some "custom-vpc-link-name")
                  "Custom VPC link name should be set"
          } ]
    |> testSequenced
