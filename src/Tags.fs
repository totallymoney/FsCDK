namespace FsCDK

open Amazon.CDK
open Constructs

/// <summary>
/// Global tagging helpers for consistent resource tagging across stacks.
///
/// **Rationale:**
/// - Tags enable cost allocation and resource organization
/// - Consistent tagging simplifies governance and compliance
/// - Tags help with automation and resource discovery
/// - Standard tags improve operational visibility
///
/// **Best Practices:**
/// - Apply tags at stack level for inheritance
/// - Use consistent tag names across organization
/// - Include: project, environment, owner, cost-center
/// - Avoid PII or sensitive data in tags
/// </summary>
module Tags =

    /// <summary>
    /// Standard tag set for FsCDK resources
    /// </summary>
    type StandardTags =
        { Project: string option
          Environment: string option
          Owner: string option
          CreatedBy: string
          CostCenter: string option
          ManagedBy: string }

    /// <summary>
    /// Default tags applied to all FsCDK resources
    /// </summary>
    let defaultTags =
        { Project = None
          Environment = None
          Owner = None
          CreatedBy = "FsCDK"
          CostCenter = None
          ManagedBy = "FsCDK" }

    /// <summary>
    /// Applies standard tags to a construct (stack or resource)
    /// </summary>
    let applyStandardTags (construct: IConstruct) (tags: StandardTags) =
        tags.Project |> Option.iter (fun v -> Tags.Of(construct).Add("Project", v))

        tags.Environment
        |> Option.iter (fun v -> Tags.Of(construct).Add("Environment", v))

        tags.Owner |> Option.iter (fun v -> Tags.Of(construct).Add("Owner", v))
        Tags.Of(construct).Add("CreatedBy", tags.CreatedBy)

        tags.CostCenter
        |> Option.iter (fun v -> Tags.Of(construct).Add("CostCenter", v))

        Tags.Of(construct).Add("ManagedBy", tags.ManagedBy)

    /// <summary>
    /// Applies custom tags to a construct
    /// </summary>
    let applyCustomTags (construct: IConstruct) (tags: (string * string) list) =
        tags |> List.iter (fun (key, value) -> Tags.Of(construct).Add(key, value))

    /// <summary>
    /// Creates standard tags from environment and project info
    /// </summary>
    let createTags (project: string) (environment: string) (owner: string option) =
        { defaultTags with
            Project = Some project
            Environment = Some environment
            Owner = owner }

    /// <summary>
    /// Applies tags to a stack with standard conventions
    /// </summary>
    let tagStack (stack: Stack) (project: string) (environment: string) (owner: string option) =
        let tags = createTags project environment owner
        applyStandardTags stack tags

    /// <summary>
    /// Removes a tag from a construct
    /// </summary>
    let removeTag (construct: IConstruct) (key: string) = Tags.Of(construct).Remove(key)

    /// <summary>
    /// Removes all FsCDK standard tags from a construct
    /// </summary>
    let removeStandardTags (construct: IConstruct) =
        removeTag construct "Project"
        removeTag construct "Environment"
        removeTag construct "Owner"
        removeTag construct "CreatedBy"
        removeTag construct "CostCenter"
        removeTag construct "ManagedBy"

/// <summary>
/// Tag builder for fluent tag creation
/// </summary>
type TagBuilder() =
    let mutable project: string option = None
    let mutable environment: string option = None
    let mutable owner: string option = None
    let mutable costCenter: string option = None
    let mutable customTags: (string * string) list = []

    member _.Project(p: string) =
        project <- Some p
        ()

    member _.Environment(e: string) =
        environment <- Some e
        ()

    member _.Owner(o: string) =
        owner <- Some o
        ()

    member _.CostCenter(cc: string) =
        costCenter <- Some cc
        ()

    member _.AddTag(key: string, value: string) =
        customTags <- (key, value) :: customTags
        ()

    member _.Build() =
        { Tags.StandardTags.Project = project
          Tags.StandardTags.Environment = environment
          Tags.StandardTags.Owner = owner
          Tags.StandardTags.CreatedBy = "FsCDK"
          Tags.StandardTags.CostCenter = costCenter
          Tags.StandardTags.ManagedBy = "FsCDK" },
        customTags

    member this.ApplyTo(construct: IConstruct) =
        let standardTags, custom = this.Build()
        Tags.applyStandardTags construct standardTags
        Tags.applyCustomTags construct custom
