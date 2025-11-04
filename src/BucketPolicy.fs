namespace FsCDK

open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.IAM

// ============================================================================
// S3 Bucket Policy Configuration DSL
// ============================================================================

/// <summary>
/// High-level S3 Bucket Policy builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - No default statements (explicit policy definition required)
/// - Applies to specific bucket only
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework security pillar:
/// - Principle of the least privilege requires explicit permissions
/// - No default deny-all to allow incremental policy building
/// - Bucket-specific policies prevent accidental broad access
///
/// **Best Practices:**
/// - Deny HTTP requests (enforce HTTPS)
/// - Restrict access by IP address when possible
/// - Use condition keys to limit access
/// - Apply MFA delete for critical buckets
///
/// **Escape Hatch:**
/// Access the underlying CDK BucketPolicy via the `Policy` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type BucketPolicyConfig =
    { PolicyName: string
      ConstructId: string option
      Bucket: IBucket option
      Statements: PolicyStatement seq
      RemovalPolicy: Amazon.CDK.RemovalPolicy option }

type BucketPolicySpec =
    { PolicyName: string
      ConstructId: string
      Props: BucketPolicyProps
      mutable Policy: BucketPolicy }

type BucketPolicyBuilder(name: string) =
    member _.Yield(_: unit) : BucketPolicyConfig =
        { PolicyName = name
          ConstructId = None
          Bucket = None
          Statements = []
          RemovalPolicy = None }

    member _.Zero() : BucketPolicyConfig =
        { PolicyName = name
          ConstructId = None
          Bucket = None
          Statements = []
          RemovalPolicy = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> BucketPolicyConfig) : BucketPolicyConfig = f ()

    member inline x.For
        (
            config: BucketPolicyConfig,
            [<InlineIfLambda>] f: unit -> BucketPolicyConfig
        ) : BucketPolicyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: BucketPolicyConfig, b: BucketPolicyConfig) : BucketPolicyConfig =
        { PolicyName = a.PolicyName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Bucket =
            match a.Bucket with
            | Some _ -> a.Bucket
            | None -> b.Bucket
          Statements = Seq.toList a.Statements @ Seq.toList b.Statements
          RemovalPolicy =
            match a.RemovalPolicy with
            | Some _ -> a.RemovalPolicy
            | None -> b.RemovalPolicy }

    member _.Run(config: BucketPolicyConfig) : BucketPolicySpec =
        let props = BucketPolicyProps()
        let constructId = config.ConstructId |> Option.defaultValue config.PolicyName

        props.Bucket <-
            match config.Bucket with
            | Some bucket -> bucket
            | None -> invalidArg "bucket" "Bucket is required for Bucket Policy"

        config.RemovalPolicy |> Option.iter (fun rp -> props.RemovalPolicy <- rp)

        { PolicyName = config.PolicyName
          ConstructId = constructId
          Props = props
          Policy = null }

    /// <summary>Sets the construct ID for the bucket policy.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: BucketPolicyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the bucket for the policy.</summary>
    [<CustomOperation("bucket")>]
    member _.Bucket(config: BucketPolicyConfig, bucket: IBucket) = { config with Bucket = Some(bucket) }

    /// <summary>Adds a policy statement.</summary>
    [<CustomOperation("statements")>]
    member _.Statements(config: BucketPolicyConfig, statements: PolicyStatement seq) =
        { config with Statements = statements }

    /// <summary>Sets the removal policy for the bucket policy.</summary>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: BucketPolicyConfig, policy: Amazon.CDK.RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module BucketPolicyBuilders =
    /// <summary>Creates an S3 Bucket Policy with AWS security best practices.</summary>
    /// <param name="name">The policy name.</param>
    /// <code lang="fsharp">
    /// bucketPolicy "MyBucketPolicy" {
    ///     bucket myBucket
    /// }
    /// </code>
    let bucketPolicy (name: string) = BucketPolicyBuilder(name)
