module FsCDK.Tests.CertificateManagerTests

open Expecto
open FsCDK

[<Tests>]
let certificate_manager_tests =
    testList
        "Certificate Manager DSL"
        [ test "requires domain name" {
              let certSpec = certificate "MyCert" { domainName "example.com" }

              Expect.equal certSpec.Props.DomainName "example.com" "Should use domain name"
          }

          test "accepts subject alternative names" {
              let certSpec =
                  certificate "MyCert" {
                      domainName "example.com"
                      subjectAlternativeName "*.example.com"
                      subjectAlternativeName "www.example.com"
                  }

              Expect.equal certSpec.Props.SubjectAlternativeNames.Length 2 "Should have 2 SANs"
          }

          test "has validation method set" {
              let certSpec = certificate "MyCert" { domainName "example.com" }

              Expect.isNotNull certSpec.Props.Validation "Should have validation method"
          }

          test "allows custom certificate name" {
              let certSpec =
                  certificate "MyCert" {
                      domainName "example.com"
                      certificateName "MyCustomCert"
                  }

              Expect.equal certSpec.Props.CertificateName "MyCustomCert" "Should use custom name"
          }

          test "defaults constructId to certificate name" {
              let certSpec = certificate "MyCert" { domainName "example.com" }

              Expect.equal certSpec.ConstructId "MyCert" "ConstructId should default to name"
          }

          test "allows custom constructId" {
              let certSpec =
                  certificate "MyCert" {
                      constructId "CustomCert"
                      domainName "example.com"
                  }

              Expect.equal certSpec.ConstructId "CustomCert" "Should use custom constructId"
          }

          test "multiple SANs are in correct order" {
              let certSpec =
                  certificate "MyCert" {
                      domainName "example.com"
                      subjectAlternativeName "*.example.com"
                      subjectAlternativeName "api.example.com"
                      subjectAlternativeName "www.example.com"
                  }

              Expect.equal certSpec.Props.SubjectAlternativeNames.Length 3 "Should have 3 SANs"
              // SANs are added in reverse order due to list prepending
              Expect.equal certSpec.Props.SubjectAlternativeNames.[0] "www.example.com" "First SAN should be last added"
              Expect.equal certSpec.Props.SubjectAlternativeNames.[2] "*.example.com" "Last SAN should be first added"
          } ]
    |> testSequenced
