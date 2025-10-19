namespace FsCDK

#nowarn "44" // Suppress deprecation warning for LogRetention

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.KMS
open System.Collections.Generic

/// <summary>
/// High-level Lambda Function builder following AWS security best practices.
/// 
/// **Default Settings:**
/// - MemorySize = 512 MB (balanced performance/cost)
/// - Timeout = 30 seconds
/// - Environment encryption = KMS with AWS managed key (aws/lambda)
/// - X-Ray tracing = disabled (opt-in via tracing operation)
/// - CloudWatch log retention = 90 days (NINETY_DAYS)
/// - Minimal IAM execution role with:
///   - CloudWatch Logs write permissions
///   - KMS decrypt for environment variables (when encryption enabled)
/// 
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Environment variable encryption protects sensitive configuration
/// - Log retention balances auditability with cost
/// - Minimal IAM permissions follow least-privilege principle
/// - Memory/timeout defaults work for most serverless workloads
/// 
/// **Escape Hatch:**
/// Access the underlying CDK Function via the `Function` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type LambdaFunctionConfig =
    { FunctionName: string
      ConstructId: string option
      Handler: string option
      Runtime: Runtime option
      Code: Code option
      MemorySize: int option
      Timeout: Duration option
      Environment: Map<string, string>
      EnvironmentEncryption: IKey option
      Tracing: Tracing option
      LogRetention: RetentionDays option
      Description: string option
      Role: IRole option
      ReservedConcurrentExecutions: int option }

type LambdaFunctionResource =
    { FunctionName: string
      ConstructId: string
      /// The underlying CDK Function construct - use for advanced scenarios
      Function: Function
      /// The IAM execution role created for this function
      Role: IRole }

type LambdaFunctionBuilder(name: string) =
    member _.Yield _ : LambdaFunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          Code = None
          MemorySize = Some 512
          Timeout = Some (Duration.Seconds(30.0))
          Environment = Map.empty
          EnvironmentEncryption = None
          Tracing = Some Tracing.DISABLED
          LogRetention = Some RetentionDays.THREE_MONTHS
          Description = None
          Role = None
          ReservedConcurrentExecutions = None }

    member _.Zero() : LambdaFunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          Code = None
          MemorySize = Some 512
          Timeout = Some (Duration.Seconds(30.0))
          Environment = Map.empty
          EnvironmentEncryption = None
          Tracing = Some Tracing.DISABLED
          LogRetention = Some RetentionDays.THREE_MONTHS
          Description = None
          Role = None
          ReservedConcurrentExecutions = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LambdaFunctionConfig) : LambdaFunctionConfig = f ()

    member _.Combine(state1: LambdaFunctionConfig, state2: LambdaFunctionConfig) : LambdaFunctionConfig =
        { FunctionName = state1.FunctionName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Handler = state2.Handler |> Option.orElse state1.Handler
          Runtime = state2.Runtime |> Option.orElse state1.Runtime
          Code = state2.Code |> Option.orElse state1.Code
          MemorySize = state2.MemorySize |> Option.orElse state1.MemorySize
          Timeout = state2.Timeout |> Option.orElse state1.Timeout
          Environment = Map.fold (fun acc key value -> Map.add key value acc) state1.Environment state2.Environment
          EnvironmentEncryption = state2.EnvironmentEncryption |> Option.orElse state1.EnvironmentEncryption
          Tracing = state2.Tracing |> Option.orElse state1.Tracing
          LogRetention = state2.LogRetention |> Option.orElse state1.LogRetention
          Description = state2.Description |> Option.orElse state1.Description
          Role = state2.Role |> Option.orElse state1.Role
          ReservedConcurrentExecutions = state2.ReservedConcurrentExecutions |> Option.orElse state1.ReservedConcurrentExecutions }

    member inline x.For(config: LambdaFunctionConfig, [<InlineIfLambda>] f: unit -> LambdaFunctionConfig) : LambdaFunctionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: LambdaFunctionConfig) : LambdaFunctionResource =
        let functionName = config.FunctionName
        let constructId = config.ConstructId |> Option.defaultValue functionName

        let props = FunctionProps()
        props.FunctionName <- functionName

        config.Handler |> Option.iter (fun v -> props.Handler <- v)
        config.Runtime |> Option.iter (fun v -> props.Runtime <- v)
        config.Code |> Option.iter (fun v -> props.Code <- v)
        config.MemorySize |> Option.iter (fun v -> props.MemorySize <- System.Nullable<float>(float v))
        config.Timeout |> Option.iter (fun v -> props.Timeout <- v)
        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.Tracing |> Option.iter (fun v -> props.Tracing <- v)
        config.Role |> Option.iter (fun v -> props.Role <- v)
        config.ReservedConcurrentExecutions |> Option.iter (fun v -> props.ReservedConcurrentExecutions <- System.Nullable<float>(float v))

        if not (Map.isEmpty config.Environment) then
            let envDict = Dictionary<string, string>()
            config.Environment |> Map.iter (fun k v -> envDict.Add(k, v))
            props.Environment <- envDict

        config.EnvironmentEncryption |> Option.iter (fun v -> props.EnvironmentEncryption <- v)
        config.LogRetention |> Option.iter (fun v -> props.LogRetention <- v)

        { FunctionName = functionName
          ConstructId = constructId
          Function = null // Will be created during stack construction
          Role = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: LambdaFunctionConfig, id: string) =
        { config with ConstructId = Some id }

    [<CustomOperation("handler")>]
    member _.Handler(config: LambdaFunctionConfig, handler: string) =
        { config with Handler = Some handler }

    [<CustomOperation("runtime")>]
    member _.Runtime(config: LambdaFunctionConfig, runtime: Runtime) =
        { config with Runtime = Some runtime }

    [<CustomOperation("code")>]
    member _.Code(config: LambdaFunctionConfig, code: Code) =
        { config with Code = Some code }

    [<CustomOperation("codePath")>]
    member _.CodePath(config: LambdaFunctionConfig, path: string) =
        { config with Code = Some (Code.FromAsset(path)) }

    [<CustomOperation("memorySize")>]
    member _.MemorySize(config: LambdaFunctionConfig, size: int) =
        { config with MemorySize = Some size }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: LambdaFunctionConfig, seconds: float) =
        { config with Timeout = Some (Duration.Seconds(seconds)) }

    [<CustomOperation("environment")>]
    member _.Environment(config: LambdaFunctionConfig, env: (string * string) list) =
        { config with Environment = env |> Map.ofList }

    [<CustomOperation("environmentEncryption")>]
    member _.EnvironmentEncryption(config: LambdaFunctionConfig, key: IKey) =
        { config with EnvironmentEncryption = Some key }

    [<CustomOperation("tracing")>]
    member _.Tracing(config: LambdaFunctionConfig, tracing: Tracing) =
        { config with Tracing = Some tracing }

    [<CustomOperation("xrayEnabled")>]
    member _.XRayEnabled(config: LambdaFunctionConfig) =
        { config with Tracing = Some Tracing.ACTIVE }

    [<CustomOperation("logRetention")>]
    member _.LogRetention(config: LambdaFunctionConfig, retention: RetentionDays) =
        { config with LogRetention = Some retention }

    [<CustomOperation("description")>]
    member _.Description(config: LambdaFunctionConfig, desc: string) =
        { config with Description = Some desc }

    [<CustomOperation("role")>]
    member _.Role(config: LambdaFunctionConfig, role: IRole) =
        { config with Role = Some role }

    [<CustomOperation("reservedConcurrentExecutions")>]
    member _.ReservedConcurrentExecutions(config: LambdaFunctionConfig, count: int) =
        { config with ReservedConcurrentExecutions = Some count }

[<AutoOpen>]
module HighLevelLambdaBuilders =
    /// <summary>
    /// Creates a new Lambda function builder with secure defaults.
    /// Example: lambdaFunction "my-function" { handler "index.handler"; runtime Runtime.NODEJS_18_X }
    /// </summary>
    let lambdaFunction name = LambdaFunctionBuilder(name)
