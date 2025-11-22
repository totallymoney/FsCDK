namespace FsCDK

open System
open Amazon.CDK
open Amazon.CDK.AWS.S3

type TransitionConfig =
    { StorageClass: StorageClass option
      TransitionAfter: Duration option
      TransitionDate: DateTime option }

type TransitionBuilder() =
    member _.Yield(_: unit) : TransitionConfig =
        { StorageClass = None
          TransitionAfter = None
          TransitionDate = None }

    member _.Zero() : TransitionConfig =
        { StorageClass = None
          TransitionAfter = None
          TransitionDate = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> TransitionConfig) : TransitionConfig = f ()

    member _.Combine(state1: TransitionConfig, state2: TransitionConfig) : TransitionConfig =
        { StorageClass = state2.StorageClass |> Option.orElse state1.StorageClass
          TransitionAfter = state2.TransitionAfter |> Option.orElse state1.TransitionAfter
          TransitionDate = state2.TransitionDate |> Option.orElse state1.TransitionDate }

    member x.For(config: TransitionConfig, f: unit -> TransitionConfig) : TransitionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: TransitionConfig) : ITransition =
        let transition = Transition()

        match config.StorageClass with
        | Some sc -> transition.StorageClass <- sc
        | None -> failwith "storageClass is required for S3 transition rule"

        config.TransitionAfter |> Option.iter (fun d -> transition.TransitionAfter <- d)
        config.TransitionDate |> Option.iter (fun d -> transition.TransitionDate <- d)

        transition

    /// <summary>
    /// Sets the storage class to transition to.
    /// Common classes: GLACIER (low-cost archival), DEEP_ARCHIVE (lowest cost, rare access),
    /// INTELLIGENT_TIERING (automatic cost optimization), GLACIER_IR (instant retrieval).
    /// </summary>
    /// <param name="storageClass">The target storage class.</param>
    [<CustomOperation("storageClass")>]
    member _.StorageClass(config: TransitionConfig, storageClass: StorageClass) =
        { config with
            StorageClass = Some storageClass }

    /// <summary>
    /// Sets when objects transition after creation (use Duration.Days()).
    /// Example: transitionAfter (Duration.Days(90.0)) moves objects after 90 days.
    /// </summary>
    /// <param name="duration">Time after object creation to transition.</param>
    [<CustomOperation("transitionAfter")>]
    member _.TransitionAfter(config: TransitionConfig, duration: Duration) =
        { config with
            TransitionAfter = Some duration }

    /// <summary>
    /// Sets a specific date when objects should transition.
    /// Use this for one-time transitions on a specific date.
    /// </summary>
    /// <param name="date">The date to perform the transition.</param>
    [<CustomOperation("transitionDate")>]
    member _.TransitionDate(config: TransitionConfig, date: DateTime) =
        { config with
            TransitionDate = Some date }

type NoncurrentVersionTransitionConfig =
    { StorageClass: StorageClass option
      TransitionAfter: Duration option
      NoncurrentVersionsToRetain: float option }

type NoncurrentVersionTransitionBuilder() =
    member _.Yield(_: unit) : NoncurrentVersionTransitionConfig =
        { StorageClass = None
          TransitionAfter = None
          NoncurrentVersionsToRetain = None }

    member _.Zero() : NoncurrentVersionTransitionConfig =
        { StorageClass = None
          TransitionAfter = None
          NoncurrentVersionsToRetain = None }

    member inline _.Delay
        ([<InlineIfLambda>] f: unit -> NoncurrentVersionTransitionConfig)
        : NoncurrentVersionTransitionConfig =
        f ()

    member _.Combine
        (
            state1: NoncurrentVersionTransitionConfig,
            state2: NoncurrentVersionTransitionConfig
        ) : NoncurrentVersionTransitionConfig =
        { StorageClass = state2.StorageClass |> Option.orElse state1.StorageClass
          TransitionAfter = state2.TransitionAfter |> Option.orElse state1.TransitionAfter
          NoncurrentVersionsToRetain =
            state2.NoncurrentVersionsToRetain
            |> Option.orElse state1.NoncurrentVersionsToRetain }

    member inline x.For
        (
            config: NoncurrentVersionTransitionConfig,
            [<InlineIfLambda>] f: unit -> NoncurrentVersionTransitionConfig
        ) : NoncurrentVersionTransitionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: NoncurrentVersionTransitionConfig) : INoncurrentVersionTransition =
        let transition = NoncurrentVersionTransition()

        match config.StorageClass with
        | Some sc -> transition.StorageClass <- sc
        | None -> failwith "storageClass is required for S3 noncurrent version transition rule"

        config.TransitionAfter |> Option.iter (fun d -> transition.TransitionAfter <- d)

        config.NoncurrentVersionsToRetain
        |> Option.iter (fun n -> transition.NoncurrentVersionsToRetain <- n)

        transition :> INoncurrentVersionTransition

    /// <summary>
    /// Sets the storage class to transition noncurrent versions to.
    /// Use GLACIER or DEEP_ARCHIVE for old versions to reduce costs.
    /// </summary>
    /// <param name="storageClass">The target storage class.</param>
    [<CustomOperation("storageClass")>]
    member _.StorageClass(config: NoncurrentVersionTransitionConfig, storageClass: StorageClass) =
        { config with
            StorageClass = Some storageClass }

    /// <summary>
    /// Sets when noncurrent versions transition after becoming noncurrent.
    /// Example: transitionAfter (Duration.Days(30.0)) moves old versions after 30 days.
    /// </summary>
    /// <param name="duration">Time after version becomes noncurrent to transition.</param>
    [<CustomOperation("transitionAfter")>]
    member _.TransitionAfter(config: NoncurrentVersionTransitionConfig, duration: Duration) =
        { config with
            TransitionAfter = Some duration }

    /// <summary>
    /// Sets the number of noncurrent versions to retain before transitioning.
    /// Example: noncurrentVersionsToRetain 3.0 keeps the 3 most recent noncurrent versions.
    /// </summary>
    /// <param name="count">Number of noncurrent versions to keep in standard storage.</param>
    [<CustomOperation("noncurrentVersionsToRetain")>]
    member _.NoncurrentVersionsToRetain(config: NoncurrentVersionTransitionConfig, count: float) =
        { config with
            NoncurrentVersionsToRetain = Some count }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module S3TransitionBuilders =
    /// <summary>
    /// Creates an S3 lifecycle transition rule for moving objects to different storage classes.
    /// Transitions reduce storage costs by automatically moving objects to cheaper storage tiers.
    /// </summary>
    /// <code lang="fsharp">
    /// transition {
    ///     storageClass StorageClass.GLACIER
    ///     transitionAfter (Duration.Days(90.0))
    /// }
    /// </code>
    let transition = TransitionBuilder()

    /// <summary>
    /// Creates an S3 lifecycle transition rule for noncurrent (versioned) objects.
    /// Use this when versioning is enabled to transition old versions to cheaper storage.
    /// </summary>
    /// <code lang="fsharp">
    /// noncurrentVersionTransition {
    ///     storageClass StorageClass.GLACIER
    ///     transitionAfter (Duration.Days(30.0))
    ///     noncurrentVersionsToRetain 3.0
    /// }
    /// </code>
    let noncurrentVersionTransition = NoncurrentVersionTransitionBuilder()
