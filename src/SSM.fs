namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SSM

/// <summary>
/// High-level Systems Manager Parameter Store builder following AWS best practices.
///
/// **Default Settings:**
/// - Type = String (most common)
/// - Tier = Standard (free, up to 10,000 parameters)
///
/// **Rationale:**
/// Parameter Store provides centralized configuration management.
/// String type covers most use cases (connection strings, URLs, etc.).
/// Standard tier is free and sufficient for most applications.
///
/// **Use Cases:**
/// - Application configuration
/// - Database connection strings
/// - API endpoints and keys
/// - Feature flags
///
/// **Escape Hatch:**
/// Access the underlying CDK StringParameter via the `Parameter` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type SSMParameterConfig =
    { ParameterName: string
      ConstructId: string option
      StringValue: string option
      Description: string option
      ParameterTier: ParameterTier option
      AllowedPattern: string option }

type SSMParameterSpec =
    {
        ParameterName: string
        ConstructId: string
        /// The underlying CDK StringParameter construct
        Props: StringParameterProps
        mutable Parameter: StringParameter option
    }

type SSMParameterBuilder(name: string) =
    member _.Yield(_: unit) : SSMParameterConfig =
        { ParameterName = name
          ConstructId = None
          StringValue = None
          Description = None
          ParameterTier = Some ParameterTier.STANDARD
          AllowedPattern = None }

    member _.Zero() : SSMParameterConfig =
        { ParameterName = name
          ConstructId = None
          StringValue = None
          Description = None
          ParameterTier = Some ParameterTier.STANDARD
          AllowedPattern = None }

    member _.Combine(state1: SSMParameterConfig, state2: SSMParameterConfig) : SSMParameterConfig =
        { ParameterName = state2.ParameterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          StringValue = state2.StringValue |> Option.orElse state1.StringValue
          Description = state2.Description |> Option.orElse state1.Description
          ParameterTier = state2.ParameterTier |> Option.orElse state1.ParameterTier
          AllowedPattern = state2.AllowedPattern |> Option.orElse state1.AllowedPattern }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SSMParameterConfig) : SSMParameterConfig = f ()

    member inline x.For
        (
            config: SSMParameterConfig,
            [<InlineIfLambda>] f: unit -> SSMParameterConfig
        ) : SSMParameterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SSMParameterConfig) : SSMParameterSpec =
        let parameterName = config.ParameterName
        let constructId = config.ConstructId |> Option.defaultValue parameterName

        let props = StringParameterProps()
        props.ParameterName <- parameterName

        match config.StringValue with
        | Some value -> props.StringValue <- value
        | None -> failwith "StringValue is required for SSM Parameter"

        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.ParameterTier |> Option.iter (fun v -> props.Tier <- v)
        config.AllowedPattern |> Option.iter (fun v -> props.AllowedPattern <- v)

        { ParameterName = parameterName
          ConstructId = constructId
          Props = props
          Parameter = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SSMParameterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("stringValue")>]
    member _.StringValue(config: SSMParameterConfig, value: string) =
        { config with StringValue = Some value }

    [<CustomOperation("description")>]
    member _.Description(config: SSMParameterConfig, description: string) =
        { config with
            Description = Some description }

    [<CustomOperation("tier")>]
    member _.Tier(config: SSMParameterConfig, tier: ParameterTier) =
        { config with
            ParameterTier = Some tier }

    [<CustomOperation("allowedPattern")>]
    member _.AllowedPattern(config: SSMParameterConfig, pattern: string) =
        { config with
            AllowedPattern = Some pattern }

/// <summary>
/// High-level Systems Manager Document builder.
///
/// **Default Settings:**
/// - Document type = Command
/// - Document format = JSON
///
/// **Rationale:**
/// SSM Documents define actions that Systems Manager performs on managed instances.
/// Command documents are most common for remote execution.
///
/// **Use Cases:**
/// - Remote command execution
/// - Automated patching
/// - Configuration enforcement
/// - Inventory collection
/// </summary>
type SSMDocumentConfig =
    { DocumentName: string
      ConstructId: string option
      Content: obj option
      DocumentType: string option
      DocumentFormat: string option
      TargetType: string option }

type SSMDocumentSpec =
    {
        DocumentName: string
        ConstructId: string
        /// The underlying CDK CfnDocument construct
        mutable Document: CfnDocument option
    }

type SSMDocumentBuilder(name: string) =
    member _.Yield(_: unit) : SSMDocumentConfig =
        { DocumentName = name
          ConstructId = None
          Content = None
          DocumentType = Some "Command"
          DocumentFormat = Some "JSON"
          TargetType = None }

    member _.Zero() : SSMDocumentConfig =
        { DocumentName = name
          ConstructId = None
          Content = None
          DocumentType = Some "Command"
          DocumentFormat = Some "JSON"
          TargetType = None }

    member _.Combine(state1: SSMDocumentConfig, state2: SSMDocumentConfig) : SSMDocumentConfig =
        { DocumentName = state2.DocumentName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Content = state2.Content |> Option.orElse state1.Content
          DocumentType = state2.DocumentType |> Option.orElse state1.DocumentType
          DocumentFormat = state2.DocumentFormat |> Option.orElse state1.DocumentFormat
          TargetType = state2.TargetType |> Option.orElse state1.TargetType }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SSMDocumentConfig) : SSMDocumentConfig = f ()

    member inline x.For
        (
            config: SSMDocumentConfig,
            [<InlineIfLambda>] f: unit -> SSMDocumentConfig
        ) : SSMDocumentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SSMDocumentConfig) : SSMDocumentSpec =
        let documentName = config.DocumentName
        let constructId = config.ConstructId |> Option.defaultValue documentName

        let props = CfnDocumentProps()
        props.Name <- documentName

        match config.Content with
        | Some content -> props.Content <- content
        | None -> failwith "Content is required for SSM Document"

        config.DocumentType |> Option.iter (fun v -> props.DocumentType <- v)
        config.DocumentFormat |> Option.iter (fun v -> props.DocumentFormat <- v)
        config.TargetType |> Option.iter (fun v -> props.TargetType <- v)

        { DocumentName = documentName
          ConstructId = constructId
          Document = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SSMDocumentConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("content")>]
    member _.Content(config: SSMDocumentConfig, content: obj) = { config with Content = Some content }

    [<CustomOperation("documentType")>]
    member _.DocumentType(config: SSMDocumentConfig, docType: string) =
        { config with
            DocumentType = Some docType }

    [<CustomOperation("documentFormat")>]
    member _.DocumentFormat(config: SSMDocumentConfig, format: string) =
        { config with
            DocumentFormat = Some format }

    [<CustomOperation("targetType")>]
    member _.TargetType(config: SSMDocumentConfig, targetType: string) =
        { config with
            TargetType = Some targetType }

/// Helper functions for SSM operations
module SSMHelpers =

    /// Creates a simple Run Command document for PowerShell
    let powerShellRunCommand (commands: string list) =
        {| schemaVersion = "2.2"
           description = "Execute PowerShell commands"
           mainSteps =
            [| {| action = "aws:runPowerShellScript"
                  name = "runCommands"
                  inputs = {| runCommand = commands |> Array.ofList |} |} |] |}
        :> obj

    /// Creates a simple Run Command document for Shell script
    let shellRunCommand (commands: string list) =
        {| schemaVersion = "2.2"
           description = "Execute shell commands"
           mainSteps =
            [| {| action = "aws:runShellScript"
                  name = "runCommands"
                  inputs = {| runCommand = commands |> Array.ofList |} |} |] |}
        :> obj

[<AutoOpen>]
module SSMBuilders =
    /// <summary>
    /// Creates a new SSM Parameter Store parameter builder.
    /// Example: ssmParameter "/myapp/config/dbhost" { stringValue "localhost"; description "Database host" }
    /// </summary>
    let ssmParameter name = SSMParameterBuilder(name)

    /// <summary>
    /// Creates a new SSM Document builder.
    /// Example: ssmDocument "InstallSoftware" { content (powerShellRunCommand ["choco install nodejs"]) }
    /// </summary>
    let ssmDocument name = SSMDocumentBuilder(name)
