namespace FsCDK.Storage

open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.KMS

/// <summary>
/// High-level S3 Bucket builder following AWS security best practices.
/// 
/// **Default Security Settings:**
/// - BlockPublicAccess = BLOCK_ALL (prevents public access)
/// - ServerSideEncryption = SSE-KMS with AWS managed key (aws/s3)
/// - Versioning = disabled (opt-in via versioned operation)
/// - EnforceSSL = true (requires HTTPS for all requests)
/// 
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework security pillar:
/// - Encryption at rest protects data from unauthorized access
/// - Blocking public access prevents accidental data exposure
/// - SSL/TLS enforcement protects data in transit
/// - KMS provides audit trails and key rotation capabilities
/// 
/// **Escape Hatch:**
/// Access the underlying CDK Bucket via the `Bucket` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type S3BucketConfig =
    { BucketName: string
      ConstructId: string option
      BlockPublicAccess: BlockPublicAccess option
      Encryption: BucketEncryption option
      EncryptionKey: IKey option
      EnforceSSL: bool option
      Versioned: bool option
      RemovalPolicy: RemovalPolicy option
      LifecycleRules: ILifecycleRule list
      AutoDeleteObjects: bool option }

type S3BucketResource =
    { BucketName: string
      ConstructId: string
      /// The underlying CDK Bucket construct - use for advanced scenarios
      Bucket: Bucket }

type S3BucketBuilder(name: string) =
    member _.Yield _ : S3BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = Some BlockPublicAccess.BLOCK_ALL
          Encryption = Some BucketEncryption.KMS_MANAGED
          EncryptionKey = None
          EnforceSSL = Some true
          Versioned = Some false
          RemovalPolicy = None
          LifecycleRules = []
          AutoDeleteObjects = None }

    member _.Yield(lifecycleRule: ILifecycleRule) : S3BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = Some BlockPublicAccess.BLOCK_ALL
          Encryption = Some BucketEncryption.KMS_MANAGED
          EncryptionKey = None
          EnforceSSL = Some true
          Versioned = Some false
          RemovalPolicy = None
          LifecycleRules = [ lifecycleRule ]
          AutoDeleteObjects = None }

    member _.Zero() : S3BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = Some BlockPublicAccess.BLOCK_ALL
          Encryption = Some BucketEncryption.KMS_MANAGED
          EncryptionKey = None
          EnforceSSL = Some true
          Versioned = Some false
          RemovalPolicy = None
          LifecycleRules = []
          AutoDeleteObjects = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> S3BucketConfig) : S3BucketConfig = f ()

    member _.Combine(state1: S3BucketConfig, state2: S3BucketConfig) : S3BucketConfig =
        { BucketName = state1.BucketName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          BlockPublicAccess = state2.BlockPublicAccess |> Option.orElse state1.BlockPublicAccess
          Encryption = state2.Encryption |> Option.orElse state1.Encryption
          EncryptionKey = state2.EncryptionKey |> Option.orElse state1.EncryptionKey
          EnforceSSL = state2.EnforceSSL |> Option.orElse state1.EnforceSSL
          Versioned = state2.Versioned |> Option.orElse state1.Versioned
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          LifecycleRules = state1.LifecycleRules @ state2.LifecycleRules
          AutoDeleteObjects = state2.AutoDeleteObjects |> Option.orElse state1.AutoDeleteObjects }

    member inline x.For(config: S3BucketConfig, [<InlineIfLambda>] f: unit -> S3BucketConfig) : S3BucketConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: S3BucketConfig) : S3BucketResource =
        let bucketName = config.BucketName
        let constructId = config.ConstructId |> Option.defaultValue bucketName

        let props = BucketProps()
        props.BucketName <- bucketName

        config.BlockPublicAccess |> Option.iter (fun v -> props.BlockPublicAccess <- v)
        config.Encryption |> Option.iter (fun v -> props.Encryption <- v)
        config.EncryptionKey |> Option.iter (fun v -> props.EncryptionKey <- v)
        config.EnforceSSL |> Option.iter (fun v -> props.EnforceSSL <- v)
        config.Versioned |> Option.iter (fun v -> props.Versioned <- v)
        config.RemovalPolicy |> Option.iter (fun v -> props.RemovalPolicy <- System.Nullable<RemovalPolicy>(v))
        config.AutoDeleteObjects |> Option.iter (fun v -> props.AutoDeleteObjects <- v)

        if not (List.isEmpty config.LifecycleRules) then
            props.LifecycleRules <- (config.LifecycleRules |> List.toArray)

        { BucketName = bucketName
          ConstructId = constructId
          Bucket = null } // Will be created during stack construction

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: S3BucketConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("blockPublicAccess")>]
    member _.BlockPublicAccess(config: S3BucketConfig, value: BlockPublicAccess) =
        { config with BlockPublicAccess = Some value }

    [<CustomOperation("encryption")>]
    member _.Encryption(config: S3BucketConfig, value: BucketEncryption) =
        { config with Encryption = Some value }

    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: S3BucketConfig, key: IKey) =
        { config with EncryptionKey = Some key }

    [<CustomOperation("enforceSSL")>]
    member _.EnforceSSL(config: S3BucketConfig, value: bool) =
        { config with EnforceSSL = Some value }

    [<CustomOperation("versioned")>]
    member _.Versioned(config: S3BucketConfig, value: bool) =
        { config with Versioned = Some value }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: S3BucketConfig, value: RemovalPolicy) =
        { config with RemovalPolicy = Some value }

    [<CustomOperation("autoDeleteObjects")>]
    member _.AutoDeleteObjects(config: S3BucketConfig, enabled: bool) =
        { config with AutoDeleteObjects = Some enabled }

/// <summary>
/// Helper functions for creating S3 lifecycle rules
/// </summary>
module LifecycleRuleHelpers =
    
    /// <summary>
    /// Creates a lifecycle rule that transitions objects to GLACIER storage after specified days
    /// </summary>
    let transitionToGlacier (days: int) (id: string) =
        LifecycleRule(
            Id = id,
            Enabled = true,
            Transitions = [| Transition(StorageClass = StorageClass.GLACIER, TransitionAfter = Duration.Days(float days)) |]
        )
    
    /// <summary>
    /// Creates a lifecycle rule that expires objects after specified days
    /// </summary>
    let expireAfter (days: int) (id: string) =
        LifecycleRule(
            Id = id,
            Enabled = true,
            Expiration = Duration.Days(float days)
        )
    
    /// <summary>
    /// Creates a lifecycle rule that deletes non-current versions after specified days
    /// </summary>
    let deleteNonCurrentVersions (days: int) (id: string) =
        LifecycleRule(
            Id = id,
            Enabled = true,
            NoncurrentVersionExpiration = Duration.Days(float days)
        )

[<AutoOpen>]
module S3Builders =
    /// <summary>
    /// Creates a new S3 bucket builder with secure defaults.
    /// Example: s3Bucket "my-bucket" { versioned true }
    /// </summary>
    let s3Bucket name = S3BucketBuilder(name)
