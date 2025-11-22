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
/// - Principle of least privilege requires explicit permissions
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
      Statements: PolicyStatement list
      RemovalPolicy: Amazon.CDK.RemovalPolicy option }

type BucketPolicySpec =
    { PolicyName: string
      ConstructId: string
      Props: BucketPolicyProps
      mutable Policy: BucketPolicy option }

type BucketPolicyBuilder(name: string) =
    member _.Yield(_: unit) : BucketPolicyConfig =
        { PolicyName = name
          ConstructId = None
          Bucket = None
          Statements = []
          RemovalPolicy = None }

    member _.Yield(statement: PolicyStatement) : BucketPolicyConfig =
        { PolicyName = name
          ConstructId = None
          Bucket = None
          Statements = [ statement ]
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
          Statements = a.Statements @ b.Statements
          RemovalPolicy =
            match a.RemovalPolicy with
            | Some _ -> a.RemovalPolicy
            | None -> b.RemovalPolicy }

    member _.Run(config: BucketPolicyConfig) : BucketPolicySpec =
        let props = BucketPolicyProps()
        let constructId = config.ConstructId |> Option.defaultValue config.PolicyName

        config.Bucket |> Option.iter (fun bucket -> props.Bucket <- bucket)

        config.RemovalPolicy |> Option.iter (fun rp -> props.RemovalPolicy <- rp)

        { PolicyName = config.PolicyName
          ConstructId = constructId
          Props = props
          Policy = None }

    /// <summary>Sets the construct ID for the bucket policy.</summary>
    /// <param name="config">The configuration.</param>
    /// <param name="id">The construct ID.</param>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: BucketPolicyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the bucket for the policy.</summary>
    /// <param name="config">The configuration.</param>
    /// <param name="bucket">The bucket.</param>
    [<CustomOperation("bucket")>]
    member _.Bucket(config: BucketPolicyConfig, bucket: IBucket) = { config with Bucket = Some(bucket) }

    /// <summary>Adds a policy statement.</summary>
    [<CustomOperation("statement")>]
    member _.Statement(config: BucketPolicyConfig, statement: PolicyStatement) =
        { config with
            Statements = statement :: config.Statements }

    /// <summary>Adds a statement that denies non-HTTPS requests (security best practice).</summary>
    [<CustomOperation("denyInsecureTransport")>]
    member _.DenyInsecureTransport(config: BucketPolicyConfig) =
        let conditions = System.Collections.Generic.Dictionary<string, obj>()
        conditions.Add("aws:SecureTransport", box "false")

        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Sid = "DenyInsecureTransport",
                    Effect = System.Nullable Effect.DENY,
                    Principals = [| AnyPrincipal() :> IPrincipal |],
                    Actions = [| "s3:*" |],
                    Resources =
                        [| match config.Bucket with
                           | Some b -> b.BucketArn + "/*"
                           | None -> "*" |],
                    Conditions = dict<string, obj> [ "Bool", conditions ]
                )
            )

        { config with
            Statements = statement :: config.Statements }

    /// <summary>Adds a statement that allows CloudFront OAI access.</summary>
    [<CustomOperation("allowCloudFrontOAI")>]
    member _.AllowCloudFrontOAI(config: BucketPolicyConfig, oaiCanonicalUserId: string) =
        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Sid = "AllowCloudFrontOAI",
                    Effect = System.Nullable Effect.ALLOW,
                    Principals = [| CanonicalUserPrincipal(oaiCanonicalUserId) :> IPrincipal |],
                    Actions = [| "s3:GetObject" |],
                    Resources =
                        [| match config.Bucket with
                           | Some(b) -> b.BucketArn + "/*"
                           | None -> "*" |]
                )
            )

        { config with
            Statements = statement :: config.Statements }

    /// <summary>Adds a statement that restricts access to specific IP addresses.</summary>
    [<CustomOperation("allowFromIpAddresses")>]
    member _.AllowFromIpAddresses(config: BucketPolicyConfig, ipAddresses: string list) =
        let conditions = System.Collections.Generic.Dictionary<string, obj>()
        conditions.Add("aws:SourceIp", box (ipAddresses |> List.toArray))

        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Sid = "AllowFromSpecificIPs",
                    Effect = System.Nullable Effect.ALLOW,
                    Principals = [| AnyPrincipal() :> IPrincipal |],
                    Actions = [| "s3:GetObject" |],
                    Resources =
                        [| match config.Bucket with
                           | Some(b) -> b.BucketArn + "/*"
                           | None -> "*" |],
                    Conditions = dict [ "IpAddress", conditions ]
                )
            )

        { config with
            Statements = statement :: config.Statements }

    /// <summary>Adds a statement that denies access from specific IP addresses.</summary>
    [<CustomOperation("denyFromIpAddresses")>]
    member _.DenyFromIpAddresses(config: BucketPolicyConfig, ipAddresses: string list) =
        let conditions = System.Collections.Generic.Dictionary<string, obj>()
        conditions.Add("aws:SourceIp", box (ipAddresses |> List.toArray))

        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Sid = "DenyFromSpecificIPs",
                    Effect = System.Nullable Effect.DENY,
                    Principals = [| AnyPrincipal() :> IPrincipal |],
                    Actions = [| "s3:*" |],
                    Resources =
                        [| match config.Bucket with
                           | Some b -> b.BucketArn + "/*"
                           | None -> "*" |],
                    Conditions = dict [ "IpAddress", conditions ]
                )
            )

        { config with
            Statements = statement :: config.Statements }

    /// <summary>Sets the removal policy for the bucket policy.</summary>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: BucketPolicyConfig, policy: Amazon.CDK.RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

// ============================================================================
// Helper module for common policy statements
// ============================================================================

module BucketPolicyStatements =
    /// Creates a statement to enforce HTTPS-only access
    let denyInsecureTransport (bucket: IBucket) =
        let conditions = System.Collections.Generic.Dictionary<string, obj>()
        conditions.Add("aws:SecureTransport", box "false")

        PolicyStatement(
            PolicyStatementProps(
                Sid = "DenyInsecureTransport",
                Effect = System.Nullable Effect.DENY,
                Principals = [| AnyPrincipal() :> IPrincipal |],
                Actions = [| "s3:*" |],
                Resources = [| bucket.BucketArn + "/*" |],
                Conditions = dict [ "Bool", conditions ]
            )
        )

    /// Creates a statement to allow public read access
    let allowPublicRead (bucket: IBucket) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "PublicReadGetObject",
                Effect = System.Nullable Effect.ALLOW,
                Principals = [| AnyPrincipal() :> IPrincipal |],
                Actions = [| "s3:GetObject" |],
                Resources = [| bucket.BucketArn + "/*" |]
            )
        )

    /// Creates a statement to allow specific principal full access
    let allowPrincipalFullAccess (bucket: IBucket) (principal: IPrincipal) =
        PolicyStatement(
            PolicyStatementProps(
                Effect = System.Nullable Effect.ALLOW,
                Principals = [| principal |],
                Actions = [| "s3:*" |],
                Resources = [| bucket.BucketArn; bucket.BucketArn + "/*" |]
            )
        )

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
    ///     denyInsecureTransport
    ///     allowFromIpAddresses ["203.0.113.0/24"; "198.51.100.0/24"]
    /// }
    /// </code>
    let bucketPolicy (name: string) = BucketPolicyBuilder(name)
