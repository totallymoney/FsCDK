module FsCDK.Tests.HttpApiTests

open Amazon.CDK
open Amazon.CDK.Assertions
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.Apigatewayv2
open Expecto
open FsCDK
open FsCdk.Tests.TestHelpers

let private handlerFn name =
    lambda name {
        runtime Runtime.DOTNET_8
        handler "Api::Api.Handler::handle"
        code (Lambda.dummyAssetDir ())
    }

[<Tests>]
let httpApiTests =
    testList
        "HttpApi builder"
        [ test "creates an HttpApi with Cognito authorizer, shared integration and public route" {
              let app = App()

              stack "HttpApiCognito" {
                  scope app
                  let! fn = handlerFn "ItemsFn"
                  let! pool = userPool "Users" { () }
                  let! client = userPoolClient "WebClient" { userPool pool }

                  let! api =
                      httpApi "ItemsApi" {
                          cognitoAuthorizer "Cognito" pool [ client ]
                          route HttpMethod.GET "/items" fn
                          route HttpMethod.POST "/items" fn
                          publicRoute HttpMethod.GET "/health" fn
                      }

                  cfnOutput "ApiUrl" { value api.Url }
              }

              let template = Templates.ofStack app "HttpApiCognito"

              template.ResourceCountIs("AWS::ApiGatewayV2::Api", 1.0)
              template.HasResourceProperties("AWS::ApiGatewayV2::Api", Templates.obj [ "ProtocolType", "HTTP" ])

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Authorizer",
                  Templates.obj
                      [ "AuthorizerType", "JWT"
                        "IdentitySource", [| "$request.header.Authorization" |] ]
              )

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Route",
                  Templates.obj [ "RouteKey", "GET /items"; "AuthorizationType", "JWT" ]
              )

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Route",
                  Templates.obj [ "RouteKey", "POST /items"; "AuthorizationType", "JWT" ]
              )

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Route",
                  Templates.obj [ "RouteKey", "GET /health"; "AuthorizationType", "NONE" ]
              )

              // One integration for the function, one invoke permission per route
              template.ResourceCountIs("AWS::ApiGatewayV2::Integration", 1.0)

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Integration",
                  Templates.obj [ "IntegrationType", "AWS_PROXY"; "PayloadFormatVersion", "2.0" ]
              )

              template.ResourceCountIs("AWS::Lambda::Permission", 3.0)
              template.HasOutput("ApiUrl", Match.AnyValue())
          }

          test "JWT authorizer uses issuer and audience" {
              let app = App()

              stack "HttpApiJwt" {
                  scope app
                  let! fn = handlerFn "JwtFn"

                  httpApi "JwtApi" {
                      jwtAuthorizer "Auth0" "https://example.eu.auth0.com/" [ "https://api.example.com" ]
                      defaultAuthorizationScopes [ "items:read" ]
                      route HttpMethod.ANY "/{proxy+}" fn
                  }
              }

              let template = Templates.ofStack app "HttpApiJwt"

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Authorizer",
                  Templates.obj
                      [ "JwtConfiguration",
                        Templates.obj
                            [ "Issuer", "https://example.eu.auth0.com/"
                              "Audience", [| "https://api.example.com" |] ] ]
              )

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Route",
                  Templates.obj [ "RouteKey", "ANY /{proxy+}"; "AuthorizationScopes", [| "items:read" |] ]
              )
          }

          test "throttle, access logs and custom domain land on the default stage" {
              let app = App()

              stack "HttpApiStage" {
                  scope app
                  let! fn = handlerFn "StageFn"
                  let! logs = logGroup "/fscdk/api-access" { retention RetentionDays.ONE_YEAR }
                  let! cert = certificate "ApiCert" { domainName "api.example.com" }

                  httpApi "StageApi" {
                      route HttpMethod.GET "/items" fn
                      throttle 50.0 100
                      accessLogs logs
                      customDomain "api.example.com" cert
                      disableExecuteApiEndpoint true
                  }
              }

              let template = Templates.ofStack app "HttpApiStage"

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Stage",
                  Templates.obj
                      [ "StageName", "$default"
                        "AutoDeploy", true
                        "DefaultRouteSettings", Templates.obj [ "ThrottlingRateLimit", 50; "ThrottlingBurstLimit", 100 ]
                        "AccessLogSettings",
                        Templates.obj
                            [ "Format", Match.StringLikeRegexp("\\$context.requestId")
                              "DestinationArn", Match.AnyValue() ] ]
              )

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::DomainName",
                  Templates.obj [ "DomainName", "api.example.com" ]
              )

              template.ResourceCountIs("AWS::ApiGatewayV2::ApiMapping", 1.0)

              template.HasResourceProperties(
                  "AWS::ApiGatewayV2::Api",
                  Templates.obj [ "DisableExecuteApiEndpoint", true ]
              )
          }

          test "throttle without a default stage fails" {
              let app = App()

              Expect.throws
                  (fun () ->
                      stack "HttpApiNoStage" {
                          scope app
                          let! fn = handlerFn "NoStageFn"

                          httpApi "NoStageApi" {
                              createDefaultStage false
                              route HttpMethod.GET "/items" fn
                              throttle 10.0 10
                          }
                      })
                  "throttle needs the default stage"
          }

          test "JWT authorizer without audience fails" {
              Expect.throws
                  (fun () -> httpApi "NoAudience" { jwtAuthorizer "Jwt" "https://issuer/" [] } |> ignore)
                  "audience is required"
          }

          test "Cognito authorizer without clients fails" {
              let app = App()

              Expect.throws
                  (fun () ->
                      stack "NoClients" {
                          scope app
                          let! pool = userPool "NoClientsPool" { () }
                          httpApi "NoClientsApi" { cognitoAuthorizer "Cognito" pool [] }
                      })
                  "clients are required"
          }

          test "spec records routes in order" {
              let spec = httpApi "Spec" { description "d" }

              Expect.equal spec.ApiName "Spec" "name"
              Expect.isNone spec.HttpApi "not created before the stack runs"
              Expect.isEmpty spec.Routes "no routes"
          } ]
    |> testSequenced
