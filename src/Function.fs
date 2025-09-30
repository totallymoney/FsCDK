namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
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
      Description: string option }

type FunctionSpec =
    { FunctionName: string
      ConstructId: string // Construct ID for CDK
      Props: FunctionProps }

type FunctionBuilder(name: string) =
    member _.Yield _ : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

    member _.Zero() : FunctionConfig =
        { FunctionName = name
          ConstructId = None
          Handler = None
          Runtime = None
          CodePath = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

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

        { FunctionName = config.FunctionName
          ConstructId = constructId
          Props = props }

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
    member _.Code(config: FunctionConfig, path: Code) = { config with CodePath = Some path }

    [<CustomOperation("environment")>]
    member _.Environment(config: FunctionConfig, vars: (string * string) seq) = { config with Environment = vars }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: FunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    [<CustomOperation("memory")>]
    member _.Memory(config: FunctionConfig, mb: int) = { config with Memory = Some mb }

    [<CustomOperation("description")>]
    member _.Description(config: FunctionConfig, desc: string) = { config with Description = Some desc }
