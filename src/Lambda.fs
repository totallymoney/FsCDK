namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open System.Collections.Generic

// ============================================================================
// Lambda Function Configuration DSL
// ============================================================================

// Lambda configuration DSL
type LambdaConfig =
    { FunctionName: string option
      ConstructId: string option // Optional custom construct ID
      Handler: string option
      Runtime: Runtime option
      CodePath: Code option
      Environment: (string * string) seq
      Timeout: float option
      Memory: int option
      Description: string option }

type LambdaSpec =
    { FunctionName: string
      ConstructId: string // Construct ID for CDK
      Props: FunctionProps }

type LambdaBuilder() =
    member _.Yield _ : LambdaConfig =
        { FunctionName = None
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

    member _.Zero() : LambdaConfig =
        { FunctionName = None
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

    member _.Run(config: LambdaConfig) : LambdaSpec =
        // Function name is required
        let lambdaName =
            match config.FunctionName with
            | Some name -> name
            | None -> failwith "Lambda function name is required"

        // Construct ID defaults to function name if not specified
        let constructId = config.ConstructId |> Option.defaultValue lambdaName

        let props = FunctionProps(FunctionName = lambdaName)

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

        { FunctionName = lambdaName
          ConstructId = constructId
          Props = props }

    [<CustomOperation("name")>]
    member _.Name(config: LambdaConfig, lambdaName: string) =
        { config with
            FunctionName = Some lambdaName }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: LambdaConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("handler")>]
    member _.Handler(config: LambdaConfig, value: string) = { config with Handler = Some value }

    [<CustomOperation("runtime")>]
    member _.Runtime(config: LambdaConfig, value: Runtime) = { config with Runtime = Some value }

    [<CustomOperation("code")>]
    member _.Code(config: LambdaConfig, path: string) =
        { config with
            CodePath = Some(Code.FromAsset(path)) }


    [<CustomOperation("code")>]
    member _.Code(config: LambdaConfig, path: Code) = { config with CodePath = Some path }

    [<CustomOperation("environment")>]
    member _.Environment(config: LambdaConfig, vars: (string * string) seq) = { config with Environment = vars }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: LambdaConfig, seconds: float) = { config with Timeout = Some seconds }

    [<CustomOperation("memory")>]
    member _.Memory(config: LambdaConfig, mb: int) = { config with Memory = Some mb }

    [<CustomOperation("description")>]
    member _.Description(config: LambdaConfig, desc: string) = { config with Description = Some desc }
