namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.KMS

/// <summary>
/// High-level CloudWatch Log Group builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Retention = 7 days (cost-effective default, change for compliance)
/// - Removal policy = DESTROY (logs cleaned up on stack deletion)
/// - Encryption = AWS managed key (optional customer managed key)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - 7-day retention balances observability and cost
/// - DESTROY removal policy prevents orphaned log groups
/// - Encryption at rest protects sensitive log data
///
/// **Escape Hatch:**
/// Access the underlying CDK LogGroup via the `LogGroup` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type CloudWatchLogGroupConfig =
    { LogGroupName: string
      ConstructId: string option
      Retention: RetentionDays voption
      RemovalPolicy: RemovalPolicy voption
      EncryptionKey: KMSKeyRef option
      LogGroupClass: LogGroupClass voption }

type CloudWatchLogGroupResource =
    {
        LogGroupName: string
        ConstructId: string
        /// The underlying CDK LogGroup construct
        LogGroup: LogGroup
    }

type CloudWatchLogGroupBuilder(name: string) =
    member _.Yield _ : CloudWatchLogGroupConfig =
        { LogGroupName = name
          ConstructId = None
          Retention = ValueSome RetentionDays.ONE_WEEK
          RemovalPolicy = ValueSome RemovalPolicy.DESTROY
          EncryptionKey = None
          LogGroupClass = ValueNone }

    member _.Zero() : CloudWatchLogGroupConfig =
        { LogGroupName = name
          ConstructId = None
          Retention = ValueSome RetentionDays.ONE_WEEK
          RemovalPolicy = ValueSome RemovalPolicy.DESTROY
          EncryptionKey = None
          LogGroupClass = ValueNone }

    member _.Combine(state1: CloudWatchLogGroupConfig, state2: CloudWatchLogGroupConfig) : CloudWatchLogGroupConfig =
        { LogGroupName = state2.LogGroupName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Retention = state2.Retention |> ValueOption.orElse state1.Retention
          RemovalPolicy = state2.RemovalPolicy |> ValueOption.orElse state1.RemovalPolicy
          EncryptionKey = state2.EncryptionKey |> Option.orElse state1.EncryptionKey
          LogGroupClass = state2.LogGroupClass |> ValueOption.orElse state1.LogGroupClass }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudWatchLogGroupConfig) : CloudWatchLogGroupConfig = f ()

    member inline x.For
        (
            config: CloudWatchLogGroupConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchLogGroupConfig
        ) : CloudWatchLogGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchLogGroupConfig) : CloudWatchLogGroupResource =
        let logGroupName = config.LogGroupName
        let constructId = config.ConstructId |> Option.defaultValue logGroupName

        let props = LogGroupProps()
        props.LogGroupName <- logGroupName

        config.Retention |> ValueOption.iter (fun v -> props.Retention <- v)
        config.RemovalPolicy |> ValueOption.iter (fun v -> props.RemovalPolicy <- v)
        config.LogGroupClass |> ValueOption.iter (fun v -> props.LogGroupClass <- v)

        config.EncryptionKey
        |> Option.iter (fun v ->
            props.EncryptionKey <-
                match v with
                | KMSKeyRef.KMSKeyInterface i -> i
                | KMSKeyRef.KMSKeySpecRef pr ->
                    match pr.Key with
                    | Some k -> k
                    | None -> failwith $"Key {pr.KeyName} has to be resolved first")

        { LogGroupName = logGroupName
          ConstructId = constructId
          LogGroup = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchLogGroupConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("retention")>]
    member _.Retention(config: CloudWatchLogGroupConfig, retention: RetentionDays) =
        { config with
            Retention = ValueSome retention }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: CloudWatchLogGroupConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = ValueSome policy }

    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: CloudWatchLogGroupConfig, key: IKey) =
        { config with
            EncryptionKey = Some(KMSKeyRef.KMSKeyInterface key) }

    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: CloudWatchLogGroupConfig, key: KMSKeySpec) =
        { config with
            EncryptionKey = Some(KMSKeyRef.KMSKeySpecRef key) }

    [<CustomOperation("logGroupClass")>]
    member _.LogGroupClass(config: CloudWatchLogGroupConfig, logClass: LogGroupClass) =
        { config with
            LogGroupClass = ValueSome logClass }

/// Helper functions for CloudWatch Logs
module CloudWatchLogsHelpers =

    /// Common retention periods for different use cases
    module RetentionPeriods =
        /// Development/testing: 3 days
        let dev = RetentionDays.THREE_DAYS

        /// Standard retention: 7 days
        let standard = RetentionDays.ONE_WEEK

        /// Production: 30 days
        let production = RetentionDays.ONE_MONTH

        /// Compliance: 90 days
        let compliance = RetentionDays.THREE_MONTHS

        /// Long-term: 1 year
        let longTerm = RetentionDays.ONE_YEAR

        /// Audit logs: 5 years
        let audit = RetentionDays.FIVE_YEARS

        /// Permanent: Infinite retention
        let permanent = RetentionDays.INFINITE

    /// Creates a log group name for ECS services
    let ecsLogGroup (serviceName: string) (environment: string) =
        sprintf "/aws/ecs/%s-%s" serviceName environment

    /// Creates a log group name for Lambda functions
    let lambdaLogGroup (functionName: string) = sprintf "/aws/lambda/%s" functionName

    /// Creates a log group name for API Gateway
    let apiGatewayLogGroup (apiName: string) (stage: string) =
        sprintf "/aws/apigateway/%s/%s" apiName stage

    /// Creates a log group name for application logs
    let appLogGroup (appName: string) (environment: string) = sprintf "/%s/%s" appName environment

[<AutoOpen>]
module CloudWatchLogsBuilders =
    /// <summary>
    /// Creates a new CloudWatch Log Group builder with sensible defaults.
    /// Example: logGroup "/aws/ecs/my-service" { retention RetentionDays.ONE_MONTH }
    /// </summary>
    let logGroup name = CloudWatchLogGroupBuilder name
