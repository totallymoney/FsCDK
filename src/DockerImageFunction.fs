namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open System.Collections.Generic
open System

// ============================================================================
// Lambda Docker Image Function Configuration DSL
// ============================================================================

// Configuration for Docker image-based Lambda
// Follow CDK naming for properties and operations; do not reuse Lambda.fs conventions
// JSII compatibility: expose and pass only .NET CDK types (no F#-specific types)

type DockerImageFunctionConfig =
    { FunctionName: string
      ConstructId: string option // Optional custom construct ID
      Code: string option
      Environment: (string * string) seq
      Timeout: float option
      Memory: int option
      Description: string option }

type DockerImageFunctionSpec =
    { FunctionName: string
      ConstructId: string // Construct ID for CDK
      Code: string
      TimeoutSeconds: System.Nullable<double>
      Props: DockerImageFunctionProps }

type DockerImageFunctionBuilder(name: string) =
    do
        if System.String.IsNullOrWhiteSpace(name) then
            failwith "Docker image function name is required"

    member _.Yield _ : DockerImageFunctionConfig =
        { FunctionName = name
          ConstructId = None
          Code = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

    member _.Zero() : DockerImageFunctionConfig =
        { FunctionName = name
          ConstructId = None
          Code = None
          Environment = []
          Timeout = None
          Memory = None
          Description = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DockerImageFunctionConfig) : DockerImageFunctionConfig = f ()

    member _.Combine(state1: DockerImageFunctionConfig, state2: DockerImageFunctionConfig) : DockerImageFunctionConfig =
        { FunctionName = state1.FunctionName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Code = state2.Code |> Option.orElse state1.Code
          Environment = List.ofSeq (Seq.append state1.Environment state2.Environment)
          Timeout = state2.Timeout |> Option.orElse state1.Timeout
          Memory = state2.Memory |> Option.orElse state1.Memory
          Description = state2.Description |> Option.orElse state1.Description }

    member inline x.For
        (
            config: DockerImageFunctionConfig,
            [<InlineIfLambda>] f: unit -> DockerImageFunctionConfig
        ) : DockerImageFunctionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: DockerImageFunctionConfig) : DockerImageFunctionSpec =
        // Function name is required
        let lambdaName = config.FunctionName

        // Construct ID defaults to function name if not specified
        let constructId = config.ConstructId |> Option.defaultValue lambdaName

        // Image asset path is required
        let imagePath =
            match config.Code with
            | Some path -> path
            | None -> failwith "Docker image asset path is required"

        // Create props without eagerly constructing DockerImageCode (to avoid JSII in unit tests)
        let props = DockerImageFunctionProps(FunctionName = lambdaName)

        // Environment variables
        if not (Seq.isEmpty config.Environment) then
            let envDict = Dictionary<string, string>()
            config.Environment |> Seq.iter envDict.Add
            props.Environment <- envDict

        // Optional properties - defer Timeout to stack application to avoid JSII in unit tests
        config.Memory |> Option.iter (fun m -> props.MemorySize <- m)
        config.Description |> Option.iter (fun desc -> props.Description <- desc)

        { FunctionName = lambdaName
          ConstructId = constructId
          Code = imagePath
          TimeoutSeconds =
            (match config.Timeout with
             | Some t -> Nullable t
             | None -> Nullable())
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DockerImageFunctionConfig, id: string) = { config with ConstructId = Some id }

    // Use the same operation name as regular Lambda to keep DSL consistent
    [<CustomOperation("code")>]
    member _.ImageAsset(config: DockerImageFunctionConfig, imagePath: string) = { config with Code = Some imagePath }

    [<CustomOperation("environment")>]
    member _.Environment(config: DockerImageFunctionConfig, vars: (string * string) seq) =
        { config with Environment = vars }

    [<CustomOperation("timeout")>]
    member _.Timeout(config: DockerImageFunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    [<CustomOperation("memorySize")>]
    member _.MemorySize(config: DockerImageFunctionConfig, mb: int) = { config with Memory = Some mb }

    [<CustomOperation("description")>]
    member _.Description(config: DockerImageFunctionConfig, desc: string) = { config with Description = Some desc }
