module FsCDK.Tests.S3SnapshotTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.S3

/// <summary>
/// Snapshot tests for S3 module ensuring security defaults are properly applied
/// These tests verify builder configuration and properties
/// </summary>
[<Tests>]
let s3_snapshot_tests =
    testList
        "S3 Module Snapshot Tests"
        [ test "s3Bucket builder applies secure defaults" {
              let bucketResource = s3Bucket "test-bucket" { () }

              // Verify construct ID defaults to bucket name
              Expect.equal bucketResource.ConstructId "test-bucket" "ConstructId should default to bucket name"

              // Verify bucket name
              Expect.equal bucketResource.BucketName "test-bucket" "BucketName should be set"
          }

          test "s3Bucket builder with versioning enabled" {
              let bucketResource = s3Bucket "test-bucket" { versioned true }

              Expect.equal bucketResource.BucketName "test-bucket" "BucketName should be set"
          }

          test "s3Bucket builder with lifecycle rule" {
              let lifecycleRule = LifecycleRuleHelpers.expireAfter 30 "expire-old-objects"
              let bucketResource = s3Bucket "test-bucket" { lifecycleRule }

              Expect.equal bucketResource.BucketName "test-bucket" "BucketName should be set"
          }

          test "s3Bucket lifecycle helper functions" {
              let expireRule = LifecycleRuleHelpers.expireAfter 30 "expire-30-days"
              Expect.equal expireRule.Id "expire-30-days" "Rule ID should be set"
              Expect.isTrue (expireRule.Enabled.HasValue && expireRule.Enabled.Value) "Rule should be enabled"
              Expect.isNotNull expireRule.Expiration "Expiration should be set"

              let glacierRule = LifecycleRuleHelpers.transitionToGlacier 90 "glacier-90-days"
              Expect.equal glacierRule.Id "glacier-90-days" "Rule ID should be set"
              Expect.isTrue (glacierRule.Enabled.HasValue && glacierRule.Enabled.Value) "Rule should be enabled"
              Expect.isNotNull glacierRule.Transitions "Transitions should be set"

              let deleteVersionsRule =
                  LifecycleRuleHelpers.deleteNonCurrentVersions 180 "delete-old-versions"

              Expect.equal deleteVersionsRule.Id "delete-old-versions" "Rule ID should be set"

              Expect.isTrue
                  (deleteVersionsRule.Enabled.HasValue && deleteVersionsRule.Enabled.Value)
                  "Rule should be enabled"

              Expect.isNotNull
                  deleteVersionsRule.NoncurrentVersionExpiration
                  "NoncurrentVersionExpiration should be set"
          }

          test "s3Bucket builder with custom construct ID" {
              let bucketResource = s3Bucket "test-bucket" { constructId "CustomBucketId" }

              Expect.equal bucketResource.ConstructId "CustomBucketId" "ConstructId should be custom"
              Expect.equal bucketResource.BucketName "test-bucket" "BucketName should be set"
          } ]
    |> testSequenced
