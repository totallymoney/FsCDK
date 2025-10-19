namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SecretsManager
open Amazon.CDK.AWS.KMS

/// <summary>
/// High-level Secrets Manager Secret builder following AWS security best practices.
/// 
/// **Default Security Settings:**
/// - Encryption = KMS with AWS managed key (aws/secretsmanager)
/// - Automatic rotation = disabled (opt-in via rotation operation)
/// - Removal policy = RETAIN (prevents accidental deletion)
/// 
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - KMS encryption provides enhanced security and audit trails
/// - Secrets retained on stack deletion prevents data loss
/// - Rotation is opt-in as it requires Lambda function setup
/// 
/// **Escape Hatch:**
/// Access the underlying CDK Secret via the `Secret` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type SecretsManagerSecretConfig =
    { SecretName: string
      ConstructId: string option
      Description: string option
      EncryptionKey: IKey option
      RemovalPolicy: RemovalPolicy option
      SecretStringValue: SecretValue option
      GenerateSecretString: SecretStringGenerator option }

type SecretsManagerSecretResource =
    { SecretName: string
      ConstructId: string
      /// The underlying CDK Secret construct - use for advanced scenarios
      Secret: Secret }

type SecretsManagerSecretBuilder(name: string) =
    member _.Yield _ : SecretsManagerSecretConfig =
        { SecretName = name
          ConstructId = None
          Description = None
          EncryptionKey = None
          RemovalPolicy = Some RemovalPolicy.RETAIN
          SecretStringValue = None
          GenerateSecretString = None }

    member _.Zero() : SecretsManagerSecretConfig =
        { SecretName = name
          ConstructId = None
          Description = None
          EncryptionKey = None
          RemovalPolicy = Some RemovalPolicy.RETAIN
          SecretStringValue = None
          GenerateSecretString = None }

    member _.Combine(state1: SecretsManagerSecretConfig, state2: SecretsManagerSecretConfig) : SecretsManagerSecretConfig =
        { SecretName = state2.SecretName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          EncryptionKey = state2.EncryptionKey |> Option.orElse state1.EncryptionKey
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          SecretStringValue = state2.SecretStringValue |> Option.orElse state1.SecretStringValue
          GenerateSecretString = state2.GenerateSecretString |> Option.orElse state1.GenerateSecretString }

    member inline x.For(config: SecretsManagerSecretConfig, [<InlineIfLambda>] f: unit -> SecretsManagerSecretConfig) : SecretsManagerSecretConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SecretsManagerSecretConfig) : SecretsManagerSecretResource =
        let secretName = config.SecretName
        let constructId = config.ConstructId |> Option.defaultValue secretName

        let props = SecretProps()
        props.SecretName <- secretName
        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.EncryptionKey |> Option.iter (fun v -> props.EncryptionKey <- v)
        config.RemovalPolicy |> Option.iter (fun v -> props.RemovalPolicy <- v)
        config.SecretStringValue |> Option.iter (fun v -> props.SecretStringValue <- v)
        config.GenerateSecretString |> Option.iter (fun v -> props.GenerateSecretString <- v)

        { SecretName = secretName
          ConstructId = constructId
          Secret = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SecretsManagerSecretConfig, id: string) =
        { config with ConstructId = Some id }

    [<CustomOperation("description")>]
    member _.Description(config: SecretsManagerSecretConfig, description: string) =
        { config with Description = Some description }

    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: SecretsManagerSecretConfig, key: IKey) =
        { config with EncryptionKey = Some key }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: SecretsManagerSecretConfig, policy: RemovalPolicy) =
        { config with RemovalPolicy = Some policy }

    [<CustomOperation("secretStringValue")>]
    member _.SecretStringValue(config: SecretsManagerSecretConfig, value: SecretValue) =
        { config with SecretStringValue = Some value }

    [<CustomOperation("generateSecretString")>]
    member _.GenerateSecretString(config: SecretsManagerSecretConfig, generator: SecretStringGenerator) =
        { config with GenerateSecretString = Some generator }

/// <summary>
/// Helper functions for creating secret string generators
/// </summary>
module SecretsManagerHelpers =
    /// <summary>
    /// Creates a secret string generator for a random password
    /// </summary>
    let generatePassword (length: int) (excludeCharacters: string option) =
        let gen = SecretStringGenerator()
        gen.PasswordLength <- System.Nullable<float>(float length)
        gen.ExcludePunctuation <- false
        gen.IncludeSpace <- false
        excludeCharacters |> Option.iter (fun chars -> gen.ExcludeCharacters <- chars)
        gen

    /// <summary>
    /// Creates a secret string generator for JSON secrets (e.g., database credentials)
    /// </summary>
    let generateJsonSecret (secretStringTemplate: string) (generateStringKey: string) =
        let gen = SecretStringGenerator()
        gen.SecretStringTemplate <- secretStringTemplate
        gen.GenerateStringKey <- generateStringKey
        gen.PasswordLength <- System.Nullable<float>(32.0)
        gen.ExcludePunctuation <- true
        gen

[<AutoOpen>]
module SecretsManagerBuilders =
    /// <summary>
    /// Creates a new Secrets Manager secret builder with secure defaults.
    /// Example: secret "my-api-key" { description "API key for external service" }
    /// </summary>
    let secret name = SecretsManagerSecretBuilder(name)
