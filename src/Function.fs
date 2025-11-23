namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CodeGuruProfiler
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.S3.Assets
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.KMS
open System.Collections.Generic

// ============================================================================
// Lambda Function Configuration DSL
// ============================================================================

type FunctionConfig =
    { FunctionName: string
      ConstructId: string option
      Handler: string option
      AdotInstrumentation: IAdotInstrumentationConfig option
      AllowAllOutbound: bool option
      AllowAllIpv6Outbound: bool option
      AllowPublicSubnet: bool option
      ApplicationLogLevelV2: ApplicationLogLevel option
      CodeSigningConfig: ICodeSigningConfig option
      Ipv6AllowedForDualStack: bool option
      Runtime: Runtime option
      CodePath: Code option
      Environment: (string * string) seq
      Timeout: float option
      MemorySize: int option
      ParamsAndSecrets: ParamsAndSecretsLayerVersion option
      Profiling: bool option
      ProfilingGroup: IProfilingGroup option
      RecursiveLoop: RecursiveLoop option
      Description: string option
      EventSources: IEventSource list
      EventSourceMappings: (string * IEventSourceMappingOptions) list
      FunctionUrlOptions: IFunctionUrlOptions list
      EventSource: IEventSource list
      Permissions: IPermission list
      RolePolicyStatements: PolicyStatement list
      AsyncInvokeOptions: IEventInvokeConfigOptions list
      ReservedConcurrentExecutions: int option
      LogGroup: ILogGroup option
      Role: IRole option
      OnFailure: IDestination option
      OnSuccess: IDestination option
      RuntimeManagementMode: RuntimeManagementMode option
      InsightsVersion: LambdaInsightsVersion option
      CurrentVersionOptions: IVersionOptions option
      Layers: ILayerVersion list
      Architecture: Architecture option
      Tracing: Tracing option
      Vpc: IVpc option
      VpcSubnets: ISubnetSelection option
      SecurityGroups: ISecurityGroup list
      SnapStart: SnapStartConf option
      SystemLogLevelV2: SystemLogLevel option
      Events: IEventSource list
      FileSystem: FileSystem option
      DeadLetterQueue: IQueue option
      DeadLetterQueueEnabled: bool option
      DeadLetterTopic: ITopic option
      AutoCreateDLQ: bool option
      LoggingFormat: LoggingFormat option
      LogRetentionRetryOptions: Amazon.CDK.AWS.Lambda.ILogRetentionRetryOptions option
      LogRetentionRole: IRole option
      InitialPolicy: PolicyStatement list
      MaxEventAge: Duration option
      RetryAttempts: int option
      EnvironmentEncryption: IKey option
      AutoAddPowertools: bool option
      EphemeralStorageSize: int option }

type FunctionSpec =
    { FunctionName: string
      ConstructId: string
      Props: FunctionProps
      FunctionUrlOptions: IFunctionUrlOptions list
      EventSources: IEventSource list
      Permissions: IPermission list
      RolePolicyStatements: PolicyStatement list
      EventSourceMappings: (string * IEventSourceMappingOptions) list
      AsyncInvokeOptions: IEventInvokeConfigOptions list
      mutable Function: IFunction option }

type FunctionBuilder(name: string) =
    let defaultConfig () : FunctionConfig =
        { FunctionName = name
          AdotInstrumentation = None
          AllowAllIpv6Outbound = None
          AllowAllOutbound = None
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          MemorySize = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = []
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = []
          ReservedConcurrentExecutions = Some 10
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = Some Tracing.ACTIVE
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          AutoCreateDLQ = Some true
          LoggingFormat = Some LoggingFormat.JSON
          MaxEventAge = None
          RetryAttempts = Some 2
          EnvironmentEncryption = None
          AutoAddPowertools = Some true
          EphemeralStorageSize = None
          AllowPublicSubnet = None
          ApplicationLogLevelV2 = None
          CodeSigningConfig = None
          DeadLetterTopic = None
          Events = []
          InitialPolicy = []
          Ipv6AllowedForDualStack = None
          LogRetentionRetryOptions = None
          LogRetentionRole = None
          ParamsAndSecrets = None
          Profiling = None
          ProfilingGroup = None
          RecursiveLoop = None
          RuntimeManagementMode = None
          SnapStart = None
          SystemLogLevelV2 = None
          Vpc = None
          OnFailure = None
          OnSuccess = None
          EventSource = [] }

    member _.Yield(_: unit) : FunctionConfig = defaultConfig ()

    member _.Zero() : FunctionConfig = defaultConfig ()

    member inline _.Delay([<InlineIfLambda>] f: unit -> FunctionConfig) : FunctionConfig = f ()

    member inline x.For(config: FunctionConfig, [<InlineIfLambda>] f: unit -> FunctionConfig) : FunctionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: FunctionConfig, state2: FunctionConfig) : FunctionConfig =
        { FunctionName = state1.FunctionName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          Handler =
            if state1.Handler.IsSome then
                state1.Handler
            else
                state2.Handler
          Runtime =
            if state1.Runtime.IsSome then
                state1.Runtime
            else
                state2.Runtime
          CodePath =
            if state1.CodePath.IsSome then
                state1.CodePath
            else
                state2.CodePath
          Environment = Seq.append state1.Environment state2.Environment
          Timeout =
            if state1.Timeout.IsSome then
                state1.Timeout
            else
                state2.Timeout
          MemorySize =
            if state1.MemorySize.IsSome then
                state1.MemorySize
            else
                state2.MemorySize
          Description =
            if state1.Description.IsSome then
                state1.Description
            else
                state2.Description
          EventSources = state1.EventSources @ state2.EventSources
          EventSourceMappings = state1.EventSourceMappings @ state2.EventSourceMappings
          FunctionUrlOptions = state1.FunctionUrlOptions @ state2.FunctionUrlOptions
          Permissions = state1.Permissions @ state2.Permissions
          RolePolicyStatements = state1.RolePolicyStatements @ state2.RolePolicyStatements
          AsyncInvokeOptions = state1.AsyncInvokeOptions @ state2.AsyncInvokeOptions
          ReservedConcurrentExecutions =
            if state1.ReservedConcurrentExecutions.IsSome then
                state1.ReservedConcurrentExecutions
            else
                state2.ReservedConcurrentExecutions
          LogGroup =
            (if state1.LogGroup.IsSome then
                 state1.LogGroup
             else
                 state2.LogGroup)
          Role = if state1.Role.IsSome then state1.Role else state2.Role
          InsightsVersion =
            if state1.InsightsVersion.IsSome then
                state1.InsightsVersion
            else
                state2.InsightsVersion
          CurrentVersionOptions =
            if state1.CurrentVersionOptions.IsSome then
                state1.CurrentVersionOptions
            else
                state2.CurrentVersionOptions
          Layers = state1.Layers @ state2.Layers
          Architecture =
            if state1.Architecture.IsSome then
                state1.Architecture
            else
                state2.Architecture
          Tracing =
            if state1.Tracing.IsSome then
                state1.Tracing
            else
                state2.Tracing
          VpcSubnets =
            if state1.VpcSubnets.IsSome then
                state1.VpcSubnets
            else
                state2.VpcSubnets
          SecurityGroups = state1.SecurityGroups @ state2.SecurityGroups
          FileSystem =
            if state1.FileSystem.IsSome then
                state1.FileSystem
            else
                state2.FileSystem
          DeadLetterQueue =
            (if state1.DeadLetterQueue.IsSome then
                 state1.DeadLetterQueue
             else
                 state2.DeadLetterQueue)
          DeadLetterQueueEnabled =
            if state1.DeadLetterQueueEnabled.IsSome then
                state1.DeadLetterQueueEnabled
            else
                state2.DeadLetterQueueEnabled
          LoggingFormat =
            if state1.LoggingFormat.IsSome then
                state1.LoggingFormat
            else
                state2.LoggingFormat
          MaxEventAge =
            if state1.MaxEventAge.IsSome then
                state1.MaxEventAge
            else
                state2.MaxEventAge
          RetryAttempts =
            if state1.RetryAttempts.IsSome then
                state1.RetryAttempts
            else
                state2.RetryAttempts
          EnvironmentEncryption =
            (if state1.EnvironmentEncryption.IsSome then
                 state1.EnvironmentEncryption
             else
                 state2.EnvironmentEncryption)
          AutoCreateDLQ =
            if state1.AutoCreateDLQ.IsSome then
                state1.AutoCreateDLQ
            else
                state2.AutoCreateDLQ
          AutoAddPowertools =
            if state1.AutoAddPowertools.IsSome then
                state1.AutoAddPowertools
            else
                state2.AutoAddPowertools
          EphemeralStorageSize =
            if state1.EphemeralStorageSize.IsSome then
                state1.EphemeralStorageSize
            else
                state2.EphemeralStorageSize
          AdotInstrumentation = state1.AdotInstrumentation |> Option.orElse state2.AdotInstrumentation
          AllowAllIpv6Outbound = state1.AllowAllIpv6Outbound |> Option.orElse state2.AllowAllIpv6Outbound
          AllowAllOutbound = state1.AllowAllOutbound |> Option.orElse state2.AllowAllOutbound
          AllowPublicSubnet = state1.AllowPublicSubnet |> Option.orElse state2.AllowPublicSubnet
          ApplicationLogLevelV2 = state1.ApplicationLogLevelV2 |> Option.orElse state2.ApplicationLogLevelV2
          CodeSigningConfig = state1.CodeSigningConfig |> Option.orElse state2.CodeSigningConfig
          DeadLetterTopic = state1.DeadLetterTopic |> Option.orElse state2.DeadLetterTopic
          Events = state1.Events @ state2.Events
          InitialPolicy = state1.InitialPolicy @ state2.InitialPolicy
          Ipv6AllowedForDualStack = state1.Ipv6AllowedForDualStack |> Option.orElse state2.Ipv6AllowedForDualStack
          LogRetentionRetryOptions = state1.LogRetentionRetryOptions |> Option.orElse state2.LogRetentionRetryOptions
          LogRetentionRole = state1.LogRetentionRole |> Option.orElse state2.LogRetentionRole
          ParamsAndSecrets = state1.ParamsAndSecrets |> Option.orElse state2.ParamsAndSecrets
          Profiling = state1.Profiling |> Option.orElse state2.Profiling
          ProfilingGroup = state1.ProfilingGroup |> Option.orElse state2.ProfilingGroup
          RecursiveLoop = state1.RecursiveLoop |> Option.orElse state2.RecursiveLoop
          RuntimeManagementMode = state1.RuntimeManagementMode |> Option.orElse state2.RuntimeManagementMode
          SnapStart = state1.SnapStart |> Option.orElse state2.SnapStart
          SystemLogLevelV2 = state1.SystemLogLevelV2 |> Option.orElse state2.SystemLogLevelV2
          Vpc = state1.Vpc |> Option.orElse state2.Vpc
          OnFailure = state1.OnFailure |> Option.orElse state2.OnFailure
          OnSuccess = state1.OnSuccess |> Option.orElse state2.OnSuccess
          EventSource = state1.EventSource @ state2.EventSource }


    member _.Run(config: FunctionConfig) : FunctionSpec =
        let props = FunctionProps()

        let constructId = config.ConstructId |> Option.defaultValue config.FunctionName

        props.Code <-
            match config.CodePath with
            | Some c -> c
            | None -> failwith "Lambda code path is required"

        props.Handler <-
            match config.Handler with
            | Some h -> h
            | None -> failwith "Lambda handler is required"

        props.Runtime <-
            match config.Runtime with
            | Some r -> r
            | None -> failwith "Lambda runtime is required"

        config.AdotInstrumentation
        |> Option.iter (fun a -> props.AdotInstrumentation <- a)

        config.AllowAllIpv6Outbound
        |> Option.iter (fun a -> props.AllowAllIpv6Outbound <- a)

        config.AllowAllOutbound |> Option.iter (fun a -> props.AllowAllOutbound <- a)

        config.AllowPublicSubnet |> Option.iter (fun a -> props.AllowPublicSubnet <- a)

        config.ApplicationLogLevelV2
        |> Option.iter (fun a -> props.ApplicationLogLevelV2 <- a)

        config.CodeSigningConfig |> Option.iter (fun c -> props.CodeSigningConfig <- c)

        config.Ipv6AllowedForDualStack
        |> Option.iter (fun a -> props.Ipv6AllowedForDualStack <- a)

        config.LogRetentionRetryOptions
        |> Option.iter (fun l -> props.LogRetentionRetryOptions <- l)

        config.LogRetentionRole |> Option.iter (fun l -> props.LogRetentionRole <- l)

        config.ParamsAndSecrets |> Option.iter (fun p -> props.ParamsAndSecrets <- p)

        config.Profiling |> Option.iter (fun p -> props.Profiling <- p)

        config.ProfilingGroup |> Option.iter (fun p -> props.ProfilingGroup <- p)

        config.RecursiveLoop |> Option.iter (fun r -> props.RecursiveLoop <- r)

        config.RuntimeManagementMode
        |> Option.iter (fun r -> props.RuntimeManagementMode <- r)

        config.SnapStart |> Option.iter (fun s -> props.SnapStart <- s)

        config.SystemLogLevelV2 |> Option.iter (fun s -> props.SystemLogLevelV2 <- s)

        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)

        config.OnFailure |> Option.iter (fun f -> props.OnFailure <- f)

        config.OnSuccess |> Option.iter (fun s -> props.OnSuccess <- s)

        if not (Seq.isEmpty config.Environment) then
            let envDict = Dictionary<string, string>()

            for key, value in config.Environment do
                envDict.Add(key, value)

            props.Environment <- envDict

        config.Timeout |> Option.iter (fun t -> props.Timeout <- Duration.Seconds(t))
        config.MemorySize |> Option.iter (fun m -> props.MemorySize <- m)
        config.Description |> Option.iter (fun d -> props.Description <- d)

        config.ReservedConcurrentExecutions
        |> Option.iter (fun r -> props.ReservedConcurrentExecutions <- r)

        config.LogGroup |> Option.iter (fun lg -> props.LogGroup <- lg)

        config.Role |> Option.iter (fun r -> props.Role <- r)
        config.InsightsVersion |> Option.iter (fun v -> props.InsightsVersion <- v)

        config.CurrentVersionOptions
        |> Option.iter (fun v -> props.CurrentVersionOptions <- v)

        config.Architecture |> Option.iter (fun a -> props.Architecture <- a)
        config.Tracing |> Option.iter (fun t -> props.Tracing <- t)
        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        config.Events
        |> List.iter (fun e -> props.Events <- Array.append props.Events [| e |])

        config.InitialPolicy
        |> List.iter (fun p -> props.InitialPolicy <- Array.append props.InitialPolicy [| p |])

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.FileSystem |> Option.iter (fun fs -> props.Filesystem <- fs)

        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)

        config.DeadLetterQueueEnabled
        |> Option.iter (fun e -> props.DeadLetterQueueEnabled <- e)

        config.LoggingFormat |> Option.iter (fun f -> props.LoggingFormat <- f)

        config.EnvironmentEncryption
        |> Option.iter (fun k -> props.EnvironmentEncryption <- k)

        config.DeadLetterTopic |> Option.iter (fun t -> props.DeadLetterTopic <- t)

        if not (List.isEmpty config.Layers) then
            props.Layers <- config.Layers |> List.toArray

        config.EphemeralStorageSize
        |> Option.iter (fun size -> props.EphemeralStorageSize <- Size.Mebibytes(size))

        { FunctionName = config.FunctionName
          ConstructId = constructId
          Props = props
          FunctionUrlOptions = config.FunctionUrlOptions
          EventSources = config.EventSource
          EventSourceMappings = config.EventSourceMappings
          Permissions = config.Permissions
          RolePolicyStatements = config.RolePolicyStatements
          AsyncInvokeOptions = config.AsyncInvokeOptions
          Function = None }

    // Custom operations for primitive values
    /// <summary>Sets the construct ID for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     constructId "MyFunctionConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: FunctionConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the handler for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="handler">The handler name (e.g., "index.handler").</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     handler "index.handler"
    /// }
    /// </code>
    [<CustomOperation("handler")>]
    member _.Handler(config: FunctionConfig, handler: string) = { config with Handler = Some handler }

    /// <summary>Sets the runtime for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="runtime">The Lambda runtime.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     runtime Runtime.NODEJS_18_X
    /// }
    /// </code>
    [<CustomOperation("runtime")>]
    member _.Runtime(config: FunctionConfig, runtime: Runtime) = { config with Runtime = Some runtime }

    /// <summary>Sets the code source from a local asset.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="path">The path to the code asset.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     code "./lambda"
    /// }
    /// </code>
    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: string) =
        { config with
            CodePath = Some(Code.FromAsset(path)) }

    /// <summary>Sets the code source from a local asset with options.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="path">The path to the code asset.</param>
    /// <param name="options">Asset options.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     code "./lambda" assetOptions
    /// }
    /// </code>
    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: string, options: AssetOptions) =
        { config with
            CodePath = Some(Code.FromAsset(path, options)) }

    /// <summary>Sets the code source from a Code object.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="path">The Code object.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     code (Code.FromBucket myBucket "lambda.zip")
    /// }
    /// </code>
    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: Code) = { config with CodePath = Some path }

    /// <summary>Sets the code source from a Docker image.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="directory">The directory containing the Dockerfile.</param>
    /// <param name="cmd">Optional CMD for the Docker image.</param>
    /// <param name="entrypoint">Optional ENTRYPOINT for the Docker image.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     dockerImageCode "./docker"
    /// }
    /// </code>
    [<CustomOperation("dockerImageCode")>]
    member _.DockerImageCode(config: FunctionConfig, directory: string, ?cmd: string[], ?entrypoint: string[]) =
        let props = AssetImageCodeProps()
        cmd |> Option.iter (fun c -> props.Cmd <- c)
        entrypoint |> Option.iter (fun e -> props.Entrypoint <- e)

        { config with
            CodePath = Some(Code.FromAssetImage(directory, props)) }

    /// <summary>Sets inline code for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="code">The inline code string.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     inlineCode "exports.handler = async () => 'Hello World';"
    /// }
    /// </code>
    [<CustomOperation("inlineCode")>]
    member _.InlineCode(config: FunctionConfig, code: string) =
        { config with
            CodePath = Some(Code.FromInline(code)) }

    /// <summary>Sets environment variables for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="env">List of key-value pairs for environment variables.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     environment [ "KEY1", "value1"; "KEY2", "value2" ]
    /// }
    /// </code>
    [<CustomOperation("environment")>]
    member _.Environment(config: FunctionConfig, env: (string * string) list) = { config with Environment = env }

    /// <summary>Adds a single environment variable.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="key">The environment variable key.</param>
    /// <param name="value">The environment variable value.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     envVar "API_KEY" "example-secret"
    ///     envVar "REGION" "us-east-1"
    /// }
    /// </code>
    [<CustomOperation("envVar")>]
    member _.EnvVar(config: FunctionConfig, key: string, value: string) =
        { config with
            Environment = Seq.append config.Environment [ (key, value) ] }

    /// <summary>Sets the timeout for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="seconds">The timeout in seconds.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     timeout 30.0
    /// }
    /// </code>
    [<CustomOperation("timeout")>]
    member _.Timeout(config: FunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    /// <summary>Sets the memory allocation for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="mb">The memory size in megabytes.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     memory 512
    /// }
    /// </code>
    [<CustomOperation("memorySize")>]
    member _.MemorySize(config: FunctionConfig, mb: int) = { config with MemorySize = Some mb }

    /// <summary>Sets the description for the Lambda function.</summary>
    /// <param name="config">The function configuration.</param>
    /// <param name="desc">The function description.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     description "Processes incoming orders"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: FunctionConfig, desc: string) = { config with Description = Some desc }

    [<CustomOperation("events")>]
    member _.Events(config: FunctionConfig, eventSource: IEventSource list) =
        { config with
            Events = eventSource @ config.Events }

    [<CustomOperation("event")>]
    member _.Event(config: FunctionConfig, eventSource: IEventSource) =
        { config with
            Events = eventSource :: config.Events }

    [<CustomOperation("addUrlOptions")>]
    member _.AddUrlOptions(config: FunctionConfig, options: IFunctionUrlOptions list) =
        { config with
            FunctionUrlOptions = options }

    [<CustomOperation("addUrlOption")>]
    member _.AddUrlOption(config: FunctionConfig, options: IFunctionUrlOptions) =
        { config with
            FunctionUrlOptions = options :: config.FunctionUrlOptions }

    [<CustomOperation("addEventSources")>]
    member _.AddEventSources(config: FunctionConfig, eventSource: IEventSource list) =
        { config with
            EventSource = eventSource }

    [<CustomOperation("addEventSource")>]
    member _.AddEventSource(config: FunctionConfig, eventSource: IEventSource) =
        { config with
            EventSource = eventSource :: config.EventSource }

    [<CustomOperation("addEventSourceMappings")>]
    member _.AddEventSourceMappings
        (
            config: FunctionConfig,
            eventSourceMapping: (string * IEventSourceMappingOptions) list
        ) =
        { config with
            EventSourceMappings = eventSourceMapping }

    [<CustomOperation("addEventSourceMapping")>]
    member _.AddEventSourceMapping(config: FunctionConfig, eventSourceMapping: string * IEventSourceMappingOptions) =
        { config with
            EventSourceMappings = eventSourceMapping :: config.EventSourceMappings }

    [<CustomOperation("addPermissions")>]
    member _.AddPermissions(config: FunctionConfig, permissions: IPermission list) =
        { config with
            Permissions = permissions }

    [<CustomOperation("addPermission")>]
    member _.AddPermission(config: FunctionConfig, permissions: IPermission) =
        { config with
            Permissions = permissions :: config.Permissions }

    [<CustomOperation("addRolePolicyStatements")>]
    member _.AddRolePolicyStatements(config: FunctionConfig, statements: PolicyStatement list) =
        { config with
            RolePolicyStatements = statements }

    [<CustomOperation("addRolePolicyStatement")>]
    member _.AddRolePolicyStatement(config: FunctionConfig, statements: PolicyStatement) =
        { config with
            RolePolicyStatements = statements :: config.RolePolicyStatements }

    [<CustomOperation("asyncInvokeOptions")>]
    member _.AsyncInvokeOptions(config: FunctionConfig, options: IEventInvokeConfigOptions list) =
        { config with
            AsyncInvokeOptions = options }

    [<CustomOperation("asyncInvokeOption")>]
    member _.AsyncInvokeOption(config: FunctionConfig, options: IEventInvokeConfigOptions) =
        { config with
            AsyncInvokeOptions = options :: config.AsyncInvokeOptions }

    [<CustomOperation("fileSystem")>]
    member _.FileSystem(config: FunctionConfig, fileSystem: FileSystem) =
        { config with
            FileSystem = Some fileSystem }

    [<CustomOperation("reservedConcurrentExecutions")>]
    member _.ReservedConcurrentExecutions(config: FunctionConfig, value: int) =
        { config with
            ReservedConcurrentExecutions = Some value }

    [<CustomOperation("insightsVersion")>]
    member _.InsightsVersion(config: FunctionConfig, version: LambdaInsightsVersion) =
        { config with
            InsightsVersion = Some version }

    [<CustomOperation("layer")>]
    member _.Layer(config: FunctionConfig, layer: ILayerVersion) =
        { config with
            Layers = layer :: config.Layers }

    [<CustomOperation("layers")>]
    member _.Layers(config: FunctionConfig, layers: ILayerVersion list) =
        { config with
            Layers = layers @ config.Layers }

    [<CustomOperation("architecture")>]
    member _.Architecture(config: FunctionConfig, arch: Architecture) =
        { config with Architecture = Some arch }

    [<CustomOperation("tracing")>]
    member _.Tracing(config: FunctionConfig, tracing: Tracing) = { config with Tracing = Some tracing }

    /// Add groups to securityGroups
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: FunctionConfig, sgs: ISecurityGroup list) =
        { config with
            SecurityGroups = sgs @ config.SecurityGroups }

    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: FunctionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    [<CustomOperation("loggingFormat")>]
    member _.LoggingFormat(config: FunctionConfig, format: LoggingFormat) =
        { config with
            LoggingFormat = Some format }

    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: FunctionConfig, logGroup: ILogGroup) =
        { config with LogGroup = Some logGroup }

    [<CustomOperation("maxEventAge")>]
    member _.MaxEventAge(config: FunctionConfig, age: Duration) = { config with MaxEventAge = Some age }

    [<CustomOperation("retryAttempts")>]
    member _.RetryAttempts(config: FunctionConfig, value: int) =
        { config with
            RetryAttempts = Some value }

    [<CustomOperation("deadLetterQueueEnabled")>]
    member _.DeadLetterQueueEnabled(config: FunctionConfig, value: bool) =
        { config with
            DeadLetterQueueEnabled = Some value }

    /// <summary>
    /// Controls automatic DLQ creation. Default: true (Yan Cui recommendation).
    /// Set to false to disable auto-DLQ creation.
    /// </summary>
    [<CustomOperation("autoCreateDLQ")>]
    member _.AutoCreateDLQ(config: FunctionConfig, value: bool) =
        { config with
            AutoCreateDLQ = Some value }

    /// <summary>
    /// Controls automatic Lambda Powertools layer addition. Default: true (Yan Cui recommendation).
    /// Set to false to disable Powertools auto-addition.
    /// </summary>
    [<CustomOperation("autoAddPowertools")>]
    member _.AutoAddPowertools(config: FunctionConfig, value: bool) =
        { config with
            AutoAddPowertools = Some value }

    [<CustomOperation("environmentEncryption")>]
    member _.EnvironmentEncryption(config: FunctionConfig, key: IKey) =
        { config with
            EnvironmentEncryption = Some key }

    /// <summary>
    /// Sets the ephemeral storage size for the Lambda function in MB.
    /// Default: 512 MB (free tier). Valid range: 512-10240 MB.
    /// Cost optimization: Only increase if needed for /tmp storage.
    /// </summary>
    /// <param name="sizeInMB">Storage size in megabytes (512-10240).</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     ephemeralStorageSize 1024
    /// }
    /// </code>
    [<CustomOperation("ephemeralStorageSize")>]
    member _.EphemeralStorageSize(config: FunctionConfig, sizeInMB: int) =
        { config with
            EphemeralStorageSize = Some sizeInMB }

    [<CustomOperation("xrayEnabled")>]
    member _.XRayEnabled(config: FunctionConfig) =
        { config with
            Tracing = Some Tracing.ACTIVE }

    [<CustomOperation("role")>]
    member _.Role(config: FunctionConfig, role: IRole) = { config with Role = Some role }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: FunctionConfig, subnets: ISubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    [<CustomOperation("currentVersionOptions")>]
    member _.CurrentVersionOptions(config: FunctionConfig, options: IVersionOptions) =
        { config with
            CurrentVersionOptions = Some options }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module FunctionBuilders =
    /// <summary>Creates a Lambda function configuration.</summary>
    /// <param name="name">The function name.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     handler "index.handler"
    ///     runtime Runtime.NODEJS_18_X
    ///     code "./lambda"
    ///     timeout 30.0
    /// }
    /// </code>
    let lambda (name: string) = FunctionBuilder(name)
