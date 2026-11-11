namespace FsCDK

open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.S3.Deployment

// ============================================================================
// S3 Bucket Deployment Configuration DSL
// ============================================================================

/// <summary>
/// Uploads local files (e.g. a built single-page app) to an S3 bucket during deployment
/// and optionally invalidates a CloudFront distribution afterwards.
///
/// **Defaults:**
/// - Prune = true (objects not in the sources are deleted from the destination)
/// - Memory = 512 MB for the upload Lambda (the CDK default of 128 MB is slow for larger apps)
/// - Distribution paths = "/*" when a distribution is set
/// </summary>
type BucketDeploymentConfig =
    { DeploymentName: string
      ConstructId: string option
      Sources: ISource list
      DestinationBucket: IBucket option
      DestinationKeyPrefix: string option
      Distribution: IDistribution option
      DistributionPaths: string list
      Prune: bool option
      RetainOnDelete: bool option
      MemoryLimit: int option
      CacheControl: CacheControl list
      Exclude: string list
      LogGroup: ILogGroup option }

type BucketDeploymentSpec =
    { DeploymentName: string
      ConstructId: string
      Props: BucketDeploymentProps
      mutable Deployment: BucketDeployment option }

type BucketDeploymentBuilder(name: string) =
    member _.Yield(_: unit) : BucketDeploymentConfig =
        { DeploymentName = name
          ConstructId = None
          Sources = []
          DestinationBucket = None
          DestinationKeyPrefix = None
          Distribution = None
          DistributionPaths = []
          Prune = None
          RetainOnDelete = None
          MemoryLimit = None
          CacheControl = []
          Exclude = []
          LogGroup = None }

    member x.Yield(source: ISource) : BucketDeploymentConfig =
        { x.Yield(()) with
            Sources = [ source ] }

    member x.Zero() : BucketDeploymentConfig = x.Yield(())

    member inline _.Delay([<InlineIfLambda>] f: unit -> BucketDeploymentConfig) : BucketDeploymentConfig = f ()

    member _.Combine(state1: BucketDeploymentConfig, state2: BucketDeploymentConfig) : BucketDeploymentConfig =
        { DeploymentName = state1.DeploymentName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Sources = state1.Sources @ state2.Sources
          DestinationBucket = state2.DestinationBucket |> Option.orElse state1.DestinationBucket
          DestinationKeyPrefix = state2.DestinationKeyPrefix |> Option.orElse state1.DestinationKeyPrefix
          Distribution = state2.Distribution |> Option.orElse state1.Distribution
          DistributionPaths = state1.DistributionPaths @ state2.DistributionPaths
          Prune = state2.Prune |> Option.orElse state1.Prune
          RetainOnDelete = state2.RetainOnDelete |> Option.orElse state1.RetainOnDelete
          MemoryLimit = state2.MemoryLimit |> Option.orElse state1.MemoryLimit
          CacheControl = state1.CacheControl @ state2.CacheControl
          Exclude = state1.Exclude @ state2.Exclude
          LogGroup = state2.LogGroup |> Option.orElse state1.LogGroup }

    member inline x.For
        (
            config: BucketDeploymentConfig,
            [<InlineIfLambda>] f: unit -> BucketDeploymentConfig
        ) : BucketDeploymentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: BucketDeploymentConfig) : BucketDeploymentSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.DeploymentName
        let props = BucketDeploymentProps()

        props.DestinationBucket <-
            match config.DestinationBucket with
            | Some b -> b
            | None ->
                invalidArg "destinationBucket" $"BucketDeployment '{config.DeploymentName}' needs a destinationBucket."

        if config.Sources.IsEmpty then
            invalidArg "source" $"BucketDeployment '{config.DeploymentName}' needs at least one source."

        props.Sources <- config.Sources |> Array.ofList
        props.Prune <- config.Prune |> Option.defaultValue true
        props.MemoryLimit <- config.MemoryLimit |> Option.defaultValue 512 |> float

        config.DestinationKeyPrefix
        |> Option.iter (fun p -> props.DestinationKeyPrefix <- p)

        config.RetainOnDelete |> Option.iter (fun r -> props.RetainOnDelete <- r)
        config.LogGroup |> Option.iter (fun lg -> props.LogGroup <- lg)

        config.Distribution
        |> Option.iter (fun d ->
            props.Distribution <- d

            props.DistributionPaths <-
                if config.DistributionPaths.IsEmpty then
                    [| "/*" |]
                else
                    Array.ofList config.DistributionPaths)

        if not config.CacheControl.IsEmpty then
            props.CacheControl <- Array.ofList config.CacheControl

        if not config.Exclude.IsEmpty then
            props.Exclude <- Array.ofList config.Exclude

        { DeploymentName = config.DeploymentName
          ConstructId = constructId
          Props = props
          Deployment = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: BucketDeploymentConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Adds a source (Source.Asset, Source.Data, Source.JsonData...).</summary>
    /// <param name="source">The source.</param>
    [<CustomOperation("source")>]
    member _.Source(config: BucketDeploymentConfig, source: ISource) =
        { config with
            Sources = config.Sources @ [ source ] }

    /// <summary>Adds a local directory or zip file as a source.</summary>
    /// <param name="path">Path to the directory or zip, e.g. "./web/dist".</param>
    /// <code lang="fsharp">
    /// bucketDeployment "SiteAssets" {
    ///     sourcePath "./web/dist"
    ///     destinationBucket siteBucket
    ///     distribution cdn
    /// }
    /// </code>
    [<CustomOperation("sourcePath")>]
    member _.SourcePath(config: BucketDeploymentConfig, path: string) =
        { config with
            Sources = config.Sources @ [ Source.Asset(path) ] }

    /// <summary>Sets the destination bucket.</summary>
    /// <param name="bucket">The bucket.</param>
    [<CustomOperation("destinationBucket")>]
    member _.DestinationBucket(config: BucketDeploymentConfig, bucket: IBucket) =
        { config with
            DestinationBucket = Some bucket }

    /// <summary>Uploads under a key prefix (pruning is limited to that prefix).</summary>
    /// <param name="prefix">The prefix, e.g. "app/".</param>
    [<CustomOperation("destinationKeyPrefix")>]
    member _.DestinationKeyPrefix(config: BucketDeploymentConfig, prefix: string) =
        { config with
            DestinationKeyPrefix = Some prefix }

    /// <summary>Invalidates this distribution after the upload (paths default to "/*").</summary>
    /// <param name="distribution">The distribution.</param>
    [<CustomOperation("distribution")>]
    member _.Distribution(config: BucketDeploymentConfig, distribution: IDistribution) =
        { config with
            Distribution = Some distribution }

    /// <summary>Sets the invalidation paths.</summary>
    /// <param name="paths">Paths, e.g. [ "/index.html" ].</param>
    [<CustomOperation("distributionPaths")>]
    member _.DistributionPaths(config: BucketDeploymentConfig, paths: string list) =
        { config with
            DistributionPaths = config.DistributionPaths @ paths }

    /// <summary>Deletes destination objects that are not in the sources (default: true).</summary>
    [<CustomOperation("prune")>]
    member _.Prune(config: BucketDeploymentConfig, prune: bool) = { config with Prune = Some prune }

    /// <summary>Keeps the uploaded objects when the deployment is removed from the stack.</summary>
    [<CustomOperation("retainOnDelete")>]
    member _.RetainOnDelete(config: BucketDeploymentConfig, retain: bool) =
        { config with
            RetainOnDelete = Some retain }

    /// <summary>Sets the upload Lambda memory in MB (default: 512).</summary>
    [<CustomOperation("memoryLimit")>]
    member _.MemoryLimit(config: BucketDeploymentConfig, mb: int) = { config with MemoryLimit = Some mb }

    /// <summary>Sets Cache-Control on the uploaded objects.</summary>
    /// <param name="cacheControl">E.g. [ CacheControl.MaxAge(Duration.Days(365.0)); CacheControl.Immutable() ].</param>
    [<CustomOperation("cacheControl")>]
    member _.CacheControl(config: BucketDeploymentConfig, cacheControl: CacheControl list) =
        { config with
            CacheControl = config.CacheControl @ cacheControl }

    /// <summary>Excludes files matching glob patterns from the upload.</summary>
    [<CustomOperation("exclude")>]
    member _.Exclude(config: BucketDeploymentConfig, patterns: string list) =
        { config with
            Exclude = config.Exclude @ patterns }

    /// <summary>Sets the log group of the upload Lambda.</summary>
    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: BucketDeploymentConfig, logGroup: ILogGroup) =
        { config with LogGroup = Some logGroup }

[<AutoOpen>]
module BucketDeploymentBuilders =
    /// <summary>Creates an S3 bucket deployment.</summary>
    /// <param name="name">The deployment name.</param>
    /// <code lang="fsharp">
    /// bucketDeployment "SiteAssets" {
    ///     sourcePath "./web/dist"
    ///     destinationBucket siteBucket
    ///     distribution cdn
    /// }
    /// </code>
    let bucketDeployment (name: string) = BucketDeploymentBuilder name
