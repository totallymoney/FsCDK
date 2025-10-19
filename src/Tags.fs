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
        match tags.Project with
        | Some v -> Tags.Of(construct).Add("Project", v)
        | None -> ()

        match tags.Environment with
        | Some v -> Tags.Of(construct).Add("Environment", v)
        | None -> ()

        match tags.Owner with
        | Some v -> Tags.Of(construct).Add("Owner", v)
        | None -> ()

        Tags.Of(construct).Add("CreatedBy", tags.CreatedBy)

        match tags.CostCenter with
        | Some v -> Tags.Of(construct).Add("CostCenter", v)
        | None -> ()

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
/// Immutable tag builder state for fluent tag creation
/// </summary>
type TagBuilderState =
    { Project: string option
      Environment: string option
      Owner: string option
      CostCenter: string option
      CustomTags: (string * string) list }

/// <summary>
/// Tag builder for fluent tag creation using immutable state
/// </summary>
type TagBuilder(state: TagBuilderState) =
    new() =
        TagBuilder(
            { Project = None
              Environment = None
              Owner = None
              CostCenter = None
              CustomTags = [] }
        )

    member _.Project(p: string) =
        TagBuilder({ state with Project = Some p })

    member _.Environment(e: string) =
        TagBuilder({ state with Environment = Some e })

    member _.Owner(o: string) =
        TagBuilder({ state with Owner = Some o })

    member _.CostCenter(cc: string) =
        TagBuilder({ state with CostCenter = Some cc })

    member _.AddTag(key: string, value: string) =
        TagBuilder(
            { state with
                CustomTags = (key, value) :: state.CustomTags }
        )

    member _.Build() =
        { Tags.StandardTags.Project = state.Project
          Tags.StandardTags.Environment = state.Environment
          Tags.StandardTags.Owner = state.Owner
          Tags.StandardTags.CreatedBy = "FsCDK"
          Tags.StandardTags.CostCenter = state.CostCenter
          Tags.StandardTags.ManagedBy = "FsCDK" },
        state.CustomTags

    member this.ApplyTo(construct: IConstruct) =
        let standardTags, custom = this.Build()
        Tags.applyStandardTags construct standardTags
        Tags.applyCustomTags construct custom
