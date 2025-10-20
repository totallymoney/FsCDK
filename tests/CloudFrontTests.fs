module FsCDK.Tests.CloudFrontTests

open System
open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.CloudFront.Origins
open Amazon.CDK.AWS.S3

[<Tests>]
let tests =
    // Some of these test doesn't run well in parallel
    testSequenced
    <| testList
        "CloudFront"
        [

          testCase "Distribution requires default behavior"
          <| fun _ ->
              let mutable ex: exn option = None

              try
                  let _ = cloudFrontDistribution "MissingDefault" { () }
                  ()
              with e ->
                  ex <- Some e

              Expect.isSome ex "Expected an ArgumentException for missing default behavior"

              match ex with
              | Some(:? ArgumentException as ae) ->
                  Expect.equal ae.ParamName "defaultBehavior" "ParamName should be 'defaultBehavior'"
              | Some e -> failtestf "Unexpected exception type: %A" e
              | None -> ()

          testCase "domainName preserves call order"
          <| fun _ ->
              let spec =
                  cloudFrontDistribution "WithDomains" {
                      httpDefaultBehavior "origin.example.com"
                      domainName "a.example.com"
                      domainName "b.example.com"
                  }

              Expect.equal
                  spec.Props.DomainNames
                  [| "a.example.com"; "b.example.com" |]
                  "Domain names should preserve call order"

          testCase "Distribution defaults are AWS-aligned"
          <| fun _ ->
              let spec =
                  cloudFrontDistribution "Defaults" { httpDefaultBehavior "origin.example.com" }

              Expect.isTrue spec.Props.Enabled.HasValue "Enabled should default to true"
              Expect.isTrue spec.Props.Enabled.Value "Enabled should default to true"

              Expect.equal
                  spec.Props.HttpVersion.Value
                  HttpVersion.HTTP2_AND_3
                  "HTTP version should default to HTTP2_AND_3"

              Expect.equal
                  spec.Props.MinimumProtocolVersion.Value
                  SecurityPolicyProtocol.TLS_V1_2_2021
                  "Min TLS should default to TLS 1.2 2021"

              Expect.equal spec.Props.PriceClass.Value PriceClass.PRICE_CLASS_100 "PriceClass should default to 100"
              Expect.isTrue spec.Props.EnableIpv6.Value "IPv6 should default to true"

          testCase "additionalHttpBehavior adds to AdditionalBehaviors map"
          <| fun _ ->
              let spec =
                  cloudFrontDistribution "Additional" {
                      httpDefaultBehavior "origin.example.com"
                      additionalHttpBehavior "/api/*" "api.example.com"
                  }

              Expect.isNotNull
                  spec.Props.AdditionalBehaviors
                  "AdditionalBehaviors should be non-null when at least one is added"

              let found = spec.Props.AdditionalBehaviors.ContainsKey("/api/*")
              Expect.isTrue found "Expected to find path pattern '/api/*' in AdditionalBehaviors"

          testCase "s3DefaultBehavior wires origin and sets sensible defaults"
          <| fun _ ->
              // Minimal CDK scope for S3 origin creation
              let app = new App()
              let stack = new Stack(app, "TestStack")
              let bucket = S3OriginType.StaticWebsiteOrigin(new Bucket(stack, "Bucket"))

              let spec = cloudFrontDistribution "S3Default" { s3DefaultBehavior bucket }

              let behavior = spec.Props.DefaultBehavior :?> BehaviorOptions
              // origin type
              Expect.isTrue
                  (behavior.Origin :? S3StaticWebsiteOrigin)
                  "Default origin should be of type created in this test"
              // viewer protocol policy and compression
              Expect.equal
                  behavior.ViewerProtocolPolicy.Value
                  ViewerProtocolPolicy.REDIRECT_TO_HTTPS
                  "Viewer protocol should redirect to HTTPS"

              Expect.isTrue behavior.Compress.HasValue "Compression should default to true"
              Expect.isTrue behavior.Compress.Value "Compression should default to true"
              // policies should be set (avoid brittle reference checks)
              Expect.isNotNull behavior.CachePolicy "CachePolicy should be set"
              Expect.isNotNull behavior.OriginRequestPolicy "OriginRequestPolicy should be set" ]
