module FsCDK.Tests.OIDCProviderTests

open Expecto
open FsCDK

[<Tests>]
let oidc_provider_tests =
    testList
        "OIDC Provider DSL"
        [ test "fails when URL is missing" {
              let thrower () =
                  oidcProvider "MyProvider" { () } |> ignore

              Expect.throws thrower "OIDC Provider builder should throw when URL is missing"
          }

          test "accepts GitHub Actions URL" {
              let providerSpec =
                  oidcProvider "GitHubActions" {
                      url OIDCProviders.GitHubActionsUrl
                      clientId OIDCProviders.GitHubActionsClientId
                  }

              Expect.equal providerSpec.Props.Url OIDCProviders.GitHubActionsUrl "Should use GitHub URL"
          }

          test "accepts multiple client IDs" {
              let providerSpec =
                  oidcProvider "MyProvider" {
                      url "https://example.com"
                      clientId "client1"
                      clientId "client2"
                  }

              Expect.equal providerSpec.Props.ClientIds.Length 2 "Should have 2 client IDs"
          }

          test "accepts thumbprints" {
              let providerSpec =
                  oidcProvider "MyProvider" {
                      url "https://example.com"
                      thumbprint "1234567890abcdef"
                  }

              Expect.equal providerSpec.Props.Thumbprints.Length 1 "Should have thumbprint"
          }

          test "defaults constructId to provider name" {
              let providerSpec = oidcProvider "MyProvider" { url "https://example.com" }

              Expect.equal providerSpec.ConstructId "MyProvider" "ConstructId should default to name"
          } ]
