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
      DataType: ParameterDataType option
      Description: string option
      SimpleName: bool option
      Tier: ParameterTier option
      AllowedPattern: string option }

type SSMParameterSpec =
    { ParameterName: string
      ConstructId: string
      Props: StringParameterProps
      mutable Parameter: StringParameter option }

type SSMParameterBuilder(name: string) =
    member _.Yield(_: unit) : SSMParameterConfig =
        { ParameterName = name
          ConstructId = None
          StringValue = None
          DataType = None
          Description = None
          SimpleName = None
          Tier = Some ParameterTier.STANDARD
          AllowedPattern = None }

    member _.Zero() : SSMParameterConfig =
        { ParameterName = name
          ConstructId = None
          StringValue = None
          DataType = None
          Description = None
          SimpleName = None
          Tier = Some ParameterTier.STANDARD
          AllowedPattern = None }

    member _.Combine(state1: SSMParameterConfig, state2: SSMParameterConfig) : SSMParameterConfig =
        { ParameterName = state2.ParameterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          StringValue = state2.StringValue |> Option.orElse state1.StringValue
          DataType = state2.DataType |> Option.orElse state1.DataType
          Description = state2.Description |> Option.orElse state1.Description
          SimpleName = state2.SimpleName |> Option.orElse state1.SimpleName
          Tier = state2.Tier |> Option.orElse state1.Tier
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
        config.Tier |> Option.iter (fun v -> props.Tier <- v)
        config.AllowedPattern |> Option.iter (fun v -> props.AllowedPattern <- v)

        config.DataType |> Option.iter (fun v -> props.DataType <- v)

        config.SimpleName |> Option.iter (fun v -> props.SimpleName <- v)



        { ParameterName = parameterName
          ConstructId = constructId
          Props = props
          Parameter = None }

    /// <summary>Sets the construct ID for the underlying CDK StringParameter construct.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="id">The construct ID to set</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     constructId "DbHost"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SSMParameterConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the string value for the SSM Parameter.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="value">The string value to store</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     stringValue "localhost"
    /// }
    /// </code>
    [<CustomOperation("stringValue")>]
    member _.StringValue(config: SSMParameterConfig, value: string) =
        { config with StringValue = Some value }

    /// <summary>Sets the description for the SSM Parameter.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="description">The description to store</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     description "Database host"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: SSMParameterConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets the data type for the SSM Parameter.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="dataType">The data type to store</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     dataType ParameterDataType.String
    /// }
    /// </code>
    [<CustomOperation("dataType")>]
    member _.DataType(config: SSMParameterConfig, dataType: ParameterDataType) =
        { config with DataType = Some dataType }

    /// <summary>Sets whether the SSM Parameter should be a simple name.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="simpleName">Whether the SSM Parameter should be a simple name</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     simpleName true
    /// }
    /// </code>
    [<CustomOperation("simpleName")>]
    member _.SimpleName(config: SSMParameterConfig, simpleName: bool) =
        { config with
            SimpleName = Some simpleName }

    /// <summary>Sets the parameter tier for the SSM Parameter.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="tier">The parameter tier to store</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     tier ParameterTier.Advanced
    /// }
    /// </code>
    [<CustomOperation("tier")>]
    member _.Tier(config: SSMParameterConfig, tier: ParameterTier) = { config with Tier = Some tier }

    /// <summary>Sets the allowed pattern for the SSM Parameter.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="pattern">The allowed pattern to store</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     allowedPattern "^[a-z0-9]+$"
    /// }
    /// </code>
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
      Attachments: obj option
      Requires: obj option
      Tags: ICfnTag list
      DocumentType: string option
      DocumentFormat: string option
      UpdateMethod: string option
      VersionName: string option
      TargetType: string option }

type SSMDocumentSpec =
    { DocumentName: string
      ConstructId: string
      Props: CfnDocumentProps
      mutable Document: CfnDocument option }

type SSMDocumentBuilder(name: string) =
    member _.Yield(_: unit) : SSMDocumentConfig =
        { DocumentName = name
          ConstructId = None
          Content = None
          Attachments = None
          Requires = None
          Tags = []
          DocumentType = Some "Command"
          DocumentFormat = Some "JSON"
          UpdateMethod = None
          VersionName = None
          TargetType = None }

    member _.Zero() : SSMDocumentConfig =
        { DocumentName = name
          ConstructId = None
          Content = None
          Attachments = None
          Requires = None
          Tags = []
          DocumentType = Some "Command"
          DocumentFormat = Some "JSON"
          UpdateMethod = None
          VersionName = None
          TargetType = None }

    member _.Combine(state1: SSMDocumentConfig, state2: SSMDocumentConfig) : SSMDocumentConfig =
        { DocumentName = state2.DocumentName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Content = state2.Content |> Option.orElse state1.Content
          Attachments = state2.Attachments |> Option.orElse state1.Attachments
          Requires = state2.Requires |> Option.orElse state1.Requires
          Tags = state2.Tags @ state1.Tags
          DocumentType = state2.DocumentType |> Option.orElse state1.DocumentType
          DocumentFormat = state2.DocumentFormat |> Option.orElse state1.DocumentFormat
          UpdateMethod = state2.UpdateMethod |> Option.orElse state1.UpdateMethod
          VersionName = state2.VersionName |> Option.orElse state1.VersionName
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

        if not (List.isEmpty config.Tags) then
            props.Tags <- config.Tags |> List.toArray

        config.Attachments |> Option.iter (fun v -> props.Attachments <- v)

        config.Requires |> Option.iter (fun v -> props.Requires <- v)

        config.UpdateMethod |> Option.iter (fun v -> props.UpdateMethod <- v)

        config.VersionName |> Option.iter (fun v -> props.VersionName <- v)

        match config.Content with
        | Some content -> props.Content <- content
        | None -> failwith "Content is required for SSM Document"

        config.DocumentType |> Option.iter (fun v -> props.DocumentType <- v)
        config.DocumentFormat |> Option.iter (fun v -> props.DocumentFormat <- v)
        config.TargetType |> Option.iter (fun v -> props.TargetType <- v)

        { DocumentName = documentName
          ConstructId = constructId
          Props = props
          Document = None }

    /// <summary>Sets the construct ID for the underlying CDK CfnDocument construct.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="id">The construct ID to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	constructId "InstallSoftware"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SSMDocumentConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the content for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="content">The content to store</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	content (powerShellRunCommand ["choco install nodejs"])
    /// }
    /// </code>
    [<CustomOperation("content")>]
    member _.Content(config: SSMDocumentConfig, content: obj) = { config with Content = Some content }

    /// <summary>Sets the document type and format for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="docType">The document type to store</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    ///     documentType "Automation"
    /// }
    /// </code>
    [<CustomOperation("documentType")>]
    member _.DocumentType(config: SSMDocumentConfig, docType: string) =
        { config with
            DocumentType = Some docType }

    /// <summary>Sets the document format for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="format">The document format to store</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	documentFormat "YAML"
    /// }
    /// </code>
    [<CustomOperation("documentFormat")>]
    member _.DocumentFormat(config: SSMDocumentConfig, format: string) =
        { config with
            DocumentFormat = Some format }

    /// <summary>Sets the target type for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="targetType">The target type to store</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	targetType "/AWS::EC2::Instance"
    /// }
    /// </code>
    [<CustomOperation("targetType")>]
    member _.TargetType(config: SSMDocumentConfig, targetType: string) =
        { config with
            TargetType = Some targetType }

    /// <summary>Adds a tag to the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="tag">The tag to add</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	tag (CfnTag(Key= "Environment", Value="Production"))
    /// }
    /// </code>
    [<CustomOperation("tag")>]
    member _.Tag(config: SSMDocumentConfig, tag: ICfnTag) =
        { config with
            Tags = tag :: config.Tags }

    /// <summary>Sets tags for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="tags">The tags to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	tags [CfnTag(Key= "Environment", Value="Production")]
    /// }
    /// </code>
    [<CustomOperation("tags")>]
    member _.Tags(config: SSMDocumentConfig, tags: ICfnTag list) = { config with Tags = tags }

    /// <summary>Sets the attachments for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="attachments">The attachments to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    ///    attachments myAttachments
    /// }
    /// </code>
    [<CustomOperation("attachments")>]
    member _.Attachments(config: SSMDocumentConfig, attachments: obj) =
        { config with
            Attachments = Some attachments }

    /// <summary>Sets the requires for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="requires">The requires to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    ///    requires myRequires
    /// }
    /// </code>
    [<CustomOperation("requires")>]
    member _.Requires(config: SSMDocumentConfig, requires: obj) =
        { config with Requires = Some requires }

    /// <summary>Sets the update method for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="updateMethod">The update method to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	updateMethod "CreateUpdate"
    /// }
    /// </code>
    [<CustomOperation("updateMethod")>]
    member _.UpdateMethod(config: SSMDocumentConfig, updateMethod: string) =
        { config with
            UpdateMethod = Some updateMethod }

    /// <summary>Sets the version name for the SSM Document.</summary>
    /// <param name="config">The current builder state</param>
    /// <param name="versionName">The version name to set</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    /// 	versionName "1.0"
    /// }
    /// </code>
    [<CustomOperation("versionName")>]
    member _.VersionName(config: SSMDocumentConfig, versionName: string) =
        { config with
            VersionName = Some versionName }

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

    /// Creates a simple Run Command document for a Shell script
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
    /// <summary>Creates a new SSM Parameter Store parameter builder.</summary>
    /// <param name="name">The name of the parameter to create</param>
    /// <code lang="fsharp">
    /// ssmParameter "/myapp/config/dbhost" {
    ///     stringValue "localhost"
    ///     description "Database host"
    /// }
    /// </code>
    let ssmParameter name = SSMParameterBuilder(name)

    /// <summary>Creates a new SSM Document builder.</summary>
    /// <param name="name">The name of the document to create</param>
    /// <code lang="fsharp">
    /// ssmDocument "InstallSoftware" {
    ///     content (powerShellRunCommand ["choco install nodejs"])
    /// }
    /// </code>
    let ssmDocument name = SSMDocumentBuilder(name)
