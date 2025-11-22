namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.StepFunctions
open Amazon.CDK.AWS.StepFunctions.Tasks
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.IAM

// ============================================================================
// Step Functions Configuration
// ============================================================================

/// <summary>
/// High-level AWS Step Functions (State Machine) builder following AWS best practices.
///
/// **Default Settings:**
/// - StateMachineType = STANDARD (for long-running workflows)
/// - Logging = ALL events logged to CloudWatch (requires logDestination for Stack deployment)
/// - TracingEnabled = true (X-Ray integration)
/// - Timeout = 1 hour (configurable)
///
/// **Rationale:**
/// These defaults follow Yan Cui's production serverless best practices:
/// - Standard type for reliable, exactly-once execution
/// - Full logging for debugging and audit trails (requires logDestination when deploying)
/// - X-Ray tracing for distributed system visibility
/// - Reasonable timeout prevents runaway workflows
///
/// **Use Cases:**
/// - Order processing workflows
/// - ETL data pipelines
/// - Human approval workflows
/// - Saga orchestration patterns
/// - Microservice orchestration
///
/// **Escape Hatch:**
/// Access the underlying CDK StateMachine via the `StateMachine` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type StepFunctionConfig =
    { StateMachineName: string
      ConstructId: string option
      Definition: IChainable option
      StateMachineType: StateMachineType voption
      Timeout: Duration option
      TracingEnabled: bool voption
      LoggingLevel: LogLevel voption
      LogDestination: ILogGroup option
      Role: IRole option
      Comment: string option }

type StepFunctionSpec =
    { StateMachineName: string
      ConstructId: string
      Props: StateMachineProps
      mutable StateMachine: StateMachine option }

    /// Gets the state machine ARN
    member this.StateMachineArn =
        match this.StateMachine with
        | Some sm -> sm.StateMachineArn
        | None -> null

type StepFunctionBuilder(name: string) =
    member _.Yield(_: unit) : StepFunctionConfig =
        { StateMachineName = name
          ConstructId = None
          Definition = None
          StateMachineType = ValueSome StateMachineType.STANDARD
          Timeout = Some(Duration.Hours(1.0))
          TracingEnabled = ValueSome true
          LoggingLevel = ValueSome LogLevel.ALL
          LogDestination = None
          Role = None
          Comment = None }

    member _.Zero() : StepFunctionConfig =
        { StateMachineName = name
          ConstructId = None
          Definition = None
          StateMachineType = ValueSome StateMachineType.STANDARD
          Timeout = Some(Duration.Hours(1.0))
          TracingEnabled = ValueSome true
          LoggingLevel = ValueSome LogLevel.ALL
          LogDestination = None
          Role = None
          Comment = None }

    member _.Combine(state1: StepFunctionConfig, state2: StepFunctionConfig) : StepFunctionConfig =
        { StateMachineName = state2.StateMachineName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Definition = state2.Definition |> Option.orElse state1.Definition
          StateMachineType = state2.StateMachineType |> ValueOption.orElse state1.StateMachineType
          Timeout = state2.Timeout |> Option.orElse state1.Timeout
          TracingEnabled = state2.TracingEnabled |> ValueOption.orElse state1.TracingEnabled
          LoggingLevel = state2.LoggingLevel |> ValueOption.orElse state1.LoggingLevel
          LogDestination = state2.LogDestination |> Option.orElse state1.LogDestination
          Role = state2.Role |> Option.orElse state1.Role
          Comment = state2.Comment |> Option.orElse state1.Comment }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StepFunctionConfig) : StepFunctionConfig = f ()

    member inline x.For
        (
            config: StepFunctionConfig,
            [<InlineIfLambda>] f: unit -> StepFunctionConfig
        ) : StepFunctionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: StepFunctionConfig) : StepFunctionSpec =
        let stateMachineName = config.StateMachineName
        let constructId = config.ConstructId |> Option.defaultValue stateMachineName

        let props = StateMachineProps()
        props.StateMachineName <- stateMachineName

        match config.Definition with
        | Some definition -> props.DefinitionBody <- DefinitionBody.FromChainable(definition)
        | None -> failwith "Definition is required for Step Function state machine"

        config.StateMachineType
        |> ValueOption.iter (fun v -> props.StateMachineType <- v)

        config.Timeout |> Option.iter (fun v -> props.Timeout <- v)

        config.TracingEnabled
        |> ValueOption.iter (fun v -> props.TracingEnabled <- System.Nullable<bool>(v))

        config.Role |> Option.iter (fun role -> props.Role <- role)

        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        // Logging configuration
        // Note: AWS requires a log destination when logging level is not OFF
        match config.LoggingLevel with
        | ValueSome level ->
            match config.LogDestination with
            | Some logGroup ->
                let logOptions = LogOptions()
                logOptions.Level <- level
                logOptions.Destination <- logGroup
                logOptions.IncludeExecutionData <- System.Nullable<bool>(true)
                props.Logs <- logOptions
            | None -> ()
        | _ -> ()
        // No logging if level not set or destination not provided

        { StateMachineName = stateMachineName
          ConstructId = constructId
          Props = props
          StateMachine = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: StepFunctionConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("definition")>]
    member _.Definition(config: StepFunctionConfig, definition: IChainable) =
        { config with
            Definition = Some definition }

    [<CustomOperation("stateMachineType")>]
    member _.StateMachineType(config: StepFunctionConfig, machineType: StateMachineType) =
        { config with
            StateMachineType = ValueSome machineType }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: StepFunctionConfig, timeout: Duration) = { config with Timeout = Some timeout }

    [<CustomOperation("tracingEnabled")>]
    member _.TracingEnabled(config: StepFunctionConfig, enabled: bool) =
        { config with
            TracingEnabled = ValueSome enabled }

    [<CustomOperation("loggingLevel")>]
    member _.LoggingLevel(config: StepFunctionConfig, level: LogLevel) =
        { config with
            LoggingLevel = ValueSome level }

    [<CustomOperation("logDestination")>]
    member _.LogDestination(config: StepFunctionConfig, destination: ILogGroup) =
        { config with
            LogDestination = Some destination }

    [<CustomOperation("role")>]
    member _.Role(config: StepFunctionConfig, role: IRole) = { config with Role = Some role }

    [<CustomOperation("comment")>]
    member _.Comment(config: StepFunctionConfig, comment: string) = { config with Comment = Some comment }

/// Helper functions for Step Functions operations
module StepFunctionHelpers =

    // Note: State creation helpers require a Construct scope.
    // Users should create states within a state machine definition context.
    // Example:
    //   let definition =
    //       let task1 = LambdaInvoke(scope, "InvokeFunction", LambdaInvokeProps(...))
    //       let task2 = Pass(scope, "PassState", PassProps(...))
    //       task1.Next(task2)

    /// Chain interface for sequencing states
    let chain (first: IChainable) (second: IChainable) = Chain.Start(first).Next(second)

    /// Common state machine types
    module StateMachineTypes =
        let standard = StateMachineType.STANDARD // Long-running, exactly-once
        let express = StateMachineType.EXPRESS // Short-lived, at-least-once, cheaper

    /// Common timeout durations
    module Timeouts =
        let fiveMinutes = Duration.Minutes(5.0)
        let fifteenMinutes = Duration.Minutes(15.0)
        let thirtyMinutes = Duration.Minutes(30.0)
        let oneHour = Duration.Hours(1.0)
        let oneDay = Duration.Days(1.0)

    /// Common logging levels
    module LoggingLevels =
        let off = LogLevel.OFF
        let error = LogLevel.ERROR
        let all = LogLevel.ALL

[<AutoOpen>]
module StepFunctionBuilders =
    /// <summary>
    /// Creates a new Step Functions state machine builder.
    /// Example: stepFunction "OrderWorkflow" { definition (taskState "ProcessOrder" processLambda) }
    /// </summary>
    let stepFunction name = StepFunctionBuilder name
