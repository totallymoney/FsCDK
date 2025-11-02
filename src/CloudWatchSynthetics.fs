namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Synthetics
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.S3

/// <summary>
/// High-level CloudWatch Synthetics Canary builder following AWS best practices.
///
/// **Default Settings:**
/// - Runtime = Synthetics Python 3.10
/// - Schedule = Every 5 minutes
/// - Timeout = 60 seconds
/// - Memory = 960 MB
/// - Active tracing = enabled (X-Ray)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Python runtime provides flexibility and ease of use
/// - 5-minute checks balance cost and availability monitoring
/// - Active tracing helps debug canary failures
/// - Memory sized for typical HTTP/HTTPS checks
///
/// **Use Cases:**
/// - Website uptime monitoring
/// - API endpoint health checks
/// - User workflow validation
/// - Multi-step transactions
///
/// **Escape Hatch:**
/// Access the underlying CDK Canary via the `Canary` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type CloudWatchCanaryConfig =
    { CanaryName: string
      ConstructId: string option
      Runtime: Runtime option
      Test: Test option
      Schedule: Schedule option
      ArtifactsBucketLocation: ArtifactsBucketLocation option
      FailureRetentionPeriod: Duration option
      SuccessRetentionPeriod: Duration option
      TimeToLive: Duration option
      EnvironmentVariables: Map<string, string> option
      Role: IRole option
      StartAfterCreation: bool option }

type CloudWatchCanaryResource =
    {
        CanaryName: string
        ConstructId: string
        /// The underlying CDK Canary construct
        Canary: Canary
    }

type CloudWatchCanaryBuilder(name: string) =
    member _.Yield _ : CloudWatchCanaryConfig =
        { CanaryName = name
          ConstructId = None
          Runtime = Some Runtime.SYNTHETICS_PYTHON_SELENIUM_3_0
          Test = None
          Schedule = Some(Schedule.Rate(Duration.Minutes(5.0)))
          ArtifactsBucketLocation = None
          FailureRetentionPeriod = Some(Duration.Days(31.0))
          SuccessRetentionPeriod = Some(Duration.Days(31.0))
          TimeToLive = Some(Duration.Seconds(60.0))
          EnvironmentVariables = None
          Role = None
          StartAfterCreation = Some true }

    member _.Zero() : CloudWatchCanaryConfig =
        { CanaryName = name
          ConstructId = None
          Runtime = Some Runtime.SYNTHETICS_PYTHON_SELENIUM_3_0
          Test = None
          Schedule = Some(Schedule.Rate(Duration.Minutes(5.0)))
          ArtifactsBucketLocation = None
          FailureRetentionPeriod = Some(Duration.Days(31.0))
          SuccessRetentionPeriod = Some(Duration.Days(31.0))
          TimeToLive = Some(Duration.Seconds(60.0))
          EnvironmentVariables = None
          Role = None
          StartAfterCreation = Some true }

    member _.Combine(state1: CloudWatchCanaryConfig, state2: CloudWatchCanaryConfig) : CloudWatchCanaryConfig =
        { CanaryName = state2.CanaryName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Runtime = state2.Runtime |> Option.orElse state1.Runtime
          Test = state2.Test |> Option.orElse state1.Test
          Schedule = state2.Schedule |> Option.orElse state1.Schedule
          ArtifactsBucketLocation = state2.ArtifactsBucketLocation |> Option.orElse state1.ArtifactsBucketLocation
          FailureRetentionPeriod = state2.FailureRetentionPeriod |> Option.orElse state1.FailureRetentionPeriod
          SuccessRetentionPeriod = state2.SuccessRetentionPeriod |> Option.orElse state1.SuccessRetentionPeriod
          TimeToLive = state2.TimeToLive |> Option.orElse state1.TimeToLive
          EnvironmentVariables = state2.EnvironmentVariables |> Option.orElse state1.EnvironmentVariables
          Role = state2.Role |> Option.orElse state1.Role
          StartAfterCreation = state2.StartAfterCreation |> Option.orElse state1.StartAfterCreation }

    member inline x.For
        (
            config: CloudWatchCanaryConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchCanaryConfig
        ) : CloudWatchCanaryConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchCanaryConfig) : CloudWatchCanaryResource =
        let canaryName = config.CanaryName
        let constructId = config.ConstructId |> Option.defaultValue canaryName

        let props = CanaryProps()
        props.CanaryName <- canaryName

        match config.Runtime with
        | Some runtime -> props.Runtime <- runtime
        | None -> failwith "Runtime is required for Canary"

        match config.Test with
        | Some test -> props.Test <- test
        | None -> failwith "Test is required for Canary"

        config.Schedule |> Option.iter (fun v -> props.Schedule <- v)

        config.ArtifactsBucketLocation
        |> Option.iter (fun v -> props.ArtifactsBucketLocation <- v)

        config.FailureRetentionPeriod
        |> Option.iter (fun v -> props.FailureRetentionPeriod <- v)

        config.SuccessRetentionPeriod
        |> Option.iter (fun v -> props.SuccessRetentionPeriod <- v)

        config.TimeToLive |> Option.iter (fun v -> props.TimeToLive <- v)
        config.Role |> Option.iter (fun v -> props.Role <- v)

        config.StartAfterCreation
        |> Option.iter (fun v -> props.StartAfterCreation <- System.Nullable<bool>(v))

        config.EnvironmentVariables
        |> Option.iter (fun vars ->
            let dict = System.Collections.Generic.Dictionary<string, string>()
            vars |> Map.iter (fun k v -> dict.Add(k, v))
            props.EnvironmentVariables <- dict)

        { CanaryName = canaryName
          ConstructId = constructId
          Canary = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchCanaryConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("runtime")>]
    member _.Runtime(config: CloudWatchCanaryConfig, runtime: Runtime) = { config with Runtime = Some runtime }

    [<CustomOperation("test")>]
    member _.Test(config: CloudWatchCanaryConfig, test: Test) = { config with Test = Some test }

    [<CustomOperation("schedule")>]
    member _.Schedule(config: CloudWatchCanaryConfig, schedule: Schedule) =
        { config with Schedule = Some schedule }

    [<CustomOperation("artifactsBucket")>]
    member _.ArtifactsBucket(config: CloudWatchCanaryConfig, bucket: IBucket) =
        { config with
            ArtifactsBucketLocation = Some(ArtifactsBucketLocation(Bucket = bucket)) }

    [<CustomOperation("failureRetentionPeriod")>]
    member _.FailureRetentionPeriod(config: CloudWatchCanaryConfig, period: Duration) =
        { config with
            FailureRetentionPeriod = Some period }

    [<CustomOperation("successRetentionPeriod")>]
    member _.SuccessRetentionPeriod(config: CloudWatchCanaryConfig, period: Duration) =
        { config with
            SuccessRetentionPeriod = Some period }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: CloudWatchCanaryConfig, timeout: Duration) =
        { config with
            TimeToLive = Some timeout }

    [<CustomOperation("envVar")>]
    member _.EnvVar(config: CloudWatchCanaryConfig, key: string, value: string) =
        let currentVars = config.EnvironmentVariables |> Option.defaultValue Map.empty

        { config with
            EnvironmentVariables = Some(currentVars |> Map.add key value) }

    [<CustomOperation("role")>]
    member _.Role(config: CloudWatchCanaryConfig, role: IRole) = { config with Role = Some role }

    [<CustomOperation("startAfterCreation")>]
    member _.StartAfterCreation(config: CloudWatchCanaryConfig, start: bool) =
        { config with
            StartAfterCreation = Some start }

/// Helper functions for CloudWatch Synthetics
module CloudWatchSyntheticsHelpers =

    /// Creates inline code for a simple HTTP GET check
    let httpGetCheck (url: string) =
        let script =
            $"""
from aws_synthetics.selenium import synthetics_webdriver as webdriver
from aws_synthetics.common import synthetics_logger as logger

def handler(event, context):
    logger.info("Starting canary")
    browser = webdriver.Chrome()
    browser.get("{url}")
    logger.info("Navigated to URL: {url}")
    
    response_code = browser.execute_script("return window.performance.getEntries()[0].responseStatus")
    if response_code != 200:
        raise Exception(f"Failed with response code: {{response_code}}")
    
    logger.info("Canary completed successfully")
    browser.quit()
"""

        Code.FromInline(script)

    /// Creates inline code for a simple HTTPS check with status validation
    let httpsGetCheck (url: string) (expectedStatus: int) =
        let script =
            $"""
from aws_synthetics.selenium import synthetics_webdriver as webdriver
from aws_synthetics.common import synthetics_logger as logger

def handler(event, context):
    logger.info("Starting HTTPS canary")
    browser = webdriver.Chrome()
    browser.get("{url}")
    logger.info("Navigated to URL: {url}")
    
    response_code = browser.execute_script("return window.performance.getEntries()[0].responseStatus")
    if response_code != {expectedStatus}:
        raise Exception(f"Expected {{expected}}, got {{response_code}}")
    
    logger.info("Canary completed successfully")
    browser.quit()
"""

        Code.FromInline(script)

    /// Schedule helpers
    let everyMinute () = Schedule.Rate(Duration.Minutes(1.0))
    let every5Minutes () = Schedule.Rate(Duration.Minutes(5.0))
    let every15Minutes () = Schedule.Rate(Duration.Minutes(15.0))
    let everyHour () = Schedule.Rate(Duration.Hours(1.0))

[<AutoOpen>]
module CloudWatchSyntheticsBuilders =
    /// <summary>
    /// Creates a new CloudWatch Synthetics Canary builder.
    /// Example: canary "my-website-check" { test (httpGetCheck "https://example.com"); schedule (every5Minutes()) }
    /// </summary>
    let canary name = CloudWatchCanaryBuilder name
