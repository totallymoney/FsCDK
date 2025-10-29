namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.KMS
open Amazon.CDK.AWS.IAM

// ============================================================================
// Key Management Service (KMS) Key Configuration DSL
// ============================================================================

/// <summary>
/// High-level KMS Key builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - Key rotation = enabled (automatic yearly rotation)
/// - Removal policy = RETAIN (prevents accidental key deletion)
/// - Key spec = SYMMETRIC_DEFAULT (AES-256-GCM)
/// - Key usage = ENCRYPT_DECRYPT
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Automatic rotation reduces risk of key compromise
/// - Retained keys prevent data loss (encrypted data becomes unreadable without key)
/// - Symmetric encryption is the most common use case
/// - CloudTrail provides audit trails for all key usage
///
/// **Use Cases:**
/// - Encrypt data at rest (S3, EBS, RDS, DynamoDB)
/// - Encrypt secrets (Secrets Manager, Parameter Store)
/// - Sign and verify (asymmetric keys)
/// - Generate HMACs
///
/// **Escape Hatch:**
/// Access the underlying CDK Key via the `Key` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type KMSKeyConfig =
    { KeyName: string
      ConstructId: string option
      Description: string option
      Alias: string option
      EnableKeyRotation: bool option
      RemovalPolicy: RemovalPolicy option
      Enabled: bool option
      KeySpec: KeySpec option
      KeyUsage: KeyUsage option
      PendingWindow: Duration option
      AdmissionPrincipal: IPrincipal option
      Policy: PolicyDocument option }

type KMSKeySpec =
    { KeyName: string
      ConstructId: string
      Props: KeyProps
      mutable Key: IKey }


type KMSKeyBuilder(name: string) =
    member _.Yield _ : KMSKeyConfig =
        { KeyName = name
          ConstructId = None
          Description = None
          Alias = None
          EnableKeyRotation = Some true
          RemovalPolicy = Some RemovalPolicy.RETAIN
          Enabled = Some true
          KeySpec = Some KeySpec.SYMMETRIC_DEFAULT
          KeyUsage = Some KeyUsage.ENCRYPT_DECRYPT
          PendingWindow = None
          AdmissionPrincipal = None
          Policy = None }

    member _.Zero() : KMSKeyConfig =
        { KeyName = name
          ConstructId = None
          Description = None
          Alias = None
          EnableKeyRotation = Some true
          RemovalPolicy = Some RemovalPolicy.RETAIN
          Enabled = Some true
          KeySpec = Some KeySpec.SYMMETRIC_DEFAULT
          KeyUsage = Some KeyUsage.ENCRYPT_DECRYPT
          PendingWindow = None
          AdmissionPrincipal = None
          Policy = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> KMSKeyConfig) : KMSKeyConfig = f ()

    member _.Combine(state1: KMSKeyConfig, state2: KMSKeyConfig) : KMSKeyConfig =
        { KeyName = state1.KeyName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          Alias = state2.Alias |> Option.orElse state1.Alias
          EnableKeyRotation = state2.EnableKeyRotation |> Option.orElse state1.EnableKeyRotation
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          Enabled = state2.Enabled |> Option.orElse state1.Enabled
          KeySpec = state2.KeySpec |> Option.orElse state1.KeySpec
          KeyUsage = state2.KeyUsage |> Option.orElse state1.KeyUsage
          PendingWindow = state2.PendingWindow |> Option.orElse state1.PendingWindow
          AdmissionPrincipal = state2.AdmissionPrincipal |> Option.orElse state1.AdmissionPrincipal
          Policy = state2.Policy |> Option.orElse state1.Policy }

    member inline x.For(config: KMSKeyConfig, [<InlineIfLambda>] f: unit -> KMSKeyConfig) : KMSKeyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: KMSKeyConfig) : KMSKeySpec =
        let constructId = config.ConstructId |> Option.defaultValue config.KeyName

        let props = KeyProps()

        config.Description |> Option.iter (fun d -> props.Description <- d)
        config.Alias |> Option.iter (fun a -> props.Alias <- a)
        config.EnableKeyRotation |> Option.iter (fun r -> props.EnableKeyRotation <- r)
        config.RemovalPolicy |> Option.iter (fun p -> props.RemovalPolicy <- p)
        config.Enabled |> Option.iter (fun e -> props.Enabled <- e)
        config.KeySpec |> Option.iter (fun s -> props.KeySpec <- s)
        config.KeyUsage |> Option.iter (fun u -> props.KeyUsage <- u)
        config.PendingWindow |> Option.iter (fun w -> props.PendingWindow <- w)
        //config.AdmissionPrincipal |> Option.iter (fun p -> props.AdmissionPrincipal <- p)
        config.Policy |> Option.iter (fun p -> props.Policy <- p)

        { KeyName = config.KeyName
          ConstructId = constructId
          Props = props
          Key = null }

    /// <summary>Sets the construct ID for the KMS key.</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     constructId "MyKMSKey"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: KMSKeyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the description for the KMS key.</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="description">Human-readable description.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     description "Encryption key for S3 bucket data"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: KMSKeyConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets an alias for the KMS key (e.g., "alias/my-app-key").</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="alias">The key alias.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     alias "alias/my-app-key"
    /// }
    /// </code>
    [<CustomOperation("alias")>]
    member _.Alias(config: KMSKeyConfig, alias: string) = { config with Alias = Some alias }

    /// <summary>Enables automatic key rotation (recommended for security).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="value">True to enable key rotation, false to disable.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     enableKeyRotation true
    /// }
    /// </code>
    [<CustomOperation("enableKeyRotation")>]
    member _.EnableKeyRotation(config: KMSKeyConfig, value: bool) =
        { config with
            EnableKeyRotation = Some value }

    /// <summary>Sets the removal policy (RETAIN, DESTROY, SNAPSHOT).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="policy">The removal policy.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     removalPolicy RemovalPolicy.DESTROY
    /// }
    /// </code>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: KMSKeyConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    /// <summary>Sets whether the key is enabled (default: true).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="enabled">Enable or disable the key.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     enabled false
    /// }
    /// </code>
    [<CustomOperation("enabled")>]
    member _.Enabled(config: KMSKeyConfig, enabled: bool) = { config with Enabled = Some enabled }

    /// <summary>Sets the key spec (SYMMETRIC_DEFAULT, RSA_2048, etc.).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="spec">The key specification.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     keySpec KeySpec.RSA_2048
    /// }
    /// </code>
    [<CustomOperation("keySpec")>]
    member _.KeySpec(config: KMSKeyConfig, spec: KeySpec) = { config with KeySpec = Some spec }

    /// <summary>Sets the key usage (ENCRYPT_DECRYPT, SIGN_VERIFY, GENERATE_VERIFY_MAC).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="usage">The key usage.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     keyUsage KeyUsage.SIGN_VERIFY
    /// }
    /// </code>
    [<CustomOperation("keyUsage")>]
    member _.KeyUsage(config: KMSKeyConfig, usage: KeyUsage) = { config with KeyUsage = Some usage }

    /// <summary>Sets the pending window for key deletion (7-30 days).</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="window">The pending window duration.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     pendingWindow (Duration.Days(30.0))
    /// }
    /// </code>
    [<CustomOperation("pendingWindow")>]
    member _.PendingWindow(config: KMSKeyConfig, window: Duration) =
        { config with
            PendingWindow = Some window }

    /// <summary>Sets the principal that can administer the key.</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="principal">The IAM principal.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     admissionPrincipal (AccountRootPrincipal())
    /// }
    /// </code>
    [<CustomOperation("admissionPrincipal")>]
    member _.AdmissionPrincipal(config: KMSKeyConfig, principal: IPrincipal) =
        { config with
            AdmissionPrincipal = Some principal }

    /// <summary>Sets a custom key policy.</summary>
    /// <param name="config">The current KMS key configuration.</param>
    /// <param name="policy">The key policy document.</param>
    /// <code lang="fsharp">
    /// kmsKey "my-key" {
    ///     policy myCustomPolicy
    /// }
    /// </code>
    [<CustomOperation("policy")>]
    member _.Policy(config: KMSKeyConfig, policy: PolicyDocument) = { config with Policy = Some policy }

// ============================================================================
// KMS Helper Functions
// ============================================================================

/// <summary>
/// Helper functions for common KMS key patterns
/// </summary>
module KMSHelpers =

    /// <summary>
    /// Creates a KMS key for S3 bucket encryption
    /// </summary>
    let createS3EncryptionKey (keyName: string) (description: string) =
        let props = KeyProps()
        props.Description <- description
        props.EnableKeyRotation <- true
        props.RemovalPolicy <- RemovalPolicy.RETAIN
        props.Alias <- sprintf "alias/%s-s3" keyName
        props

    /// <summary>
    /// Creates a KMS key for Secrets Manager encryption
    /// </summary>
    let createSecretsManagerKey (keyName: string) (description: string) =
        let props = KeyProps()
        props.Description <- description
        props.EnableKeyRotation <- true
        props.RemovalPolicy <- RemovalPolicy.RETAIN
        props.Alias <- sprintf "alias/%s-secrets" keyName
        props

    /// <summary>
    /// Creates a KMS key for EBS volume encryption
    /// </summary>
    let createEBSEncryptionKey (keyName: string) (description: string) =
        let props = KeyProps()
        props.Description <- description
        props.EnableKeyRotation <- true
        props.RemovalPolicy <- RemovalPolicy.RETAIN
        props.Alias <- sprintf "alias/%s-ebs" keyName
        props

    /// <summary>
    /// Creates a KMS key for Lambda environment variable encryption
    /// </summary>
    let createLambdaEncryptionKey (keyName: string) (description: string) =
        let props = KeyProps()
        props.Description <- description
        props.EnableKeyRotation <- true
        props.RemovalPolicy <- RemovalPolicy.RETAIN
        props.Alias <- sprintf "alias/%s-lambda" keyName
        props

    /// <summary>
    /// Creates an asymmetric KMS key for signing
    /// </summary>
    let createSigningKey (keyName: string) (description: string) =
        let props = KeyProps()
        props.Description <- description
        props.EnableKeyRotation <- false // Asymmetric keys don't support rotation
        props.KeySpec <- KeySpec.RSA_2048
        props.KeyUsage <- KeyUsage.SIGN_VERIFY
        props.Alias <- sprintf "alias/%s-signing" keyName
        props

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module KMSBuilders =
    /// <summary>
    /// Creates a new KMS key builder with secure defaults.
    /// Example: kmsKey "my-encryption-key" { description "Encrypts sensitive data" }
    /// </summary>
    let kmsKey name = KMSKeyBuilder(name)
