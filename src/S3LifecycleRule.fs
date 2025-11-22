namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.S3

type LifecycleRuleConfig =
    { AbortIncompleteMultipartUploadAfter: Duration option
      Enabled: bool option
      Expiration: Duration option
      ExpirationDate: System.DateTime option
      ExpiredObjectDeleteMarker: bool option
      Id: string option
      NoncurrentVersionExpiration: Duration option
      NoncurrentVersionsToRetain: float option
      NoncurrentVersionTransitions: INoncurrentVersionTransition list
      ObjectSizeGreaterThan: float option
      ObjectSizeLessThan: float option
      Prefix: string option
      TagFilters: System.Collections.Generic.IDictionary<string, obj> option
      Transitions: ITransition list }

type LifecycleRuleBuilder() =
    member _.Yield(transition: ITransition) : LifecycleRuleConfig =
        { AbortIncompleteMultipartUploadAfter = None
          Enabled = None
          Expiration = None
          ExpirationDate = None
          ExpiredObjectDeleteMarker = None
          Id = None
          NoncurrentVersionExpiration = None
          NoncurrentVersionsToRetain = None
          NoncurrentVersionTransitions = []
          ObjectSizeGreaterThan = None
          ObjectSizeLessThan = None
          Prefix = None
          TagFilters = None
          Transitions = [ transition ] }

    member _.Yield(noncurrentTransition: INoncurrentVersionTransition) : LifecycleRuleConfig =
        { AbortIncompleteMultipartUploadAfter = None
          Enabled = None
          Expiration = None
          ExpirationDate = None
          ExpiredObjectDeleteMarker = None
          Id = None
          NoncurrentVersionExpiration = None
          NoncurrentVersionsToRetain = None
          NoncurrentVersionTransitions = [ noncurrentTransition ]
          ObjectSizeGreaterThan = None
          ObjectSizeLessThan = None
          Prefix = None
          TagFilters = None
          Transitions = [] }

    member _.Yield(_: unit) : LifecycleRuleConfig =
        { AbortIncompleteMultipartUploadAfter = None
          Enabled = None
          Expiration = None
          ExpirationDate = None
          ExpiredObjectDeleteMarker = None
          Id = None
          NoncurrentVersionExpiration = None
          NoncurrentVersionsToRetain = None
          NoncurrentVersionTransitions = []
          ObjectSizeGreaterThan = None
          ObjectSizeLessThan = None
          Prefix = None
          TagFilters = None
          Transitions = [] }

    member _.Zero() : LifecycleRuleConfig =
        { AbortIncompleteMultipartUploadAfter = None
          Enabled = None
          Expiration = None
          ExpirationDate = None
          ExpiredObjectDeleteMarker = None
          Id = None
          NoncurrentVersionExpiration = None
          NoncurrentVersionsToRetain = None
          NoncurrentVersionTransitions = []
          ObjectSizeGreaterThan = None
          ObjectSizeLessThan = None
          Prefix = None
          TagFilters = None
          Transitions = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LifecycleRuleConfig) : LifecycleRuleConfig = f ()

    member _.Combine(state1: LifecycleRuleConfig, state2: LifecycleRuleConfig) : LifecycleRuleConfig =
        { AbortIncompleteMultipartUploadAfter =
            state2.AbortIncompleteMultipartUploadAfter
            |> Option.orElse state1.AbortIncompleteMultipartUploadAfter
          Enabled = state2.Enabled |> Option.orElse state1.Enabled
          Expiration = state2.Expiration |> Option.orElse state1.Expiration
          ExpirationDate = state2.ExpirationDate |> Option.orElse state1.ExpirationDate
          ExpiredObjectDeleteMarker =
            state2.ExpiredObjectDeleteMarker
            |> Option.orElse state1.ExpiredObjectDeleteMarker
          Id = state2.Id |> Option.orElse state1.Id
          NoncurrentVersionExpiration =
            state2.NoncurrentVersionExpiration
            |> Option.orElse state1.NoncurrentVersionExpiration
          NoncurrentVersionsToRetain =
            state2.NoncurrentVersionsToRetain
            |> Option.orElse state1.NoncurrentVersionsToRetain
          NoncurrentVersionTransitions = state1.NoncurrentVersionTransitions @ state2.NoncurrentVersionTransitions
          ObjectSizeGreaterThan = state2.ObjectSizeGreaterThan |> Option.orElse state1.ObjectSizeGreaterThan
          ObjectSizeLessThan = state2.ObjectSizeLessThan |> Option.orElse state1.ObjectSizeLessThan
          Prefix = state2.Prefix |> Option.orElse state1.Prefix
          TagFilters = state2.TagFilters |> Option.orElse state1.TagFilters
          Transitions = state1.Transitions @ state2.Transitions }

    member inline x.For
        (
            config: LifecycleRuleConfig,
            [<InlineIfLambda>] f: unit -> LifecycleRuleConfig
        ) : LifecycleRuleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: LifecycleRuleConfig) : ILifecycleRule =
        let rule = LifecycleRule()

        config.AbortIncompleteMultipartUploadAfter
        |> Option.iter (fun d -> rule.AbortIncompleteMultipartUploadAfter <- d)

        config.Enabled |> Option.iter (fun e -> rule.Enabled <- e)
        config.Expiration |> Option.iter (fun e -> rule.Expiration <- e)
        config.ExpirationDate |> Option.iter (fun d -> rule.ExpirationDate <- d)

        config.ExpiredObjectDeleteMarker
        |> Option.iter (fun e -> rule.ExpiredObjectDeleteMarker <- e)

        config.Id |> Option.iter (fun i -> rule.Id <- i)

        config.NoncurrentVersionExpiration
        |> Option.iter (fun d -> rule.NoncurrentVersionExpiration <- d)

        config.NoncurrentVersionsToRetain
        |> Option.iter (fun n -> rule.NoncurrentVersionsToRetain <- n)

        if not (List.isEmpty config.NoncurrentVersionTransitions) then
            rule.NoncurrentVersionTransitions <- config.NoncurrentVersionTransitions |> List.toArray

        config.ObjectSizeGreaterThan
        |> Option.iter (fun s -> rule.ObjectSizeGreaterThan <- s)

        config.ObjectSizeLessThan |> Option.iter (fun s -> rule.ObjectSizeLessThan <- s)

        config.Prefix |> Option.iter (fun p -> rule.Prefix <- p)
        config.TagFilters |> Option.iter (fun t -> rule.TagFilters <- t)

        if not (List.isEmpty config.Transitions) then
            rule.Transitions <- config.Transitions |> List.toArray

        rule :> ILifecycleRule

    [<CustomOperation("abortIncompleteMultipartUploadAfter")>]
    member _.AbortIncompleteMultipartUploadAfter(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            AbortIncompleteMultipartUploadAfter = Some duration }

    [<CustomOperation("enabled")>]
    member _.Enabled(config: LifecycleRuleConfig, value: bool) = { config with Enabled = Some value }

    [<CustomOperation("expiration")>]
    member _.Expiration(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            Expiration = Some duration }

    [<CustomOperation("expirationDate")>]
    member _.ExpirationDate(config: LifecycleRuleConfig, date: System.DateTime) =
        { config with
            ExpirationDate = Some date }

    [<CustomOperation("expiredObjectDeleteMarker")>]
    member _.ExpiredObjectDeleteMarker(config: LifecycleRuleConfig, value: bool) =
        { config with
            ExpiredObjectDeleteMarker = Some value }

    [<CustomOperation("id")>]
    member _.Id(config: LifecycleRuleConfig, id: string) = { config with Id = Some id }

    [<CustomOperation("noncurrentVersionExpiration")>]
    member _.NoncurrentVersionExpiration(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            NoncurrentVersionExpiration = Some duration }

    [<CustomOperation("noncurrentVersionsToRetain")>]
    member _.NoncurrentVersionsToRetain(config: LifecycleRuleConfig, count: float) =
        { config with
            NoncurrentVersionsToRetain = Some count }

    [<CustomOperation("noncurrentVersionTransitions")>]
    member _.NoncurrentVersionTransitions(config: LifecycleRuleConfig, transitions: INoncurrentVersionTransition list) =
        { config with
            NoncurrentVersionTransitions = config.NoncurrentVersionTransitions @ transitions }

    [<CustomOperation("objectSizeGreaterThan")>]
    member _.ObjectSizeGreaterThan(config: LifecycleRuleConfig, size: float) =
        { config with
            ObjectSizeGreaterThan = Some size }

    [<CustomOperation("objectSizeLessThan")>]
    member _.ObjectSizeLessThan(config: LifecycleRuleConfig, size: float) =
        { config with
            ObjectSizeLessThan = Some size }

    [<CustomOperation("prefix")>]
    member _.Prefix(config: LifecycleRuleConfig, prefix: string) = { config with Prefix = Some prefix }

    [<CustomOperation("tagFilters")>]
    member _.TagFilters(config: LifecycleRuleConfig, filters: System.Collections.Generic.IDictionary<string, obj>) =
        { config with
            TagFilters = Some filters }

    [<CustomOperation("transitions")>]
    member _.Transitions(config: LifecycleRuleConfig, transitions: ITransition list) =
        { config with
            Transitions = config.Transitions @ transitions }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module S3LifecycleRuleBuilders =
    let lifecycleRule = LifecycleRuleBuilder()
