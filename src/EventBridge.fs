namespace FsCDK

open Amazon.CDK.AWS.Events

// ============================================================================
// EventBridge Rule Configuration DSL
// ============================================================================

/// <summary>
/// High-level EventBridge Rule builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Enabled = true (rules are active by default)
/// - Event bus = default event bus
/// - Dead letter queue = not configured (opt-in for production)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Active rules by default to prevent misconfiguration
/// - Default event bus for simplicity
/// - DLQ opt-in allows users to handle failures explicitly
///
/// **Escape Hatch:**
/// Access the underlying CDK Rule via the `Rule` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type EventBridgeRuleConfig =
    { RuleName: string
      ConstructId: string option
      RuleName_: string option
      Description: string option
      Enabled: bool option
      EventPattern: IEventPattern option
      Schedule: Schedule option
      Targets: IRuleTarget seq
      EventBus: IEventBus option }

type EventBridgeRuleSpec =
    { RuleName: string
      ConstructId: string
      Props: RuleProps
      mutable Rule: IRule option }

type EventBridgeRuleBuilder(name: string) =
    member _.Yield _ : EventBridgeRuleConfig =
        { RuleName = name
          ConstructId = None
          RuleName_ = None
          Description = None
          Enabled = Some true
          EventPattern = None
          Schedule = None
          Targets = []
          EventBus = None }

    member _.Zero() : EventBridgeRuleConfig =
        { RuleName = name
          ConstructId = None
          RuleName_ = None
          Description = None
          Enabled = Some true
          EventPattern = None
          Schedule = None
          Targets = []
          EventBus = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> EventBridgeRuleConfig) : EventBridgeRuleConfig = f ()

    member inline x.For
        (
            config: EventBridgeRuleConfig,
            [<InlineIfLambda>] f: unit -> EventBridgeRuleConfig
        ) : EventBridgeRuleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: EventBridgeRuleConfig, b: EventBridgeRuleConfig) : EventBridgeRuleConfig =
        { RuleName = a.RuleName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          RuleName_ =
            match a.RuleName_ with
            | Some _ -> a.RuleName_
            | None -> b.RuleName_
          Description =
            match a.Description with
            | Some _ -> a.Description
            | None -> b.Description
          Enabled =
            match a.Enabled with
            | Some _ -> a.Enabled
            | None -> b.Enabled
          EventPattern =
            match a.EventPattern with
            | Some _ -> a.EventPattern
            | None -> b.EventPattern
          Schedule =
            match a.Schedule with
            | Some _ -> a.Schedule
            | None -> b.Schedule
          Targets = Seq.toList a.Targets @ Seq.toList b.Targets
          EventBus =
            match a.EventBus with
            | Some _ -> a.EventBus
            | None -> b.EventBus }

    member _.Run(config: EventBridgeRuleConfig) : EventBridgeRuleSpec =
        let props = RuleProps()
        let constructId = config.ConstructId |> Option.defaultValue config.RuleName

        // AWS Best Practice: Enable rules by default
        props.Enabled <- config.Enabled |> Option.defaultValue true

        // Either eventPattern or schedule must be set
        config.EventPattern |> Option.iter (fun p -> props.EventPattern <- p)
        config.Schedule |> Option.iter (fun s -> props.Schedule <- s)

        config.RuleName_ |> Option.iter (fun n -> props.RuleName <- n)
        config.Description |> Option.iter (fun d -> props.Description <- d)
        config.EventBus |> Option.iter (fun bus -> props.EventBus <- bus)

        if not (Seq.isEmpty config.Targets) then
            props.Targets <- config.Targets |> Seq.toArray

        { RuleName = config.RuleName
          ConstructId = constructId
          Props = props
          Rule = None }

    /// <summary>Sets the construct ID for the EventBridge rule.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: EventBridgeRuleConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the rule name.</summary>
    [<CustomOperation("ruleName")>]
    member _.RuleName(config: EventBridgeRuleConfig, name: string) = { config with RuleName_ = Some name }

    /// <summary>Sets the rule description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: EventBridgeRuleConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Enables or disables the rule.</summary>
    [<CustomOperation("enabled")>]
    member _.Enabled(config: EventBridgeRuleConfig, enabled: bool) = { config with Enabled = Some enabled }

    /// <summary>Sets the event pattern for the rule.</summary>
    [<CustomOperation("eventPattern")>]
    member _.EventPattern(config: EventBridgeRuleConfig, pattern: IEventPattern) =
        { config with
            EventPattern = Some pattern }

    /// <summary>Sets the schedule for the rule.</summary>
    [<CustomOperation("schedule")>]
    member _.Schedule(config: EventBridgeRuleConfig, schedule: Schedule) =
        { config with Schedule = Some schedule }

    /// <summary>Adds a target to the rule.</summary>
    [<CustomOperation("targets")>]
    member _.Targets(config: EventBridgeRuleConfig, targets: IRuleTarget seq) = { config with Targets = targets }

    /// <summary>Adds a single target to the rule.</summary>
    [<CustomOperation("target")>]
    member _.Target(config: EventBridgeRuleConfig, target: IRuleTarget) =
        { config with
            Targets = Seq.append config.Targets [ target ] }

    /// <summary>Sets the event bus for the rule.</summary>
    [<CustomOperation("eventBus")>]
    member _.EventBus(config: EventBridgeRuleConfig, eventBus: IEventBus) =
        { config with EventBus = Some eventBus }

// ============================================================================
// EventBridge Event Bus Configuration DSL
// ============================================================================

type EventBusConfig =
    { EventBusName: string
      ConstructId: string option
      EventSourceName: string option }

type EventBusSpec =
    { EventBusName: string
      ConstructId: string
      mutable EventBus: IEventBus }

type EventBusBuilder(name: string) =
    member _.Yield _ : EventBusConfig =
        { EventBusName = name
          ConstructId = None
          EventSourceName = None }

    member _.Zero() : EventBusConfig =
        { EventBusName = name
          ConstructId = None
          EventSourceName = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> EventBusConfig) : EventBusConfig = f ()

    member inline x.For(config: EventBusConfig, [<InlineIfLambda>] f: unit -> EventBusConfig) : EventBusConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: EventBusConfig, b: EventBusConfig) : EventBusConfig =
        { EventBusName = a.EventBusName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          EventSourceName =
            match a.EventSourceName with
            | Some _ -> a.EventSourceName
            | None -> b.EventSourceName }

    member _.Run(config: EventBusConfig) : EventBusSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.EventBusName

        // For custom event buses, we just use the name
        // EventSource is only for partner event buses
        { EventBusName = config.EventBusName
          ConstructId = constructId
          EventBus = null }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: EventBusConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the event source name for partner event bus.</summary>
    [<CustomOperation("eventSourceName")>]
    member _.EventSourceName(config: EventBusConfig, sourceName: string) =
        { config with
            EventSourceName = Some sourceName }

    /// <summary>Sets a custom event bus name (creates a custom event bus, not default).</summary>
    [<CustomOperation("customEventBusName")>]
    member _.CustomEventBusName(config: EventBusConfig, name: string) = { config with EventBusName = name }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module EventBridgeBuilders =
    /// <summary>Creates an EventBridge rule with AWS best practices.</summary>
    /// <param name="name">The rule name.</param>
    /// <code lang="fsharp">
    /// eventBridgeRule "MyRule" {
    ///     description "Process daily events"
    ///     schedule (Schedule.Rate(Duration.Hours(24.0)))
    ///     target (LambdaFunction(myFunction))
    /// }
    /// </code>
    let eventBridgeRule (name: string) = EventBridgeRuleBuilder(name)

    /// <summary>Creates an EventBridge event bus.</summary>
    /// <param name="name">The event bus name.</param>
    /// <code lang="fsharp">
    /// eventBus "MyEventBus" {
    ///     constructId "CustomEventBus"
    /// }
    /// </code>
    let eventBus (name: string) = EventBusBuilder(name)
