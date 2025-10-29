namespace FsCDK

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
      Props: DockerImageFunctionProps
      mutable Function: IFunction }

type DockerImageFunctionBuilder(name: string) =
    do
        if String.IsNullOrWhiteSpace(name) then
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
          Props = props
          Function = null }

    /// <summary>Sets the construct ID for the Docker Lambda function.</summary>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     constructId "MyDockerFunctionConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DockerImageFunctionConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the Docker image path for the function.</summary>
    /// <param name="imagePath">The path to the Docker image directory.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     code "./docker"
    /// }
    /// </code>
    // Use the same operation name as regular Lambda to keep DSL consistent
    [<CustomOperation("code")>]
    member _.ImageAsset(config: DockerImageFunctionConfig, imagePath: string) = { config with Code = Some imagePath }

    /// <summary>Sets environment variables for the Docker Lambda function.</summary>
    /// <param name="vars">Sequence of key-value pairs for environment variables.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     environment [ "API_URL", "https://api.example.com"; "DEBUG", "true" ]
    /// }
    /// </code>
    [<CustomOperation("environment")>]
    member _.Environment(config: DockerImageFunctionConfig, vars: (string * string) seq) =
        { config with Environment = vars }

    /// <summary>Sets the timeout for the Docker Lambda function.</summary>
    /// <param name="seconds">The timeout in seconds.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     timeout 60.0
    /// }
    /// </code>
    [<CustomOperation("timeout")>]
    member _.Timeout(config: DockerImageFunctionConfig, seconds: float) = { config with Timeout = Some seconds }

    /// <summary>Sets the memory allocation for the Docker Lambda function.</summary>
    /// <param name="mb">The memory size in megabytes.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     memorySize 1024
    /// }
    /// </code>
    [<CustomOperation("memorySize")>]
    member _.MemorySize(config: DockerImageFunctionConfig, mb: int) = { config with Memory = Some mb }

    /// <summary>Sets the description for the Docker Lambda function.</summary>
    /// <param name="desc">The function description.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyFunction" {
    ///     description "Image processing service"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: DockerImageFunctionConfig, desc: string) = { config with Description = Some desc }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module DockerImageFunctionBuilders =
    /// <summary>Creates a Lambda function from a Docker image.</summary>
    /// <param name="name">The function name.</param>
    /// <code lang="fsharp">
    /// dockerImageFunction "MyDockerFunction" {
    ///     code "./docker"
    ///     timeout 60.0
    ///     memorySize 1024
    /// }
    /// </code>
    let dockerImageFunction name = DockerImageFunctionBuilder(name)
