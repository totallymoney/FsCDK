namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.S3.Assets
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.KMS
open System.Collections.Generic

// ============================================================================
// Lambda Function Configuration DSL
// ============================================================================

// Lambda configuration DSL
type FunctionConfig =
    { FunctionName: string
      ConstructId: string option // Optional custom construct ID
      Handler: string option
      Runtime: Runtime option
      CodePath: Code option
      Environment: (string * string) seq
      Timeout: float option
      Memory: int option
      Description: string option
      EventSources: IEventSource list
      EventSourceMappings: (string * IEventSourceMappingOptions) list
      FunctionUrlOptions: IFunctionUrlOptions option
      Permissions: (string * IPermission) list
      RolePolicyStatements: PolicyStatement list
      AsyncInvokeOptions: IEventInvokeConfigOptions option
      ReservedConcurrentExecutions: int option
      LogGroup: ILogGroup option
      Role: IRole option
      InsightsVersion: LambdaInsightsVersion option
      CurrentVersionOptions: VersionOptions option
      Layers: ILayerVersion list
      Architecture: Architecture option
      Tracing: Tracing option
      VpcSubnets: SubnetSelection option
      SecurityGroups: SecurityGroupRef list
      FileSystem: FileSystem option
      DeadLetterQueue: IQueue option
      DeadLetterQueueEnabled: bool option
      LoggingFormat: LoggingFormat option
      MaxEventAge: Duration option
      RetryAttempts: int option
      EnvironmentEncryption: KMSKeyRef option }

type FunctionSpec =
    { FunctionName: string
      ConstructId: string
      Props: IFunctionProps
      Actions: (Function -> unit) list
      EventSources: ResizeArray<IEventSource>
      mutable Function: IFunction option }

type FunctionBuilder(name: string) =
    member _.Yield(spec: EventInvokeConfigSpec) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = Some spec.Options
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(spec: FunctionUrlSpec) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = Some spec.Options
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(stmt: PolicyStatement) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = [ stmt ]
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(event: IEventSource) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = [ event ]
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(spec: PermissionSpec) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = [ (spec.Id, spec.Permission) ]
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(spec: EventSourceMappingSpec) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = [ (spec.Id, spec.Options) ]
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield _ : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Zero() : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

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
          Memory =
            if state1.Memory.IsSome then
                state1.Memory
            else
                state2.Memory
          Description =
            if state1.Description.IsSome then
                state1.Description
            else
                state2.Description
          EventSources = state1.EventSources @ state2.EventSources
          EventSourceMappings = state1.EventSourceMappings @ state2.EventSourceMappings
          FunctionUrlOptions =
            if state1.FunctionUrlOptions.IsSome then
                state1.FunctionUrlOptions
            else
                state2.FunctionUrlOptions
          Permissions = state1.Permissions @ state2.Permissions
          RolePolicyStatements = state1.RolePolicyStatements @ state2.RolePolicyStatements
          AsyncInvokeOptions =
            if state1.AsyncInvokeOptions.IsSome then
                state1.AsyncInvokeOptions
            else
                state2.AsyncInvokeOptions
          ReservedConcurrentExecutions =
            if state1.ReservedConcurrentExecutions.IsSome then
                state1.ReservedConcurrentExecutions
            else
                state2.ReservedConcurrentExecutions
          LogGroup =
            if state1.LogGroup.IsSome then
                state1.LogGroup
            else
                state2.LogGroup
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
            if state1.DeadLetterQueue.IsSome then
                state1.DeadLetterQueue
            else
                state2.DeadLetterQueue
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
            if state1.EnvironmentEncryption.IsSome then
                state1.EnvironmentEncryption
            else
                state2.EnvironmentEncryption }

    member _.Run(config: FunctionConfig) : FunctionSpec =
        let props = FunctionProps()

        // Determine the construct ID
        let constructId = config.ConstructId |> Option.defaultValue config.FunctionName

        // Required properties - fail fast if missing
        props.Handler <-
            match config.Handler with
            | Some h -> h
            | None -> failwith "Lambda handler is required"

        props.Runtime <-
            match config.Runtime with
            | Some r -> r
            | None -> failwith "Lambda runtime is required"

        props.Code <-
            match config.CodePath with
            | Some c -> c
            | None -> failwith "Lambda code path is required"

        // Optional properties
        if not (Seq.isEmpty config.Environment) then
            let envDict = Dictionary<string, string>()

            for key, value in config.Environment do
                envDict.Add(key, value)

            props.Environment <- envDict

        config.Timeout |> Option.iter (fun t -> props.Timeout <- Duration.Seconds(t))
        config.Memory |> Option.iter (fun m -> props.MemorySize <- m)
        config.Description |> Option.iter (fun d -> props.Description <- d)

        config.ReservedConcurrentExecutions
        |> Option.iter (fun r -> props.ReservedConcurrentExecutions <- r)

        config.LogGroup |> Option.iter (fun lg -> props.LogGroup <- lg)
        config.Role |> Option.iter (fun r -> props.Role <- r)
        config.InsightsVersion |> Option.iter (fun v -> props.InsightsVersion <- v)

        config.CurrentVersionOptions
        |> Option.iter (fun v -> props.CurrentVersionOptions <- v)

        if not (List.isEmpty config.Layers) then
            props.Layers <- config.Layers |> List.toArray

        config.Architecture |> Option.iter (fun a -> props.Architecture <- a)
        config.Tracing |> Option.iter (fun t -> props.Tracing <- t)
        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <-
                config.SecurityGroups
                |> List.map VpcHelpers.resolveSecurityGroupRef
                |> Array.ofList

        config.FileSystem |> Option.iter (fun fs -> props.Filesystem <- fs)
        config.DeadLetterQueue |> Option.iter (fun dlq -> props.DeadLetterQueue <- dlq)

        config.DeadLetterQueueEnabled
        |> Option.iter (fun e -> props.DeadLetterQueueEnabled <- e)

        config.LoggingFormat |> Option.iter (fun f -> props.LoggingFormat <- f)
        config.MaxEventAge |> Option.iter (fun age -> props.MaxEventAge <- age)
        config.RetryAttempts |> Option.iter (fun r -> props.RetryAttempts <- r)

        config.EnvironmentEncryption
        |> Option.iter (fun v ->
            props.EnvironmentEncryption <-
                match v with
                | KMSKeyRef.KMSKeyInterface i -> i
                | KMSKeyRef.KMSKeySpecRef pr ->
                    match pr.Key with
                    | Some k -> k
                    | None -> failwith $"Key {pr.KeyName} has to be resolved first")

        // Actions to perform on the Function after creation
        let actions =
            [
              // Add event sources
              for source in config.EventSources do
                  fun (fn: Function) -> fn.AddEventSource(source)

              // Add event source mappings
              for id, options in config.EventSourceMappings do
                  fun (fn: Function) -> fn.AddEventSourceMapping(id, options) |> ignore

              // Add function URL if configured
              match config.FunctionUrlOptions with
              | Some opts -> fun (fn: Function) -> fn.AddFunctionUrl(opts) |> ignore
              | None -> fun _ -> ()

              // Add permissions
              for id, permission in config.Permissions do
                  fun (fn: Function) -> fn.AddPermission(id, permission)

              // Add policy statements to the role
              for stmt in config.RolePolicyStatements do
                  fun (fn: Function) -> fn.AddToRolePolicy(stmt)

              // Configure async invoke if specified
              match config.AsyncInvokeOptions with
              | Some opts -> fun (fn: Function) -> fn.ConfigureAsyncInvoke(opts)
              | None -> fun _ -> () ]

        { FunctionName = config.FunctionName
          ConstructId = constructId
          Props = props
          Actions = actions
          EventSources = ResizeArray()
          Function = None }

    // Custom operations for primitive values
    /// <summary>Sets the construct ID for the Lambda function.</summary>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     constructId "MyFunctionConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: FunctionConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the handler for the Lambda function.</summary>
    /// <param name="handler">The handler name (e.g., "index.handler").</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     handler "index.handler"
    /// }
    /// </code>
    [<CustomOperation("handler")>]
    member _.Handler(config: FunctionConfig, handler: string) = { config with Handler = Some handler }

    /// <summary>Sets the runtime for the Lambda function.</summary>
    /// <param name="runtime">The Lambda runtime.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     runtime Runtime.NODEJS_18_X
    /// }
    /// </code>
    [<CustomOperation("runtime")>]
    member _.Runtime(config: FunctionConfig, runtime: Runtime) = { config with Runtime = Some runtime }

    /// <summary>Sets the code source from a local asset.</summary>
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
    /// <param name="path">The Code object.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     code (Code.FromBucket myBucket "lambda.zip")
    /// }
    /// </code>
    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: Code) = { config with CodePath = Some path }

    /// <summary>Sets the code source from a Docker image.</summary>
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
    /// <param name="env">List of key-value pairs for environment variables.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     environment [ "KEY1", "value1"; "KEY2", "value2" ]
    /// }
    /// </code>
    [<CustomOperation("environment")>]
    member _.Environment(config: FunctionConfig, env: (string * string) list) = { config with Environment = env }

    /// <summary>Adds a single environment variable.</summary>
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
    /// <param name="seconds">The timeout in seconds.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     timeout 30.0
    /// }
    /// </code>
    [<CustomOperation("timeout")>]
    member _.Timeout(config: FunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    /// <summary>Sets the memory allocation for the Lambda function.</summary>
    /// <param name="mb">The memory size in megabytes.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     memory 512
    /// }
    /// </code>
    [<CustomOperation("memory")>]
    member _.Memory(config: FunctionConfig, mb: int) = { config with Memory = Some mb }

    /// <summary>Sets the description for the Lambda function.</summary>
    /// <param name="desc">The function description.</param>
    /// <code lang="fsharp">
    /// lambda "MyFunction" {
    ///     description "Processes incoming orders"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: FunctionConfig, desc: string) = { config with Description = Some desc }

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
        let sgsrefs = sgs |> List.map SecurityGroupRef.SecurityGroupInterface

        { config with
            SecurityGroups = sgsrefs @ config.SecurityGroups }

    /// Add groups to securityGroups
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: FunctionConfig, sgs: SecurityGroupSpec list) =
        let sgsrefs = sgs |> List.map SecurityGroupRef.SecurityGroupSpecRef

        { config with
            SecurityGroups = sgsrefs @ config.SecurityGroups }

    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: FunctionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    [<CustomOperation("loggingFormat")>]
    member _.LoggingFormat(config: FunctionConfig, format: LoggingFormat) =
        { config with
            LoggingFormat = Some format }

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

    [<CustomOperation("environmentEncryption")>]
    member _.EnvironmentEncryption(config: FunctionConfig, key: IKey) =
        { config with
            EnvironmentEncryption = Some(KMSKeyRef.KMSKeyInterface key) }

    [<CustomOperation("environmentEncryption")>]
    member _.EnvironmentEncryption(config: FunctionConfig, key: KMSKeySpec) =
        { config with
            EnvironmentEncryption = Some(KMSKeyRef.KMSKeySpecRef key) }

    [<CustomOperation("xrayEnabled")>]
    member _.XRayEnabled(config: FunctionConfig) =
        { config with
            Tracing = Some Tracing.ACTIVE }

    [<CustomOperation("role")>]
    member _.Role(config: FunctionConfig, role: IRole) = { config with Role = Some role }

    // Implicit yields for complex types
    member _.Yield(logGroup: ILogGroup) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = Some logGroup
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(role: IRole) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = Some role
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(versionOptions: VersionOptions) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = Some versionOptions
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(vpcSubnets: SubnetSelection) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = Some vpcSubnets
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

    member _.Yield(fileSystem: FileSystem) : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None
          EventSources = []
          EventSourceMappings = []
          FunctionUrlOptions = None
          Permissions = []
          RolePolicyStatements = []
          AsyncInvokeOptions = None
          ReservedConcurrentExecutions = None
          LogGroup = None
          Role = None
          InsightsVersion = None
          CurrentVersionOptions = None
          Layers = []
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = Some fileSystem
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None
          EnvironmentEncryption = None }

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
