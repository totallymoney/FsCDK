namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CloudTrail
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.Logs

// ============================================================================
// CloudTrail Configuration DSL
// ============================================================================

/// <summary>
/// Represents a CloudTrail trail specification after configuration.
/// </summary>
type CloudTrailSpec =
    { TrailName: string
      ConstructId: string
      Props: TrailProps
      CloudWatchLogsRetention: RetentionDays option
      SendToCloudWatchLogs: bool
      mutable Trail: Trail option }

/// <summary>
/// High-level CloudTrail builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - IsMultiRegionTrail = true (captures events from all regions)
/// - IncludeGlobalServiceEvents = true (includes IAM, STS, CloudFront events)
/// - EnableFileValidation = true (enables log file integrity validation)
/// - ManagementEvents = ReadWriteType.ALL (logs all management events)
/// - SendToCloudWatchLogs = true (enables CloudWatch Logs integration)
///
/// **Rationale:**
/// CloudTrail provides audit logs of all AWS API calls, which is critical for:
/// - Security incident investigation and forensics
/// - Compliance requirements (HIPAA, PCI-DSS, SOC2, GDPR)
/// - Detecting unauthorized access or privilege escalation
/// - Meeting AWS Well-Architected Framework security pillar requirements
///
/// Per "Security as Code" (O'Reilly):
/// "Log all API calls with CloudTrail. This is non-negotiable for security monitoring."
///
/// **Note on Costs:**
/// The first trail recording management events is free. Additional trails or data events incur charges.
/// CloudWatch Logs integration has additional costs but provides real-time monitoring capabilities.
/// </summary>
type CloudTrailConfig =
    { TrailName: string
      ConstructId: string option
      IsMultiRegionTrail: bool option
      IncludeGlobalServiceEvents: bool option
      EnableFileValidation: bool option
      ManagementEvents: ReadWriteType option
      S3Bucket: IBucket option
      CloudWatchLogsRetention: RetentionDays option
      SendToCloudWatchLogs: bool option
      IsOrganizationTrail: bool option }

type CloudTrailBuilder(name: string) =

    member _.Yield(_: unit) : CloudTrailConfig =
        { TrailName = name
          ConstructId = None
          IsMultiRegionTrail = Some true
          IncludeGlobalServiceEvents = Some true
          EnableFileValidation = Some true
          ManagementEvents = Some ReadWriteType.ALL
          S3Bucket = None
          CloudWatchLogsRetention = Some RetentionDays.ONE_MONTH
          SendToCloudWatchLogs = Some true
          IsOrganizationTrail = None }

    member _.Zero() : CloudTrailConfig =
        { TrailName = name
          ConstructId = None
          IsMultiRegionTrail = Some true
          IncludeGlobalServiceEvents = Some true
          EnableFileValidation = Some true
          ManagementEvents = Some ReadWriteType.ALL
          S3Bucket = None
          CloudWatchLogsRetention = Some RetentionDays.ONE_MONTH
          SendToCloudWatchLogs = Some true
          IsOrganizationTrail = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudTrailConfig) : CloudTrailConfig = f ()

    member inline x.For(config: CloudTrailConfig, [<InlineIfLambda>] f: unit -> CloudTrailConfig) : CloudTrailConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: CloudTrailConfig, state2: CloudTrailConfig) : CloudTrailConfig =
        { TrailName = state1.TrailName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          IsMultiRegionTrail = state2.IsMultiRegionTrail |> Option.orElse state1.IsMultiRegionTrail
          IncludeGlobalServiceEvents =
            state2.IncludeGlobalServiceEvents
            |> Option.orElse state1.IncludeGlobalServiceEvents
          EnableFileValidation = state2.EnableFileValidation |> Option.orElse state1.EnableFileValidation
          ManagementEvents = state2.ManagementEvents |> Option.orElse state1.ManagementEvents
          S3Bucket = state2.S3Bucket |> Option.orElse state1.S3Bucket
          CloudWatchLogsRetention = state2.CloudWatchLogsRetention |> Option.orElse state1.CloudWatchLogsRetention
          SendToCloudWatchLogs = state2.SendToCloudWatchLogs |> Option.orElse state1.SendToCloudWatchLogs
          IsOrganizationTrail = state2.IsOrganizationTrail |> Option.orElse state1.IsOrganizationTrail }

    member _.Run(config: CloudTrailConfig) : CloudTrailSpec =
        let props = TrailProps()
        let constructId = config.ConstructId |> Option.defaultValue config.TrailName

        // Security best practice: Multi-region by default
        props.IsMultiRegionTrail <- config.IsMultiRegionTrail |> Option.defaultValue true

        // Security best practice: Include global service events (IAM, STS, etc.)
        props.IncludeGlobalServiceEvents <- config.IncludeGlobalServiceEvents |> Option.defaultValue true

        // Security best practice: Enable log file validation for integrity checking
        props.EnableFileValidation <- config.EnableFileValidation |> Option.defaultValue true

        // Security best practice: Log all management events
        props.ManagementEvents <- config.ManagementEvents |> Option.defaultValue ReadWriteType.ALL

        // Optional: Custom S3 bucket (if not provided, CDK will create one)
        config.S3Bucket |> Option.iter (fun bucket -> props.Bucket <- bucket)

        // Optional: Organization trail (for AWS Organizations)
        config.IsOrganizationTrail
        |> Option.iter (fun isOrg -> props.IsOrganizationTrail <- isOrg)

        { TrailName = config.TrailName
          ConstructId = constructId
          Props = props
          CloudWatchLogsRetention = config.CloudWatchLogsRetention
          SendToCloudWatchLogs = config.SendToCloudWatchLogs |> Option.defaultValue true
          Trail = None }

    /// <summary>Sets the construct ID for the CloudTrail.</summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="id">The construct ID.</param>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudTrailConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>
    /// Sets whether this is a multi-region trail.
    /// **Security Best Practice:** Multi-region trails are enabled by default to capture events from all AWS regions.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="enabled">Whether to enable multi-region (default: true).</param>
    /// <code lang="fsharp">
    /// cloudTrail "MyTrail" {
    ///     isMultiRegionTrail false // Only if single-region monitoring is acceptable
    /// }
    /// </code>
    [<CustomOperation("isMultiRegionTrail")>]
    member _.IsMultiRegionTrail(config: CloudTrailConfig, enabled: bool) =
        { config with
            IsMultiRegionTrail = Some enabled }

    /// <summary>
    /// Sets whether to include global service events (IAM, STS, CloudFront, etc.).
    /// **Security Best Practice:** Enabled by default to capture critical security events.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="enabled">Whether to include global events (default: true).</param>
    [<CustomOperation("includeGlobalServiceEvents")>]
    member _.IncludeGlobalServiceEvents(config: CloudTrailConfig, enabled: bool) =
        { config with
            IncludeGlobalServiceEvents = Some enabled }

    /// <summary>
    /// Sets whether to enable log file validation.
    /// **Security Best Practice:** Enabled by default for log integrity verification.
    /// This allows you to detect if log files were tampered with after delivery.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="enabled">Whether to enable validation (default: true).</param>
    [<CustomOperation("enableFileValidation")>]
    member _.EnableFileValidation(config: CloudTrailConfig, enabled: bool) =
        { config with
            EnableFileValidation = Some enabled }

    /// <summary>
    /// Sets the management event logging type.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="readWriteType">The type of events to log (default: ReadWriteType.ALL).</param>
    /// <code lang="fsharp">
    /// cloudTrail "MyTrail" {
    ///     managementEvents ReadWriteType.WRITE_ONLY // Only log write events
    /// }
    /// </code>
    [<CustomOperation("managementEvents")>]
    member _.ManagementEvents(config: CloudTrailConfig, readWriteType: ReadWriteType) =
        { config with
            ManagementEvents = Some readWriteType }

    /// <summary>
    /// Sets a custom S3 bucket for CloudTrail logs.
    /// If not specified, CDK will create a bucket with appropriate security settings.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="bucket">The S3 bucket interface.</param>
    [<CustomOperation("s3Bucket")>]
    member _.S3Bucket(config: CloudTrailConfig, bucket: IBucket) = { config with S3Bucket = Some(bucket) }

    /// <summary>
    /// Sets the CloudWatch Logs retention period for the trail.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="retention">The retention period (default: ONE_MONTH).</param>
    /// <code lang="fsharp">
    /// cloudTrail "MyTrail" {
    ///     cloudWatchLogsRetention RetentionDays.THREE_MONTHS
    /// }
    /// </code>
    [<CustomOperation("cloudWatchLogsRetention")>]
    member _.CloudWatchLogsRetention(config: CloudTrailConfig, retention: RetentionDays) =
        { config with
            CloudWatchLogsRetention = Some retention }

    /// <summary>
    /// Sets whether to send trail logs to CloudWatch Logs.
    /// **Note:** CloudWatch Logs integration enables real-time monitoring but adds cost.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="enabled">Whether to send to CloudWatch (default: true).</param>
    [<CustomOperation("sendToCloudWatchLogs")>]
    member _.SendToCloudWatchLogs(config: CloudTrailConfig, enabled: bool) =
        { config with
            SendToCloudWatchLogs = Some enabled }

    /// <summary>
    /// Sets whether this is an organization trail (requires AWS Organizations).
    /// Organization trails log events for all accounts in the organization.
    /// </summary>
    /// <param name="config">The current CloudTrail configuration.</param>
    /// <param name="enabled">Whether this is an organization trail.</param>
    /// <code lang="fsharp">
    /// cloudTrail "OrgTrail" {
    ///     isOrganizationTrail true
    /// }
    /// </code>
    [<CustomOperation("isOrganizationTrail")>]
    member _.IsOrganizationTrail(config: CloudTrailConfig, enabled: bool) =
        { config with
            IsOrganizationTrail = Some enabled }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CloudTrailBuilders =
    /// <summary>
    /// Creates a CloudTrail configuration with AWS security best practices.
    /// CloudTrail logs all AWS API calls for security monitoring and compliance.
    /// </summary>
    /// <param name="name">The trail name.</param>
    /// <code lang="fsharp">
    /// cloudTrail "SecurityAuditTrail" {
    ///     isMultiRegionTrail true
    ///     includeGlobalServiceEvents true
    ///     enableFileValidation true
    /// }
    /// </code>
    let cloudTrail (name: string) = CloudTrailBuilder(name)
