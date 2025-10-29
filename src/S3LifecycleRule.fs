namespace FsCDK

open System
open Amazon.CDK
open Amazon.CDK.AWS.S3

type LifecycleRuleConfig =
    { Id: string option
      Enabled: bool option
      Prefix: string option
      Expiration: Duration option
      ExpirationDate: DateTime option
      Transitions: ITransition seq
      NoncurrentVersionExpiration: Duration option
      NoncurrentVersionTransitions: INoncurrentVersionTransition seq
      NoncurrentVersionsToRetain: float option
      AbortIncompleteMultipartUploadAfter: Duration option
      ExpiredObjectDeleteMarker: bool option
      ObjectSizeGreaterThan: float option
      ObjectSizeLessThan: float option
      TagFilters: Map<string, obj> option }

type LifecycleRuleBuilder() =
    member _.Yield(_: unit) : LifecycleRuleConfig =
        { Id = None
          Enabled = None
          Prefix = None
          Expiration = None
          ExpirationDate = None
          Transitions = []
          NoncurrentVersionExpiration = None
          NoncurrentVersionTransitions = []
          NoncurrentVersionsToRetain = None
          AbortIncompleteMultipartUploadAfter = None
          ExpiredObjectDeleteMarker = None
          ObjectSizeLessThan = None
          ObjectSizeGreaterThan = None
          TagFilters = None }

    member _.Yield(transaction: ITransition) : LifecycleRuleConfig =
        { Id = None
          Enabled = None
          Prefix = None
          Expiration = None
          ExpirationDate = None
          Transitions = [ transaction ]
          NoncurrentVersionExpiration = None
          NoncurrentVersionTransitions = []
          NoncurrentVersionsToRetain = None
          AbortIncompleteMultipartUploadAfter = None
          ExpiredObjectDeleteMarker = None
          ObjectSizeLessThan = None
          ObjectSizeGreaterThan = None
          TagFilters = None }

    member _.Yield(transaction: INoncurrentVersionTransition) : LifecycleRuleConfig =
        { Id = None
          Enabled = None
          Prefix = None
          Expiration = None
          ExpirationDate = None
          Transitions = []
          NoncurrentVersionExpiration = None
          NoncurrentVersionTransitions = [ transaction ]
          NoncurrentVersionsToRetain = None
          AbortIncompleteMultipartUploadAfter = None
          ExpiredObjectDeleteMarker = None
          ObjectSizeLessThan = None
          ObjectSizeGreaterThan = None
          TagFilters = None }

    member _.Zero() : LifecycleRuleConfig =
        { Id = None
          Enabled = None
          Prefix = None
          Expiration = None
          ExpirationDate = None
          Transitions = []
          NoncurrentVersionExpiration = None
          NoncurrentVersionTransitions = []
          NoncurrentVersionsToRetain = None
          AbortIncompleteMultipartUploadAfter = None
          ExpiredObjectDeleteMarker = None
          ObjectSizeLessThan = None
          ObjectSizeGreaterThan = None
          TagFilters = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LifecycleRuleConfig) : LifecycleRuleConfig = f ()

    member _.Combine(state1: LifecycleRuleConfig, state2: LifecycleRuleConfig) : LifecycleRuleConfig =
        { Id = state2.Id |> Option.orElse state1.Id
          Enabled = state2.Enabled |> Option.orElse state1.Enabled
          Prefix = state2.Prefix |> Option.orElse state1.Prefix
          Expiration = state2.Expiration |> Option.orElse state1.Expiration
          ExpirationDate = state2.ExpirationDate |> Option.orElse state1.ExpirationDate
          Transitions = Seq.toList state1.Transitions @ Seq.toList state2.Transitions
          NoncurrentVersionExpiration =
            state2.NoncurrentVersionExpiration
            |> Option.orElse state1.NoncurrentVersionExpiration
          NoncurrentVersionTransitions =
            Seq.toList state1.NoncurrentVersionTransitions
            @ Seq.toList state2.NoncurrentVersionTransitions
          NoncurrentVersionsToRetain =
            state2.NoncurrentVersionsToRetain
            |> Option.orElse state1.NoncurrentVersionsToRetain
          AbortIncompleteMultipartUploadAfter =
            state2.AbortIncompleteMultipartUploadAfter
            |> Option.orElse state1.AbortIncompleteMultipartUploadAfter
          ExpiredObjectDeleteMarker =
            state2.ExpiredObjectDeleteMarker
            |> Option.orElse state1.ExpiredObjectDeleteMarker
          ObjectSizeGreaterThan = state2.ObjectSizeGreaterThan |> Option.orElse state1.ObjectSizeGreaterThan
          ObjectSizeLessThan = state2.ObjectSizeLessThan |> Option.orElse state1.ObjectSizeLessThan
          TagFilters = state2.TagFilters |> Option.orElse state1.TagFilters }

    member inline x.For
        (
            config: LifecycleRuleConfig,
            [<InlineIfLambda>] f: unit -> LifecycleRuleConfig
        ) : LifecycleRuleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: LifecycleRuleConfig) : ILifecycleRule =
        let rule = LifecycleRule()

        config.Id |> Option.iter (fun i -> rule.Id <- i)
        config.Enabled |> Option.iter (fun e -> rule.Enabled <- e)
        config.Prefix |> Option.iter (fun p -> rule.Prefix <- p)
        config.Expiration |> Option.iter (fun e -> rule.Expiration <- e)
        config.ExpirationDate |> Option.iter (fun ed -> rule.ExpirationDate <- ed)

        if not (Seq.isEmpty config.Transitions) then
            rule.Transitions <- (config.Transitions |> Seq.toArray)

        config.NoncurrentVersionExpiration
        |> Option.iter (fun nve -> rule.NoncurrentVersionExpiration <- nve)

        if not (Seq.isEmpty config.NoncurrentVersionTransitions) then
            rule.NoncurrentVersionTransitions <- (config.NoncurrentVersionTransitions |> Seq.toArray)

        if not (Seq.isEmpty config.Transitions) then
            rule.Transitions <- (config.Transitions |> Seq.toArray)

        config.NoncurrentVersionsToRetain
        |> Option.iter (fun nvr -> rule.NoncurrentVersionsToRetain <- nvr)

        config.AbortIncompleteMultipartUploadAfter
        |> Option.iter (fun a -> rule.AbortIncompleteMultipartUploadAfter <- a)

        config.ExpiredObjectDeleteMarker
        |> Option.iter (fun eodm -> rule.ExpiredObjectDeleteMarker <- eodm)

        config.ObjectSizeGreaterThan
        |> Option.iter (fun osg -> rule.ObjectSizeGreaterThan <- osg)

        config.ObjectSizeLessThan
        |> Option.iter (fun osl -> rule.ObjectSizeLessThan <- osl)

        rule

    /// <summary> Sets the ID of the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="id">The ID of the lifecycle rule.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     id "TransitionToGlacier"
    /// }
    /// </code>
    [<CustomOperation("id")>]
    member _.Id(config: LifecycleRuleConfig, id: string) = { config with Id = Some id }

    /// <summary> Sets whether the lifecycle rule is enabled. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="enabled">True to enable the rule, false to disable it.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     enabled true
    /// }
    /// </code>
    [<CustomOperation("enabled")>]
    member _.Enabled(config: LifecycleRuleConfig, enabled: bool) = { config with Enabled = Some enabled }

    /// <summary> Sets the prefix for the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="prefix">The prefix for the lifecycle rule.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     prefix "logs/"
    /// }
    /// </code>
    [<CustomOperation("prefix")>]
    member _.Prefix(config: LifecycleRuleConfig, prefix: string) = { config with Prefix = Some prefix }

    /// <summary> Sets the expiration duration for the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="duration">The expiration duration.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     expiration (Duration.Days(365.0))
    /// }
    /// </code>
    [<CustomOperation("expiration")>]
    member _.Expiration(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            Expiration = Some duration }

    /// <summary> Sets the expiration date for the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="date">The expiration date.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     expirationDate (DateTime(2025, 12, 31))
    /// }
    /// </code>
    [<CustomOperation("expirationDate")>]
    member _.ExpirationDate(config: LifecycleRuleConfig, date: DateTime) =
        { config with
            ExpirationDate = Some date }

    /// <summary> Adds transitions to the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="transitions">The list of transitions.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     transitions [ Transition(StorageClass = StorageClass.GLACIER, TransitionAfter = Duration.Days(30.0)) ]
    /// }
    /// </code>
    [<CustomOperation("transitions")>]
    member _.Transitions(config: LifecycleRuleConfig, transitions: ITransition seq) =
        { config with
            Transitions = Seq.toList config.Transitions @ (Seq.toList transitions) }

    /// <summary> Sets the number of noncurrent versions to retain. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="count">The number of noncurrent versions to retain.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     noncurrentVersionsToRetain 5.0
    /// }
    /// </code>
    [<CustomOperation("noncurrentVersionsToRetain")>]
    member _.NoncurrentVersionsToRetain(config: LifecycleRuleConfig, count: float) =
        { config with
            NoncurrentVersionsToRetain = Some count }

    /// <summary> Sets the noncurrent version expiration duration for the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="duration">The noncurrent version expiration duration.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     noncurrentVersionExpiration (Duration.Days(90.0))
    /// }
    /// </code>
    [<CustomOperation("noncurrentVersionExpiration")>]
    member _.NoncurrentVersionExpiration(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            NoncurrentVersionExpiration = Some duration }


    /// <summary> Adds noncurrent version transitions to the lifecycle rule. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="transitions">The list of noncurrent version transitions.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     noncurrentVersionTransitions [ NoncurrentVersionTransition(StorageClass = StorageClass.GLACIER, TransitionAfter = Duration.Days(30.0)) ]
    /// }
    /// </code>
    [<CustomOperation("noncurrentVersionTransitions")>]
    member _.NoncurrentVersionTransitions(config: LifecycleRuleConfig, transitions: INoncurrentVersionTransition seq) =
        { config with
            NoncurrentVersionTransitions = Seq.toList config.NoncurrentVersionTransitions @ (Seq.toList transitions) }


    /// <summary> Sets the duration after which incomplete multipart uploads are aborted. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="duration">The duration after which incomplete multipart uploads are aborted.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     abortIncompleteMultipartUploadAfter (Duration.Days(7.0))
    /// }
    /// </code>
    [<CustomOperation("abortIncompleteMultipartUploadAfter")>]
    member _.AbortIncompleteMultipartUploadAfter(config: LifecycleRuleConfig, duration: Duration) =
        { config with
            AbortIncompleteMultipartUploadAfter = Some duration }

    /// <summary> Sets whether to delete expired object delete markers. </summary>
    /// <param name="config">The current lifecycle rule configuration.</param>
    /// <param name="expiredObjectDeleteMarker">True to delete expired object delete markers, false otherwise.</param>
    /// <code lang="fsharp">
    /// lifecycleRule {
    ///     expiredObjectDeleteMarker true
    /// }
    /// </code>
    [<CustomOperation("expiredObjectDeleteMarker")>]
    member _.ExpiredObjectDeleteMarker(config: LifecycleRuleConfig, expiredObjectDeleteMarker: bool) =
        { config with
            ExpiredObjectDeleteMarker = Some expiredObjectDeleteMarker }


// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module S3LifecycleRuleBuilders =
    let lifecycleRule = LifecycleRuleBuilder()
