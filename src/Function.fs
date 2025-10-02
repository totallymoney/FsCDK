namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.S3.Assets
open System.Collections.Generic

type PermissionSpec = { Id: string; Permission: IPermission }

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
      // Post-creation operations
      EventSources: IEventSource list
      EventSourceMappings: (string * IEventSourceMappingOptions) list
      FunctionUrlOptions: IFunctionUrlOptions option
      Permissions: (string * IPermission) list
      RolePolicyStatements: PolicyStatement list
      AsyncInvokeOptions: IEventInvokeConfigOptions option }

type FunctionSpec =
    { FunctionName: string
      ConstructId: string // Construct ID for CDK
      Props: FunctionProps
      Actions: (Function -> unit) list }

type FunctionBuilder(name: string) =
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
          AsyncInvokeOptions = None }

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
          AsyncInvokeOptions = None }

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
                state2.AsyncInvokeOptions }

    member _.Delay(f: unit -> FunctionConfig) : FunctionConfig = f ()

    member x.For(config: FunctionConfig, f: unit -> FunctionConfig) : FunctionConfig =
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
          AsyncInvokeOptions = None }

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

    [<CustomOperation("eventSource")>]
    member _.EventSource(config: FunctionConfig, source: IEventSource) =
        { config with
            EventSources = config.EventSources @ [ source ] }

    [<CustomOperation("eventSourceMapping")>]
    member _.EventSourceMapping(config: FunctionConfig, id: string, options: IEventSourceMappingOptions) =
        { config with
            EventSourceMappings = config.EventSourceMappings @ [ (id, options) ] }

    [<CustomOperation("functionUrl")>]
    member _.FunctionUrl(config: FunctionConfig, options: IFunctionUrlOptions) =
        { config with
            FunctionUrlOptions = Some options }

    [<CustomOperation("toRolePolicy")>]
    member _.ToRolePolicy(config: FunctionConfig, statement: PolicyStatement) =
        { config with
            RolePolicyStatements = config.RolePolicyStatements @ [ statement ] }

    [<CustomOperation("configureAsyncInvoke")>]
    member _.ConfigureAsyncInvoke(config: FunctionConfig, options: IEventInvokeConfigOptions) =
        { config with
            AsyncInvokeOptions = Some options }


// ============================================================================
// Lambda Function URL Options Builder DSL
// ============================================================================

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

    member _.Run(config: FunctionUrlOptionsConfig) : IFunctionUrlOptions =
        let opts = FunctionUrlOptions()
        config.AuthType |> Option.iter (fun a -> opts.AuthType <- a)
        config.Cors |> Option.iter (fun c -> opts.Cors <- c)
        config.InvokeMode |> Option.iter (fun m -> opts.InvokeMode <- m)
        opts :> IFunctionUrlOptions

    [<CustomOperation("authType")>]
    member _.AuthType(config: FunctionUrlOptionsConfig, auth: FunctionUrlAuthType) =
        { config with AuthType = Some auth }

    [<CustomOperation("cors")>]
    member _.Cors(config: FunctionUrlOptionsConfig, cors: IFunctionUrlCorsOptions) = { config with Cors = Some cors }

    [<CustomOperation("invokeMode")>]
    member _.InvokeMode(config: FunctionUrlOptionsConfig, mode: InvokeMode) = { config with InvokeMode = Some mode }


// ============================================================================
// Lambda Add* Options Builders DSL (for consistency with other add functions)
// ============================================================================

// Builder for EventSourceMappingOptions

type EventSourceMappingOptionsConfig =
    { EventSourceArn: string option
      BatchSize: int option
      StartingPosition: StartingPosition option
      Enabled: bool option
      MaxBatchingWindow: Duration option
      ParallelizationFactor: int option }

type EventSourceMappingOptionsBuilder() =
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

    member _.Run(config: EventSourceMappingOptionsConfig) : IEventSourceMappingOptions =
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

        opts :> IEventSourceMappingOptions

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

    member _.Delay(f: unit -> PermissionConfig) : PermissionConfig = f ()

    member _.For(config: PermissionConfig, f: unit -> PermissionConfig) : PermissionConfig = f ()

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

    member _.Run(config: EventInvokeConfigOptionsConfig) : IEventInvokeConfigOptions =
        let o = EventInvokeConfigOptions()
        config.MaxEventAge |> Option.iter (fun d -> o.MaxEventAge <- d)
        config.RetryAttempts |> Option.iter (fun r -> o.RetryAttempts <- r)
        o :> IEventInvokeConfigOptions

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

    member _.Run(config: FunctionUrlCorsOptionsConfig) : IFunctionUrlCorsOptions =
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
        o :> IFunctionUrlCorsOptions

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

    member _.Zero() : PolicyStatementConfig =
        { Props = None
          Actions = []
          Resources = []
          Effect = None
          Principals = []
          Sid = None }

    member _.Run(config: PolicyStatementConfig) : PolicyStatement =
        match config.Props with
        | Some props -> PolicyStatement(props)
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
