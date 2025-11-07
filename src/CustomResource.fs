namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open Amazon.CDK.CustomResources

/// <summary>
/// High-level Custom Resource builder for one-time deployment tasks.
///
/// **Default Settings:**
/// - Timeout = 5 minutes
/// - Remove on update = true (cleanup on changes)
///
/// **Rationale:**
/// Custom Resources provide deployment-time execution similar to Azure Deployment Scripts.
/// These defaults follow AWS Well-Architected Framework:
/// - Reasonable timeout for typical init tasks
/// - Automatic cleanup prevents resource leaks
///
/// **Use Cases:**
/// - Database migrations and seeding
/// - DNS record creation
/// - Third-party API calls
/// - Certificate validation
/// - Resource initialization
///
/// **Escape Hatch:**
/// Access the underlying CDK AwsCustomResource via the `CustomResource` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type CustomResourceConfig =
    { ResourceName: string
      ConstructId: string option
      OnCreate: AwsSdkCall option
      OnUpdate: AwsSdkCall option
      OnDelete: AwsSdkCall option
      Policy: AwsCustomResourcePolicy option
      Timeout: Duration option
      InstallLatestAwsSdk: bool option
      LogRetention: RetentionDays option }

type CustomResourceResource =
    {
        ResourceName: string
        ConstructId: string
        Props: AwsCustomResourceProps
        /// The underlying CDK AwsCustomResource construct
        mutable CustomResource: AwsCustomResource option
    }

    /// Gets the response from the custom resource
    member this.GetResponseField(fieldName: string) =
        match this.CustomResource with
        | Some cr -> cr.GetResponseField(fieldName)
        | None ->
            failwith
                $"Custom Resource '{this.ResourceName}' has not been created yet. Ensure it's yielded in the stack before accessing it."

type CustomResourceBuilder(name: string) =
    member _.Yield _ : CustomResourceConfig =
        { ResourceName = name
          ConstructId = None
          OnCreate = None
          OnUpdate = None
          OnDelete = None
          Policy = None
          Timeout = Some(Duration.Minutes(5.0))
          InstallLatestAwsSdk = Some true
          LogRetention = Some RetentionDays.ONE_WEEK }

    member _.Zero() : CustomResourceConfig =
        { ResourceName = name
          ConstructId = None
          OnCreate = None
          OnUpdate = None
          OnDelete = None
          Policy = None
          Timeout = Some(Duration.Minutes(5.0))
          InstallLatestAwsSdk = Some true
          LogRetention = Some RetentionDays.ONE_WEEK }

    member _.Combine(state1: CustomResourceConfig, state2: CustomResourceConfig) : CustomResourceConfig =
        { ResourceName = state2.ResourceName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          OnCreate = state2.OnCreate |> Option.orElse state1.OnCreate
          OnUpdate = state2.OnUpdate |> Option.orElse state1.OnUpdate
          OnDelete = state2.OnDelete |> Option.orElse state1.OnDelete
          Policy = state2.Policy |> Option.orElse state1.Policy
          Timeout = state2.Timeout |> Option.orElse state1.Timeout
          InstallLatestAwsSdk = state2.InstallLatestAwsSdk |> Option.orElse state1.InstallLatestAwsSdk
          LogRetention = state2.LogRetention |> Option.orElse state1.LogRetention }

    member inline x.For
        (
            config: CustomResourceConfig,
            [<InlineIfLambda>] f: unit -> CustomResourceConfig
        ) : CustomResourceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CustomResourceConfig) : CustomResourceResource =
        let resourceName = config.ResourceName
        let constructId = config.ConstructId |> Option.defaultValue resourceName

        let props = AwsCustomResourceProps()

        match config.OnCreate with
        | Some create -> props.OnCreate <- create
        | None -> failwith "OnCreate is required for Custom Resource"

        config.OnUpdate |> Option.iter (fun v -> props.OnUpdate <- v)
        config.OnDelete |> Option.iter (fun v -> props.OnDelete <- v)
        config.Timeout |> Option.iter (fun v -> props.Timeout <- v)

        config.InstallLatestAwsSdk
        |> Option.iter (fun v -> props.InstallLatestAwsSdk <- System.Nullable<bool>(v))

        config.LogRetention
        |> Option.iter (fun v -> props.LogRetention <- System.Nullable<RetentionDays>(v))

        match config.Policy with
        | Some policy -> props.Policy <- policy
        | None -> props.Policy <- AwsCustomResourcePolicy.FromSdkCalls(SdkCallsPolicyOptions())

        { ResourceName = resourceName
          ConstructId = constructId
          Props = props
          CustomResource = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CustomResourceConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("onCreate")>]
    member _.OnCreate(config: CustomResourceConfig, call: AwsSdkCall) = { config with OnCreate = Some call }

    [<CustomOperation("onUpdate")>]
    member _.OnUpdate(config: CustomResourceConfig, call: AwsSdkCall) = { config with OnUpdate = Some call }

    [<CustomOperation("onDelete")>]
    member _.OnDelete(config: CustomResourceConfig, call: AwsSdkCall) = { config with OnDelete = Some call }

    [<CustomOperation("policy")>]
    member _.Policy(config: CustomResourceConfig, policy: AwsCustomResourcePolicy) =
        { config with Policy = Some policy }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: CustomResourceConfig, timeout: Duration) = { config with Timeout = Some timeout }

    [<CustomOperation("installLatestAwsSdk")>]
    member _.InstallLatestAwsSdk(config: CustomResourceConfig, install: bool) =
        { config with
            InstallLatestAwsSdk = Some install }

    [<CustomOperation("logRetention")>]
    member _.LogRetention(config: CustomResourceConfig, retention: RetentionDays) =
        { config with
            LogRetention = Some retention }

/// Helper functions for Custom Resource operations
module CustomResourceHelpers =

    /// Creates an SDK call for S3 operations
    let s3PutObject (bucket: string) (key: string) (body: string) =
        AwsSdkCall(
            Service = "S3",
            Action = "putObject",
            Parameters = dict [ "Bucket", box bucket; "Key", box key; "Body", box body ],
            PhysicalResourceId = PhysicalResourceId.Of($"{bucket}/{key}")
        )

    /// Creates an SDK call for DynamoDB operations
    let dynamoDBPutItem (tableName: string) (item: System.Collections.Generic.Dictionary<string, obj>) =
        AwsSdkCall(
            Service = "DynamoDB",
            Action = "putItem",
            Parameters = dict [ "TableName", box tableName; "Item", box item ],
            PhysicalResourceId = PhysicalResourceId.Of($"{tableName}-seed")
        )

    /// Creates an SDK call for Secrets Manager
    let secretsManagerPutSecretValue (secretId: string) (secretString: string) =
        AwsSdkCall(
            Service = "SecretsManager",
            Action = "putSecretValue",
            Parameters = dict [ "SecretId", box secretId; "SecretString", box secretString ],
            PhysicalResourceId = PhysicalResourceId.Of(secretId)
        )

    /// Creates an SDK call for SSM Parameter Store
    let ssmPutParameter (name: string) (value: string) (parameterType: string) =
        AwsSdkCall(
            Service = "SSM",
            Action = "putParameter",
            Parameters =
                dict
                    [ "Name", box name
                      "Value", box value
                      "Type", box parameterType
                      "Overwrite", box true ],
            PhysicalResourceId = PhysicalResourceId.Of(name)
        )

    /// Creates an SDK call for RDS database creation
    let rdsCreateDatabase (dbInstanceIdentifier: string) (databaseName: string) =
        AwsSdkCall(
            Service = "RDS",
            Action = "createDatabase",
            Parameters =
                dict
                    [ "DBInstanceIdentifier", box dbInstanceIdentifier
                      "DatabaseName", box databaseName ],
            PhysicalResourceId = PhysicalResourceId.Of($"{dbInstanceIdentifier}/{databaseName}")
        )

    /// Creates a generic AWS SDK call
    let createSdkCall
        (service: string)
        (action: string)
        (parameters: (string * obj) list)
        (physicalResourceId: string)
        =
        AwsSdkCall(
            Service = service,
            Action = action,
            Parameters = (parameters |> dict),
            PhysicalResourceId = PhysicalResourceId.Of(physicalResourceId)
        )

    /// Common timeout durations
    module Timeouts =
        let oneMinute = Duration.Minutes(1.0)
        let fiveMinutes = Duration.Minutes(5.0)
        let tenMinutes = Duration.Minutes(10.0)
        let fifteenMinutes = Duration.Minutes(15.0)

[<AutoOpen>]
module CustomResourceBuilders =
    /// <summary>
    /// Creates a new Custom Resource builder for deployment-time tasks.
    /// Example: customResource "db-seeder" { onCreate (CustomResourceHelpers.dynamoDBPutItem "MyTable" seedData) }
    /// </summary>
    let customResource name = CustomResourceBuilder name
