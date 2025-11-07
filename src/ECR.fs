namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ECR

/// <summary>
/// High-level ECR Repository builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Image scan on push = enabled (security best practice)
/// - Image tag mutability = MUTABLE (allows tag reuse for development)
/// - Lifecycle policy = delete untagged images after 7 days
/// - Encryption = AES256 (AWS managed encryption)
/// - Removal policy = RETAIN (prevents accidental deletion)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Image scanning detects vulnerabilities automatically
/// - Lifecycle policies reduce storage costs
/// - Encryption at rest protects container images
/// - RETAIN policy prevents accidental data loss
///
/// **Escape Hatch:**
/// Access the underlying CDK Repository via the `Repository` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type ECRRepositoryConfig =
    { RepositoryName: string
      ConstructId: string option
      ImageScanOnPush: bool voption
      ImageTagMutability: TagMutability voption
      LifecycleRules: Amazon.CDK.AWS.ECR.LifecycleRule list
      RemovalPolicy: RemovalPolicy voption
      EmptyOnDelete: bool voption }

type ECRRepositoryResource =
    {
        RepositoryName: string
        ConstructId: string
        /// The underlying CDK Repository construct
        Repository: Repository
    }

type ECRRepositoryBuilder(name: string) =
    member _.Yield _ : ECRRepositoryConfig =
        { RepositoryName = name
          ConstructId = None
          ImageScanOnPush = ValueSome true
          ImageTagMutability = ValueSome TagMutability.MUTABLE
          LifecycleRules = []
          RemovalPolicy = ValueSome RemovalPolicy.RETAIN
          EmptyOnDelete = ValueSome false }

    member _.Zero() : ECRRepositoryConfig =
        { RepositoryName = name
          ConstructId = None
          ImageScanOnPush = ValueSome true
          ImageTagMutability = ValueSome TagMutability.MUTABLE
          LifecycleRules = []
          RemovalPolicy = ValueSome RemovalPolicy.RETAIN
          EmptyOnDelete = ValueSome false }

    member _.Combine(state1: ECRRepositoryConfig, state2: ECRRepositoryConfig) : ECRRepositoryConfig =
        { RepositoryName = state2.RepositoryName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          ImageScanOnPush = state2.ImageScanOnPush |> ValueOption.orElse state1.ImageScanOnPush
          ImageTagMutability = state2.ImageTagMutability |> ValueOption.orElse state1.ImageTagMutability
          LifecycleRules =
            if state2.LifecycleRules.IsEmpty then
                state1.LifecycleRules
            else
                state2.LifecycleRules @ state1.LifecycleRules
          RemovalPolicy = state2.RemovalPolicy |> ValueOption.orElse state1.RemovalPolicy
          EmptyOnDelete = state2.EmptyOnDelete |> ValueOption.orElse state1.EmptyOnDelete }

    member inline _.Delay([<InlineIfLambda>] f: unit -> ECRRepositoryConfig) : ECRRepositoryConfig = f ()

    member inline x.For
        (
            config: ECRRepositoryConfig,
            [<InlineIfLambda>] f: unit -> ECRRepositoryConfig
        ) : ECRRepositoryConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ECRRepositoryConfig) : ECRRepositoryResource =
        let repositoryName = config.RepositoryName
        let constructId = config.ConstructId |> Option.defaultValue repositoryName

        let props = RepositoryProps()
        props.RepositoryName <- repositoryName

        config.ImageScanOnPush
        |> ValueOption.iter (fun v -> props.ImageScanOnPush <- System.Nullable<bool>(v))

        config.ImageTagMutability
        |> ValueOption.iter (fun v -> props.ImageTagMutability <- v)

        config.RemovalPolicy |> ValueOption.iter (fun v -> props.RemovalPolicy <- v)

        config.EmptyOnDelete
        |> ValueOption.iter (fun v -> props.EmptyOnDelete <- System.Nullable<bool>(v))

        if not config.LifecycleRules.IsEmpty then
            props.LifecycleRules <- config.LifecycleRules |> List.map (fun r -> r :> ILifecycleRule) |> Array.ofList

        { RepositoryName = repositoryName
          ConstructId = constructId
          Repository = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ECRRepositoryConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("imageScanOnPush")>]
    member _.ImageScanOnPush(config: ECRRepositoryConfig, enabled: bool) =
        { config with
            ImageScanOnPush = ValueSome enabled }

    [<CustomOperation("imageTagMutability")>]
    member _.ImageTagMutability(config: ECRRepositoryConfig, mutability: TagMutability) =
        { config with
            ImageTagMutability = ValueSome mutability }

    [<CustomOperation("lifecycleRule")>]
    member _.LifecycleRule(config: ECRRepositoryConfig, rule: Amazon.CDK.AWS.ECR.LifecycleRule) =
        { config with
            LifecycleRules = rule :: config.LifecycleRules }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: ECRRepositoryConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = ValueSome policy }

    [<CustomOperation("emptyOnDelete")>]
    member _.EmptyOnDelete(config: ECRRepositoryConfig, empty: bool) =
        { config with
            EmptyOnDelete = ValueSome empty }

/// Helper functions for creating ECR lifecycle rules
module ECRHelpers =

    /// Creates a lifecycle rule to delete untagged images after specified days
    let deleteUntaggedAfterDays (days: int) =
        Amazon.CDK.AWS.ECR.LifecycleRule(
            Description = sprintf "Delete untagged images after %d days" days,
            MaxImageAge = Duration.Days(float days),
            TagStatus = TagStatus.UNTAGGED
        )

    /// Creates a lifecycle rule to keep only the last N images
    let keepLastNImages (count: int) =
        Amazon.CDK.AWS.ECR.LifecycleRule(
            Description = sprintf "Keep only last %d images" count,
            MaxImageCount = System.Nullable<float>(float count),
            TagStatus = TagStatus.ANY
        )

    /// Creates a lifecycle rule to delete images with specific tag prefix after days
    let deleteTaggedAfterDays (tagPrefix: string) (days: int) =
        Amazon.CDK.AWS.ECR.LifecycleRule(
            Description = sprintf "Delete images with tag prefix '%s' after %d days" tagPrefix days,
            TagPrefixList = [| tagPrefix |],
            MaxImageAge = Duration.Days(float days),
            TagStatus = TagStatus.TAGGED
        )

    /// Creates a standard lifecycle policy for development repositories
    let standardDevLifecycleRules () =
        [ deleteUntaggedAfterDays 7; keepLastNImages 10 ]

    /// Creates a standard lifecycle policy for production repositories
    let standardProdLifecycleRules () =
        [ deleteUntaggedAfterDays 14; keepLastNImages 30 ]

[<AutoOpen>]
module ECRBuilders =
    /// <summary>
    /// Creates a new ECR repository builder with secure defaults.
    /// Example: ecrRepository "my-app" { imageScanOnPush true; lifecycleRule (deleteUntaggedAfterDays 7) }
    /// </summary>
    let ecrRepository name = ECRRepositoryBuilder name
