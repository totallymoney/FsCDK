namespace FsCDK

open System
open Amazon.CDK
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.KMS
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.EFS
open Amazon.CDK.AWS.S3.Assets
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.SQS
open System
open System.Collections.Generic

type PermissionSpec = { Id: string; Permission: IPermission }

type EventSourcesSpec = { Sources: IEventSource list }

type EventSourceMappingSpec =
    { Id: string
      Options: IEventSourceMappingOptions }

type EventInvokeConfigSpec = { Options: IEventInvokeConfigOptions }

type FunctionUrlSpec = { Options: IFunctionUrlOptions }

type FunctionUrlCorsSpec = { Options: IFunctionUrlCorsOptions }

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
      SecurityGroups: ISecurityGroup list
      FileSystem: Amazon.CDK.AWS.Lambda.FileSystem option
      DeadLetterQueue: IQueue option
      DeadLetterQueueEnabled: bool option
      LoggingFormat: LoggingFormat option
      MaxEventAge: Duration option
      RetryAttempts: int option }

type FunctionSpec =
    { FunctionName: string
      ConstructId: string // Construct ID for CDK
      Props: FunctionProps
      Actions: (Function -> unit) list }

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
          RetryAttempts = None }

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
          RetryAttempts = None }

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
          RetryAttempts = None }

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
          RetryAttempts = None }

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
          RetryAttempts = None }

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
          RetryAttempts = None }

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
          Environment = List.ofSeq (Seq.append state1.Environment state2.Environment)
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
                state2.RetryAttempts }

    member inline _.Delay([<InlineIfLambda>] f: unit -> FunctionConfig) : FunctionConfig = f ()

    member inline x.For(config: FunctionConfig, [<InlineIfLambda>] f: unit -> FunctionConfig) : FunctionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

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
          RetryAttempts = None }

    member _.Run(config: FunctionConfig) : FunctionSpec =
        // Function name is required

        // Construct ID defaults to the function name if not specified
        let constructId = config.ConstructId |> Option.defaultValue config.FunctionName

        let props = FunctionProps(FunctionName = config.FunctionName)

        // Required properties
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
            | Some path -> path
            | None -> failwith "Lambda code path is required"

        // Environment variables
        if not (Seq.isEmpty config.Environment) then
            let envDict = Dictionary<string, string>()
            config.Environment |> Seq.iter envDict.Add
            props.Environment <- envDict

        // Optional properties - only set if explicitly configured
        config.Timeout |> Option.iter (fun t -> props.Timeout <- Duration.Seconds(t))
        config.Memory |> Option.iter (fun m -> props.MemorySize <- m)
        config.Description |> Option.iter (fun desc -> props.Description <- desc)

        config.ReservedConcurrentExecutions
        |> Option.iter (fun r -> props.ReservedConcurrentExecutions <- r)

        config.LogGroup |> Option.iter (fun f -> props.LogGroup <- f)
        config.Role |> Option.iter (fun r -> props.Role <- r)
        config.InsightsVersion |> Option.iter (fun v -> props.InsightsVersion <- v)

        config.CurrentVersionOptions
        |> Option.iter (fun o -> props.CurrentVersionOptions <- o)

        if not (List.isEmpty config.Layers) then
            props.Layers <- config.Layers |> List.toArray

        config.Architecture |> Option.iter (fun a -> props.Architecture <- a)
        config.Tracing |> Option.iter (fun t -> props.Tracing <- t)
        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.FileSystem |> Option.iter (fun fs -> props.Filesystem <- fs)
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)

        config.DeadLetterQueueEnabled
        |> Option.iter (fun e -> props.DeadLetterQueueEnabled <- Nullable<bool>(e))

        config.LoggingFormat |> Option.iter (fun f -> props.LoggingFormat <- f)
        config.MaxEventAge |> Option.iter (fun d -> props.MaxEventAge <- d)
        config.RetryAttempts |> Option.iter (fun r -> props.RetryAttempts <- r)

        // Build post-creation actions based on config
        let actions = ResizeArray<Function -> unit>()

        // AddEventSource
        for src in config.EventSources do
            actions.Add(fun (fn: Function) -> fn.AddEventSource(src))

        // AddEventSourceMapping
        for id, opts in config.EventSourceMappings do
            actions.Add(fun (fn: Function) -> fn.AddEventSourceMapping(id, opts) |> ignore)

        // AddFunctionUrl
        config.FunctionUrlOptions
        |> Option.iter (fun opts -> actions.Add(fun (fn: Function) -> fn.AddFunctionUrl(opts) |> ignore))

        // AddPermission
        for id, perm in config.Permissions do
            actions.Add(fun (fn: Function) -> fn.AddPermission(id, perm))

        // AddToRolePolicy
        for stmt in config.RolePolicyStatements do
            actions.Add(fun (fn: Function) -> fn.AddToRolePolicy(stmt))

        // ConfigureAsyncInvoke
        config.AsyncInvokeOptions
        |> Option.iter (fun opts -> actions.Add(fun (fn: Function) -> fn.ConfigureAsyncInvoke(opts)))

        { FunctionName = config.FunctionName
          ConstructId = constructId
          Props = props
          Actions = List.ofSeq actions }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: FunctionConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("handler")>]
    member _.Handler(config: FunctionConfig, value: string) = { config with Handler = Some value }

    [<CustomOperation("runtime")>]
    member _.Runtime(config: FunctionConfig, value: Runtime) = { config with Runtime = Some value }

    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: string) =
        { config with
            CodePath = Some(Code.FromAsset(path)) }

    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: string, options: AssetOptions) =
        { config with
            CodePath = Some(Code.FromAsset(path, options)) }


    [<CustomOperation("code")>]
    member _.Code(config: FunctionConfig, path: Code) = { config with CodePath = Some path }

    [<CustomOperation("environment")>]
    member _.Environment(config: FunctionConfig, vars: (string * string) seq) = { config with Environment = vars }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: FunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    [<CustomOperation("memory")>]
    member _.Memory(config: FunctionConfig, mb: int) = { config with Memory = Some mb }

    [<CustomOperation("description")>]
    member _.Description(config: FunctionConfig, desc: string) = { config with Description = Some desc }

    // Custom operations for enums and primitives
    [<CustomOperation("architecture")>]
    member _.Architecture(config: FunctionConfig, value: Architecture) =
        { config with
            Architecture = Some value }

    [<CustomOperation("tracing")>]
    member _.Tracing(config: FunctionConfig, value: Tracing) = { config with Tracing = Some value }

    [<CustomOperation("loggingFormat")>]
    member _.LoggingFormat(config: FunctionConfig, value: LoggingFormat) =
        { config with
            LoggingFormat = Some value }

    [<CustomOperation("reservedConcurrentExecutions")>]
    member _.ReservedConcurrentExecutions(config: FunctionConfig, value: int) =
        { config with
            ReservedConcurrentExecutions = Some value }

    member _.Yield(layer: ILayerVersion) : FunctionConfig =
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
          Layers = [ layer ]
          Architecture = None
          Tracing = None
          VpcSubnets = None
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None }

    member _.Yield(sg: ISecurityGroup) : FunctionConfig =
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
          SecurityGroups = [ sg ]
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None }

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
          RetryAttempts = None }

    member _.Yield(fs: Amazon.CDK.AWS.Lambda.FileSystem) : FunctionConfig =
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
          FileSystem = Some fs
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None }

    member _.Yield(q: IQueue) : FunctionConfig =
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
          DeadLetterQueue = Some q
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None }

    member _.Yield(vpcSel: SubnetSelection) : FunctionConfig =
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
          VpcSubnets = Some vpcSel
          SecurityGroups = []
          FileSystem = None
          DeadLetterQueue = None
          DeadLetterQueueEnabled = None
          LoggingFormat = None
          MaxEventAge = None
          RetryAttempts = None }

    member _.Yield(verOpts: VersionOptions) : FunctionConfig =
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
          CurrentVersionOptions = Some verOpts
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
          RetryAttempts = None }

    [<CustomOperation("insightsVersion")>]
    member _.InsightsVersion(config: FunctionConfig, value: LambdaInsightsVersion) =
        { config with
            InsightsVersion = Some value }

    [<CustomOperation("maxEventAge")>]
    member _.MaxEventAge(config: FunctionConfig, value: Duration) =
        { config with MaxEventAge = Some value }

    [<CustomOperation("retryAttempts")>]
    member _.RetryAttempts(config: FunctionConfig, value: int) =
        { config with
            RetryAttempts = Some value }

    [<CustomOperation("deadLetterQueueEnabled")>]
    member _.DeadLetterQueueEnabled(config: FunctionConfig, value: bool) =
        { config with
            DeadLetterQueueEnabled = Some value }

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
          RetryAttempts = None }


// ============================================================================
// EC2 SubnetSelection Builder DSL (complex type helper)
// ============================================================================

type SubnetSelectionConfig =
    { SubnetType: SubnetType option
      AvailabilityZones: string list option }

type SubnetSelectionBuilder() =
    member _.Yield _ : SubnetSelectionConfig =
        { SubnetType = None
          AvailabilityZones = None }

    member _.Zero() : SubnetSelectionConfig =
        { SubnetType = None
          AvailabilityZones = None }

    member _.Combine(a: SubnetSelectionConfig, b: SubnetSelectionConfig) : SubnetSelectionConfig =
        { SubnetType = (if a.SubnetType.IsSome then a.SubnetType else b.SubnetType)
          AvailabilityZones =
            (if a.AvailabilityZones.IsSome then
                 a.AvailabilityZones
             else
                 b.AvailabilityZones) }

    member inline _.Delay(f: unit -> SubnetSelectionConfig) = f ()
    member inline x.For(state: SubnetSelectionConfig, f: unit -> SubnetSelectionConfig) = x.Combine(state, f ())

    member _.Run(cfg: SubnetSelectionConfig) : SubnetSelection =
        let s = SubnetSelection()
        cfg.SubnetType |> Option.iter (fun t -> s.SubnetType <- t)

        cfg.AvailabilityZones
        |> Option.iter (fun az -> s.AvailabilityZones <- (az |> List.toArray))

        s

    [<CustomOperation("subnetType")>]
    member _.SubnetType(cfg: SubnetSelectionConfig, value: SubnetType) = { cfg with SubnetType = Some value }

    [<CustomOperation("availabilityZones")>]
    member _.AvailabilityZones(cfg: SubnetSelectionConfig, azs: string list) =
        { cfg with
            AvailabilityZones = Some azs }

// ============================================================================
// Lambda VersionOptions Builder DSL (complex type helper)
// Use inline implicit yield in lambda CE: versionOptions { ... }
// ============================================================================

type VersionOptionsConfig =
    { Description: string option
      RemovalPolicy: RemovalPolicy option
      CodeSha256: string option }

type VersionOptionsBuilder() =
    member _.Yield _ : VersionOptionsConfig =
        { Description = None
          RemovalPolicy = None
          CodeSha256 = None }

    member _.Zero() : VersionOptionsConfig =
        { Description = None
          RemovalPolicy = None
          CodeSha256 = None }

    member _.Combine(a: VersionOptionsConfig, b: VersionOptionsConfig) : VersionOptionsConfig =
        { Description =
            if a.Description.IsSome then
                a.Description
            else
                b.Description
          RemovalPolicy =
            if a.RemovalPolicy.IsSome then
                a.RemovalPolicy
            else
                b.RemovalPolicy
          CodeSha256 = if a.CodeSha256.IsSome then a.CodeSha256 else b.CodeSha256 }

    member inline _.Delay(f: unit -> VersionOptionsConfig) = f ()
    member inline x.For(state: VersionOptionsConfig, f: unit -> VersionOptionsConfig) = x.Combine(state, f ())

    member _.Run(cfg: VersionOptionsConfig) : VersionOptions =
        let o = VersionOptions()
        cfg.Description |> Option.iter (fun d -> o.Description <- d)
        cfg.RemovalPolicy |> Option.iter (fun rp -> o.RemovalPolicy <- rp)
        cfg.CodeSha256 |> Option.iter (fun s -> o.CodeSha256 <- s)
        o

    [<CustomOperation("description")>]
    member _.Desc(cfg: VersionOptionsConfig, d: string) = { cfg with Description = Some d }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(cfg: VersionOptionsConfig, rp: RemovalPolicy) = { cfg with RemovalPolicy = Some rp }

    [<CustomOperation("codeSha256")>]
    member _.CodeSha256(cfg: VersionOptionsConfig, sha: string) = { cfg with CodeSha256 = Some sha }

// ============================================================================
// Lambda FileSystem Builder DSL (complex type helper)
// ============================================================================

type LambdaFileSystemConfig =
    { AccessPoint: IAccessPoint option
      LocalMountPath: string option }

type LambdaFileSystemBuilder() =
    member _.Yield _ : LambdaFileSystemConfig =
        { AccessPoint = None
          LocalMountPath = None }

    member _.Zero() : LambdaFileSystemConfig =
        { AccessPoint = None
          LocalMountPath = None }

    member _.Combine(a: LambdaFileSystemConfig, b: LambdaFileSystemConfig) : LambdaFileSystemConfig =
        { AccessPoint =
            if a.AccessPoint.IsSome then
                a.AccessPoint
            else
                b.AccessPoint
          LocalMountPath =
            if a.LocalMountPath.IsSome then
                a.LocalMountPath
            else
                b.LocalMountPath }

    member inline _.Delay(f: unit -> LambdaFileSystemConfig) = f ()
    member inline x.For(state: LambdaFileSystemConfig, f: unit -> LambdaFileSystemConfig) = x.Combine(state, f ())

    member _.Run(cfg: LambdaFileSystemConfig) : Amazon.CDK.AWS.Lambda.FileSystem =
        match cfg.AccessPoint, cfg.LocalMountPath with
        | Some ap, Some path -> Amazon.CDK.AWS.Lambda.FileSystem.FromEfsAccessPoint(ap, path)
        | _ -> failwith "Both accessPoint and localMountPath are required for Lambda FileSystem"

    [<CustomOperation("localMountPath")>]
    member _.LocalMountPath(cfg: LambdaFileSystemConfig, path: string) = { cfg with LocalMountPath = Some path }

    // Complex type as implicit yield
    member _.Yield(ap: IAccessPoint) : LambdaFileSystemConfig =
        { AccessPoint = Some ap
          LocalMountPath = None }

// ============================================================================
// EFS FileSystem Builder DSL
// ============================================================================

type AccessPointConfig =
    { Stack: Stack
      Id: string
      Props: AccessPointProps }

type EfsFileSystemConfig =
    { Stack: Stack option
      Id: string
      Vpc: IVpc option
      RemovalPolicy: RemovalPolicy option
      Encrypted: bool option
      KmsKey: IKey option
      PerformanceMode: PerformanceMode option
      ThroughputMode: ThroughputMode option
      ProvisionedThroughputPerSecond: Size option
      SecurityGroup: ISecurityGroup option }

type EfsFileSystemBuilder(id: string) =
    member _.Yield _ : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Zero() : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Run(config: EfsFileSystemConfig) : IFileSystem =
        let stack =
            match config.Stack with
            | Some s -> s
            | None -> failwith "Stack is required for EFS FileSystem"

        let props = FileSystemProps()

        // Set VPC
        match config.Vpc with
        | Some vpc -> props.Vpc <- vpc
        | None -> failwith "Vpc is required for EFS FileSystem"

        // Optional properties
        config.RemovalPolicy |> Option.iter (fun p -> props.RemovalPolicy <- p)
        config.Encrypted |> Option.iter (fun e -> props.Encrypted <- e)
        config.KmsKey |> Option.iter (fun k -> props.KmsKey <- k)
        config.PerformanceMode |> Option.iter (fun m -> props.PerformanceMode <- m)
        config.ThroughputMode |> Option.iter (fun m -> props.ThroughputMode <- m)

        config.ProvisionedThroughputPerSecond
        |> Option.iter (fun t -> props.ProvisionedThroughputPerSecond <- t)

        config.SecurityGroup |> Option.iter (fun sg -> props.SecurityGroup <- sg)

        FileSystem(stack, config.Id, props)

    member _.Combine(a: EfsFileSystemConfig, b: EfsFileSystemConfig) : EfsFileSystemConfig =
        { Stack = if a.Stack.IsSome then a.Stack else b.Stack
          Id = a.Id
          Vpc = if a.Vpc.IsSome then a.Vpc else b.Vpc
          RemovalPolicy =
            if a.RemovalPolicy.IsSome then
                a.RemovalPolicy
            else
                b.RemovalPolicy
          Encrypted = if a.Encrypted.IsSome then a.Encrypted else b.Encrypted
          KmsKey = if a.KmsKey.IsSome then a.KmsKey else b.KmsKey
          PerformanceMode =
            if a.PerformanceMode.IsSome then
                a.PerformanceMode
            else
                b.PerformanceMode
          ThroughputMode =
            if a.ThroughputMode.IsSome then
                a.ThroughputMode
            else
                b.ThroughputMode
          ProvisionedThroughputPerSecond =
            if a.ProvisionedThroughputPerSecond.IsSome then
                a.ProvisionedThroughputPerSecond
            else
                b.ProvisionedThroughputPerSecond
          SecurityGroup =
            if a.SecurityGroup.IsSome then
                a.SecurityGroup
            else
                b.SecurityGroup }

    member inline _.Delay(f: unit -> EfsFileSystemConfig) = f ()
    member inline x.For(state: EfsFileSystemConfig, f: unit -> EfsFileSystemConfig) = x.Combine(state, f ())

    // Custom operations only for primitive values
    [<CustomOperation("encrypted")>]
    member _.Encrypted(config: EfsFileSystemConfig, value: bool) = { config with Encrypted = Some value }

    [<CustomOperation("performanceMode")>]
    member _.PerformanceMode(config: EfsFileSystemConfig, mode: PerformanceMode) =
        { config with
            PerformanceMode = Some mode }

    [<CustomOperation("throughputMode")>]
    member _.ThroughputMode(config: EfsFileSystemConfig, mode: ThroughputMode) =
        { config with
            ThroughputMode = Some mode }

    [<CustomOperation("provisionedThroughput")>]
    member _.ProvisionedThroughput(config: EfsFileSystemConfig, throughput: Size) =
        { config with
            ProvisionedThroughputPerSecond = Some throughput }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: EfsFileSystemConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    // Implicit yields for complex types
    member _.Yield(stack: Stack) : EfsFileSystemConfig =
        { Stack = Some stack
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(vpc: IVpc) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = Some vpc
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(key: IKey) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = Some key
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(sg: ISecurityGroup) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = Some sg }

type AccessPointBuilder(id: string) =
    member _.Yield _ : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps() }

    member _.Zero() : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps() }

    member _.Run(config: AccessPointConfig) : IAccessPoint =
        match isNull (box config.Stack), isNull (box config.Props.FileSystem) with
        | true, _ -> failwith "Stack is required for AccessPointBuilder"
        | _, true -> failwith "FileSystem is required for AccessPointBuilder"
        | _ -> AccessPoint(config.Stack, config.Id, config.Props)

    // Implicit yields for complex types
    member _.Yield(stack: Stack) : AccessPointConfig =
        { Stack = stack
          Id = id
          Props = AccessPointProps() }

    member _.Yield(fs: IFileSystem) : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps(FileSystem = fs) }

    member _.Combine(a: AccessPointConfig, b: AccessPointConfig) : AccessPointConfig =
        let stack = if isNull (box a.Stack) then b.Stack else a.Stack
        let props = AccessPointProps()

        if not (isNull (box a.Props.FileSystem)) then
            props.FileSystem <- a.Props.FileSystem
        elif not (isNull (box b.Props.FileSystem)) then
            props.FileSystem <- b.Props.FileSystem

        if not (isNull (box a.Props.Path)) then
            props.Path <- a.Props.Path
        elif not (isNull (box b.Props.Path)) then
            props.Path <- b.Props.Path

        if not (isNull (box a.Props.PosixUser)) then
            props.PosixUser <- a.Props.PosixUser
        elif not (isNull (box b.Props.PosixUser)) then
            props.PosixUser <- b.Props.PosixUser

        if not (isNull (box a.Props.CreateAcl)) then
            props.CreateAcl <- a.Props.CreateAcl
        elif not (isNull (box b.Props.CreateAcl)) then
            props.CreateAcl <- b.Props.CreateAcl

        { Stack = stack
          Id = id
          Props = props }

    member inline _.Delay(f: unit -> AccessPointConfig) = f ()
    member inline x.For(state: AccessPointConfig, f: unit -> AccessPointConfig) = x.Combine(state, f ())

    // Custom operations only for primitive values
    [<CustomOperation("path")>]
    member _.Path(config: AccessPointConfig, value: string) =
        config.Props.Path <- value
        config

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointConfig, uid: string, gid: string) =
        config.Props.PosixUser <- PosixUser(Uid = uid, Gid = gid)
        config

    [<CustomOperation("createAcl")>]
    member _.CreateAcl(config: AccessPointConfig, ownerGid: string, ownerUid: string, permissions: string) =
        config.Props.CreateAcl <- Acl(OwnerGid = ownerGid, OwnerUid = ownerUid, Permissions = permissions)
        config

// ============================================================================
// EFS AccessPointProps Builder DSL
// ============================================================================

type AccessPointPropsConfig =
    { FileSystem: IFileSystem
      Path: string option
      PosixUser: PosixUser option
      CreateAcl: Acl option }

type AccessPointPropsBuilder(fileSystem: IFileSystem) =
    member _.Yield _ : AccessPointPropsConfig =
        { FileSystem = fileSystem
          Path = None
          PosixUser = None
          CreateAcl = None }

    member _.Zero() : AccessPointPropsConfig =
        { FileSystem = fileSystem
          Path = None
          PosixUser = None
          CreateAcl = None }

    member _.Combine(a: AccessPointPropsConfig, b: AccessPointPropsConfig) : AccessPointPropsConfig =
        { FileSystem = a.FileSystem
          Path = Option.orElse a.Path b.Path
          PosixUser = Option.orElse a.PosixUser b.PosixUser
          CreateAcl = Option.orElse a.CreateAcl b.CreateAcl }

    member inline _.Delay(f: unit -> AccessPointPropsConfig) = f ()

    member _.Run(config: AccessPointPropsConfig) =
        let props = AccessPointProps(FileSystem = config.FileSystem)
        config.Path |> Option.iter (fun p -> props.Path <- p)
        config.PosixUser |> Option.iter (fun u -> props.PosixUser <- u)
        config.CreateAcl |> Option.iter (fun a -> props.CreateAcl <- a)
        props

    [<CustomOperation("path")>]
    member _.Path(config: AccessPointPropsConfig, value: string) = { config with Path = Some value }

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointPropsConfig, uid: string, gid: string) =
        let user = PosixUser(Gid = gid, Uid = uid)
        { config with PosixUser = Some user }

    [<CustomOperation("createAcl")>]
    member _.CreateAcl(config: AccessPointPropsConfig, ownerGid: string, ownerUid: string, permissions: string) =
        let acl = Acl(OwnerGid = ownerGid, OwnerUid = ownerUid, Permissions = permissions)
        { config with CreateAcl = Some acl }

// ============================================================================
// Lambda Function URL Options Builder DSL
// ============================================================================

type FunctionUrlCorsOptionsConfig =
    { AllowCredentials: bool option
      AllowedHeaders: string list option
      AllowedMethods: HttpMethod list option
      AllowedOrigins: string list option
      ExposeHeaders: string list option
      MaxAge: Duration option }

type FunctionUrlCorsOptionsBuilder() =
    member _.Yield _ : FunctionUrlCorsOptionsConfig =
        { AllowCredentials = None
          AllowedHeaders = None
          AllowedMethods = None
          AllowedOrigins = None
          ExposeHeaders = None
          MaxAge = None }

    member _.Zero() : FunctionUrlCorsOptionsConfig =
        { AllowCredentials = None
          AllowedHeaders = None
          AllowedMethods = None
          AllowedOrigins = None
          ExposeHeaders = None
          MaxAge = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> FunctionUrlCorsOptionsConfig) : FunctionUrlCorsOptionsConfig =
        f ()

    member inline x.For
        (
            config: FunctionUrlCorsOptionsConfig,
            [<InlineIfLambda>] f: unit -> FunctionUrlCorsOptionsConfig
        ) : FunctionUrlCorsOptionsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine
        (
            state1: FunctionUrlCorsOptionsConfig,
            state2: FunctionUrlCorsOptionsConfig
        ) : FunctionUrlCorsOptionsConfig =
        { AllowCredentials =
            if state1.AllowCredentials.IsSome then
                state1.AllowCredentials
            else
                state2.AllowCredentials
          AllowedHeaders =
            if state1.AllowedHeaders.IsSome then
                state1.AllowedHeaders
            else
                state2.AllowedHeaders
          AllowedMethods =
            if state1.AllowedMethods.IsSome then
                state1.AllowedMethods
            else
                state2.AllowedMethods
          AllowedOrigins =
            if state1.AllowedOrigins.IsSome then
                state1.AllowedOrigins
            else
                state2.AllowedOrigins
          ExposeHeaders =
            if state1.ExposeHeaders.IsSome then
                state1.ExposeHeaders
            else
                state2.ExposeHeaders
          MaxAge =
            if state1.MaxAge.IsSome then
                state1.MaxAge
            else
                state2.MaxAge }

    member _.Run(config: FunctionUrlCorsOptionsConfig) : FunctionUrlCorsSpec =
        let o = FunctionUrlCorsOptions()
        config.AllowCredentials |> Option.iter (fun v -> o.AllowCredentials <- v)

        config.AllowedHeaders
        |> Option.iter (fun v -> o.AllowedHeaders <- (v |> List.toArray))

        config.AllowedMethods
        |> Option.iter (fun v -> o.AllowedMethods <- (v |> List.toArray))

        config.AllowedOrigins
        |> Option.iter (fun v -> o.AllowedOrigins <- (v |> List.toArray))

        config.ExposeHeaders
        |> Option.iter (fun v -> o.ExposedHeaders <- (v |> List.toArray))

        config.MaxAge |> Option.iter (fun d -> o.MaxAge <- d)
        { Options = o :> IFunctionUrlCorsOptions }

    [<CustomOperation("allowCredentials")>]
    member _.AllowCredentials(config: FunctionUrlCorsOptionsConfig, value: bool) =
        { config with
            AllowCredentials = Some value }

    [<CustomOperation("allowedHeaders")>]
    member _.AllowedHeaders(config: FunctionUrlCorsOptionsConfig, headers: string list) =
        { config with
            AllowedHeaders = Some headers }

    [<CustomOperation("allowedMethods")>]
    member _.AllowedMethods(config: FunctionUrlCorsOptionsConfig, methods: HttpMethod list) =
        { config with
            AllowedMethods = Some methods }

    [<CustomOperation("allowedOrigins")>]
    member _.AllowedOrigins(config: FunctionUrlCorsOptionsConfig, origins: string list) =
        { config with
            AllowedOrigins = Some origins }

    [<CustomOperation("exposeHeaders")>]
    member _.ExposeHeaders(config: FunctionUrlCorsOptionsConfig, headers: string list) =
        { config with
            ExposeHeaders = Some headers }

    [<CustomOperation("maxAge")>]
    member _.MaxAge(config: FunctionUrlCorsOptionsConfig, duration: Duration) = { config with MaxAge = Some duration }

type FunctionUrlOptionsConfig =
    { AuthType: FunctionUrlAuthType option
      Cors: IFunctionUrlCorsOptions option
      InvokeMode: InvokeMode option }

type FunctionUrlOptionsBuilder() =
    member _.Yield _ : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = None
          InvokeMode = None }

    member _.Zero() : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = None
          InvokeMode = None }

    member _.Run(config: FunctionUrlOptionsConfig) : FunctionUrlSpec =
        let opts = FunctionUrlOptions()
        config.AuthType |> Option.iter (fun a -> opts.AuthType <- a)
        config.Cors |> Option.iter (fun c -> opts.Cors <- c)
        config.InvokeMode |> Option.iter (fun m -> opts.InvokeMode <- m)
        { Options = opts :> IFunctionUrlOptions }

    member inline _.Delay([<InlineIfLambda>] f: unit -> FunctionUrlOptionsConfig) : FunctionUrlOptionsConfig = f ()

    member inline x.For
        (
            config: FunctionUrlOptionsConfig,
            [<InlineIfLambda>] f: unit -> FunctionUrlOptionsConfig
        ) : FunctionUrlOptionsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: FunctionUrlOptionsConfig, state2: FunctionUrlOptionsConfig) : FunctionUrlOptionsConfig =
        { AuthType =
            if state1.AuthType.IsSome then
                state1.AuthType
            else
                state2.AuthType
          Cors = if state1.Cors.IsSome then state1.Cors else state2.Cors
          InvokeMode =
            if state1.InvokeMode.IsSome then
                state1.InvokeMode
            else
                state2.InvokeMode }

    [<CustomOperation("authType")>]
    member _.AuthType(config: FunctionUrlOptionsConfig, auth: FunctionUrlAuthType) =
        { config with AuthType = Some auth }

    [<CustomOperation("invokeMode")>]
    member _.InvokeMode(config: FunctionUrlOptionsConfig, mode: InvokeMode) = { config with InvokeMode = Some mode }

    member _.Yield(spec: FunctionUrlCorsSpec) : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = Some spec.Options
          InvokeMode = None }


// ============================================================================
// Lambda Add* Options Builders DSL (for consistency with other add functions)
// ============================================================================

// Types for EventSourceMapping

type EventSourceMappingOptionsConfig =
    { EventSourceArn: string option
      BatchSize: int option
      StartingPosition: StartingPosition option
      Enabled: bool option
      MaxBatchingWindow: Duration option
      ParallelizationFactor: int option }

type EventSourceMappingOptionsBuilder(id: string) =
    member _.Yield _ : EventSourceMappingOptionsConfig =
        { EventSourceArn = None
          BatchSize = None
          StartingPosition = None
          Enabled = None
          MaxBatchingWindow = None
          ParallelizationFactor = None }

    member _.Zero() : EventSourceMappingOptionsConfig =
        { EventSourceArn = None
          BatchSize = None
          StartingPosition = None
          Enabled = None
          MaxBatchingWindow = None
          ParallelizationFactor = None }

    member inline _.Delay
        ([<InlineIfLambda>] f: unit -> EventSourceMappingOptionsConfig)
        : EventSourceMappingOptionsConfig =
        f ()

    member inline x.For
        (
            config: EventSourceMappingOptionsConfig,
            [<InlineIfLambda>] f: unit -> EventSourceMappingOptionsConfig
        ) : EventSourceMappingOptionsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine
        (
            state1: EventSourceMappingOptionsConfig,
            state2: EventSourceMappingOptionsConfig
        ) : EventSourceMappingOptionsConfig =
        { EventSourceArn =
            if state1.EventSourceArn.IsSome then
                state1.EventSourceArn
            else
                state2.EventSourceArn
          BatchSize =
            if state1.BatchSize.IsSome then
                state1.BatchSize
            else
                state2.BatchSize
          StartingPosition =
            if state1.StartingPosition.IsSome then
                state1.StartingPosition
            else
                state2.StartingPosition
          Enabled =
            if state1.Enabled.IsSome then
                state1.Enabled
            else
                state2.Enabled
          MaxBatchingWindow =
            if state1.MaxBatchingWindow.IsSome then
                state1.MaxBatchingWindow
            else
                state2.MaxBatchingWindow
          ParallelizationFactor =
            if state1.ParallelizationFactor.IsSome then
                state1.ParallelizationFactor
            else
                state2.ParallelizationFactor }

    member _.Run(config: EventSourceMappingOptionsConfig) : EventSourceMappingSpec =
        let opts = EventSourceMappingOptions()

        let arn =
            match config.EventSourceArn with
            | Some a -> a
            | None -> failwith "eventSourceArn is required for EventSourceMappingOptions"

        opts.EventSourceArn <- arn
        config.BatchSize |> Option.iter (fun v -> opts.BatchSize <- v)
        config.StartingPosition |> Option.iter (fun v -> opts.StartingPosition <- v)
        config.Enabled |> Option.iter (fun v -> opts.Enabled <- v)
        config.MaxBatchingWindow |> Option.iter (fun d -> opts.MaxBatchingWindow <- d)

        config.ParallelizationFactor
        |> Option.iter (fun v -> opts.ParallelizationFactor <- v)

        { Id = id
          Options = opts :> IEventSourceMappingOptions }

    [<CustomOperation("eventSourceArn")>]
    member _.EventSourceArn(config: EventSourceMappingOptionsConfig, arn: string) =
        { config with
            EventSourceArn = Some arn }

    [<CustomOperation("batchSize")>]
    member _.BatchSize(config: EventSourceMappingOptionsConfig, size: int) = { config with BatchSize = Some size }

    [<CustomOperation("startingPosition")>]
    member _.StartingPosition(config: EventSourceMappingOptionsConfig, pos: StartingPosition) =
        { config with
            StartingPosition = Some pos }

    [<CustomOperation("enabled")>]
    member _.Enabled(config: EventSourceMappingOptionsConfig, value: bool) = { config with Enabled = Some value }

    [<CustomOperation("maxBatchingWindow")>]
    member _.MaxBatchingWindow(config: EventSourceMappingOptionsConfig, window: Duration) =
        { config with
            MaxBatchingWindow = Some window }

    [<CustomOperation("parallelizationFactor")>]
    member _.ParallelizationFactor(config: EventSourceMappingOptionsConfig, value: int) =
        { config with
            ParallelizationFactor = Some value }

// Builder for Permission

type PermissionConfig =
    { Id: string
      Principal: IPrincipal option
      Action: string option
      SourceArn: string option
      SourceAccount: string option
      EventSourceToken: string option }

type PermissionBuilder(id: string) =
    member _.Yield _ : PermissionConfig =
        { Id = id
          Principal = None
          Action = None
          SourceArn = None
          SourceAccount = None
          EventSourceToken = None }

    member _.Zero() : PermissionConfig =
        { Id = id
          Principal = None
          Action = None
          SourceArn = None
          SourceAccount = None
          EventSourceToken = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PermissionConfig) : PermissionConfig = f ()

    member _.Run(config: PermissionConfig) : PermissionSpec =
        let p = Permission()

        let principal =
            match config.Principal with
            | Some pr -> pr
            | None -> failwith "permission.principal is required"

        p.Principal <- principal
        config.Action |> Option.iter (fun a -> p.Action <- a)
        config.SourceArn |> Option.iter (fun arn -> p.SourceArn <- arn)
        config.SourceAccount |> Option.iter (fun acc -> p.SourceAccount <- acc)
        config.EventSourceToken |> Option.iter (fun t -> p.EventSourceToken <- t)

        { Id = config.Id; Permission = p }

    [<CustomOperation("principal")>]
    member _.Principal(config: PermissionConfig, principal: IPrincipal) : PermissionConfig =
        { config with
            Principal = Some principal }

    [<CustomOperation("action")>]
    member _.Action(config: PermissionConfig, action: string) = { config with Action = Some action }

    [<CustomOperation("sourceArn")>]
    member _.SourceArn(config: PermissionConfig, arn: string) = { config with SourceArn = Some arn }

    [<CustomOperation("sourceAccount")>]
    member _.SourceAccount(config: PermissionConfig, account: string) =
        { config with
            SourceAccount = Some account }

    [<CustomOperation("eventSourceToken")>]
    member _.EventSourceToken(config: PermissionConfig, token: string) =
        { config with
            EventSourceToken = Some token }

    member _.Combine(state1: PermissionConfig, state2: PermissionConfig) =
        { Id = state1.Id
          Principal =
            if state1.Principal.IsSome then
                state1.Principal
            else
                state2.Principal
          Action =
            if state1.Action.IsSome then
                state1.Action
            else
                state2.Action
          SourceArn =
            if state1.SourceArn.IsSome then
                state1.SourceArn
            else
                state2.SourceArn
          SourceAccount =
            if state1.SourceAccount.IsSome then
                state1.SourceAccount
            else
                state2.SourceAccount
          EventSourceToken =
            if state1.EventSourceToken.IsSome then
                state1.EventSourceToken
            else
                state2.EventSourceToken }

// Builder for EventInvokeConfigOptions

type EventInvokeConfigOptionsConfig =
    { MaxEventAge: Duration option
      RetryAttempts: int option }

type EventInvokeConfigOptionsBuilder() =
    member _.Yield _ : EventInvokeConfigOptionsConfig =
        { MaxEventAge = None
          RetryAttempts = None }

    member _.Zero() : EventInvokeConfigOptionsConfig =
        { MaxEventAge = None
          RetryAttempts = None }

    member _.Run(config: EventInvokeConfigOptionsConfig) : EventInvokeConfigSpec =
        let o = EventInvokeConfigOptions()
        config.MaxEventAge |> Option.iter (fun d -> o.MaxEventAge <- d)
        config.RetryAttempts |> Option.iter (fun r -> o.RetryAttempts <- r)
        { Options = o :> IEventInvokeConfigOptions }

    [<CustomOperation("maxEventAge")>]
    member _.MaxEventAge(config: EventInvokeConfigOptionsConfig, duration: Duration) =
        { config with
            MaxEventAge = Some duration }

    [<CustomOperation("retryAttempts")>]
    member _.RetryAttempts(config: EventInvokeConfigOptionsConfig, attempts: int) =
        { config with
            RetryAttempts = Some attempts }


// ============================================================================
// Lambda Function URL CORS Options Builder DSL
// ============================================================================

// ============================================================================
// IAM PolicyStatementProps and PolicyStatement Builders DSL
// ============================================================================

type PolicyStatementPropsConfig =
    { Actions: string list option
      Resources: string list option
      Effect: Effect option
      Principals: IPrincipal list option
      Sid: string option }

type PolicyStatementPropsBuilder() =
    member _.Yield _ : PolicyStatementPropsConfig =
        { Actions = None
          Resources = None
          Effect = None
          Principals = None
          Sid = None }

    member _.Zero() : PolicyStatementPropsConfig =
        { Actions = None
          Resources = None
          Effect = None
          Principals = None
          Sid = None }

    member _.Run(config: PolicyStatementPropsConfig) : PolicyStatementProps =
        let p = PolicyStatementProps()
        config.Actions |> Option.iter (fun a -> p.Actions <- (a |> List.toArray))
        config.Resources |> Option.iter (fun r -> p.Resources <- (r |> List.toArray))
        config.Effect |> Option.iter (fun e -> p.Effect <- e)

        config.Principals
        |> Option.iter (fun pr -> p.Principals <- (pr |> List.toArray))

        config.Sid |> Option.iter (fun sid -> p.Sid <- sid)
        p

    [<CustomOperation("actions")>]
    member _.Actions(config: PolicyStatementPropsConfig, actions: string list) = { config with Actions = Some actions }

    [<CustomOperation("resources")>]
    member _.Resources(config: PolicyStatementPropsConfig, resources: string list) =
        { config with
            Resources = Some resources }

    [<CustomOperation("effect")>]
    member _.Effect(config: PolicyStatementPropsConfig, effect: Effect) = { config with Effect = Some effect }

    [<CustomOperation("principals")>]
    member _.Principals(config: PolicyStatementPropsConfig, principals: IPrincipal list) =
        { config with
            Principals = Some principals }

    [<CustomOperation("sid")>]
    member _.Sid(config: PolicyStatementPropsConfig, sid: string) = { config with Sid = Some sid }


type PolicyStatementConfig =
    { Props: PolicyStatementProps option
      Actions: string list
      Resources: string list
      Effect: Effect option
      Principals: IPrincipal list
      Sid: string option }

type PolicyStatementBuilder() =
    member _.Yield _ : PolicyStatementConfig =
        { Props = None
          Actions = []
          Resources = []
          Effect = None
          Principals = []
          Sid = None }

    member _.Yield(props: PolicyStatementProps) : PolicyStatementConfig =
        { Props = Some props
          Actions = []
          Resources = []
          Effect = None
          Principals = []
          Sid = None }

    member _.Zero() : PolicyStatementConfig =
        { Props = None
          Actions = []
          Resources = []
          Effect = None
          Principals = []
          Sid = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PolicyStatementConfig) : PolicyStatementConfig = f ()

    member inline x.For
        (
            config: PolicyStatementConfig,
            [<InlineIfLambda>] f: unit -> PolicyStatementConfig
        ) : PolicyStatementConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: PolicyStatementConfig, state2: PolicyStatementConfig) : PolicyStatementConfig =
        { Props = if state1.Props.IsSome then state1.Props else state2.Props
          Actions = state1.Actions @ state2.Actions
          Resources = state1.Resources @ state2.Resources
          Effect =
            if state1.Effect.IsSome then
                state1.Effect
            else
                state2.Effect
          Principals = state1.Principals @ state2.Principals
          Sid = if state1.Sid.IsSome then state1.Sid else state2.Sid }

    member _.Run(config: PolicyStatementConfig) : PolicyStatement =
        match config.Props with
        | Some props ->
            let stmt = PolicyStatement(props)
            // Apply any additional properties
            if config.Actions.Length > 0 then
                stmt.AddActions([| for a in config.Actions -> a |])

            if config.Resources.Length > 0 then
                stmt.AddResources([| for r in config.Resources -> r |])

            stmt
        | None ->
            let props = PolicyStatementProps()

            if config.Actions.Length > 0 then
                props.Actions <- config.Actions |> List.toArray

            if config.Resources.Length > 0 then
                props.Resources <- config.Resources |> List.toArray

            config.Effect |> Option.iter (fun e -> props.Effect <- e)

            if config.Principals.Length > 0 then
                props.Principals <- config.Principals |> List.toArray

            config.Sid |> Option.iter (fun sid -> props.Sid <- sid)
            PolicyStatement(props)

    [<CustomOperation("withProps")>]
    member _.WithProps(config: PolicyStatementConfig, props: PolicyStatementProps) = { config with Props = Some props }

    [<CustomOperation("actions")>]
    member _.Actions(config: PolicyStatementConfig, actions: string list) = { config with Actions = actions }

    [<CustomOperation("resources")>]
    member _.Resources(config: PolicyStatementConfig, resources: string list) = { config with Resources = resources }

    [<CustomOperation("effect")>]
    member _.Effect(config: PolicyStatementConfig, effect: Effect) = { config with Effect = Some effect }

    [<CustomOperation("principals")>]
    member _.Principals(config: PolicyStatementConfig, principals: IPrincipal list) =
        { config with Principals = principals }

    [<CustomOperation("sid")>]
    member _.Sid(config: PolicyStatementConfig, sid: string) = { config with Sid = Some sid }
