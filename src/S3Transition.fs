namespace FsCDK

open System
open Amazon.CDK
open Amazon.CDK.AWS.S3

type TransitionConfig =
    { StorageClass: StorageClass option
      TransitionAfter: Duration option
      TransitionDate: DateTime option }

type TransitionBuilder() =
    member _.Yield _ : TransitionConfig =
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

    [<CustomOperation("storageClass")>]
    member _.StorageClass(config: TransitionConfig, storageClass: StorageClass) =
        { config with
            StorageClass = Some storageClass }

    [<CustomOperation("transitionAfter")>]
    member _.TransitionAfter(config: TransitionConfig, duration: Duration) =
        { config with
            TransitionAfter = Some duration }

    [<CustomOperation("transitionDate")>]
    member _.TransitionDate(config: TransitionConfig, date: DateTime) =
        { config with
            TransitionDate = Some date }

type NoncurrentVersionTransitionConfig =
    { StorageClass: StorageClass option
      TransitionAfter: Duration option
      NoncurrentVersionsToRetain: float option }

type NoncurrentVersionTransitionBuilder() =
    member _.Yield _ : NoncurrentVersionTransitionConfig =
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

    [<CustomOperation("storageClass")>]
    member _.StorageClass(config: NoncurrentVersionTransitionConfig, storageClass: StorageClass) =
        { config with
            StorageClass = Some storageClass }

    [<CustomOperation("transitionAfter")>]
    member _.TransitionAfter(config: NoncurrentVersionTransitionConfig, duration: Duration) =
        { config with
            TransitionAfter = Some duration }

    [<CustomOperation("noncurrentVersionsToRetain")>]
    member _.NoncurrentVersionsToRetain(config: NoncurrentVersionTransitionConfig, count: float) =
        { config with
            NoncurrentVersionsToRetain = Some count }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module S3TransitionBuilders =
    let transition = TransitionBuilder()
    let noncurrentVersionTransition = NoncurrentVersionTransitionBuilder()
