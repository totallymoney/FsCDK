namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Lambda Event Source Mapping Types
// ============================================================================

// ============================================================================
// Lambda Event Source Mapping Options Builder DSL
// ============================================================================

type EventSourceMappingOptionsConfig =
    { EventSourceArn: string option
      BatchSize: int option
      StartingPosition: StartingPosition option
      Enabled: bool option
      MaxBatchingWindow: Duration option
      ParallelizationFactor: int option }

type EventSourceMappingOptionsBuilder() =
    member _.Yield(_: unit) : EventSourceMappingOptionsConfig =
        { EventSourceArn = None
          BatchSize = None
          StartingPosition = None
          Enabled = None
          MaxBatchingWindow = None
          ParallelizationFactor = None }

    member _.Zero() : EventSourceMappingOptionsConfig =
        { EventSourceArn = None
          BatchSize = None
          StartingPosition = None
          Enabled = None
          MaxBatchingWindow = None
          ParallelizationFactor = None }

    member inline _.Delay
        ([<InlineIfLambda>] f: unit -> EventSourceMappingOptionsConfig)
        : EventSourceMappingOptionsConfig =
        f ()

    member inline x.For
        (
            config: EventSourceMappingOptionsConfig,
            [<InlineIfLambda>] f: unit -> EventSourceMappingOptionsConfig
        ) : EventSourceMappingOptionsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine
        (
            state1: EventSourceMappingOptionsConfig,
            state2: EventSourceMappingOptionsConfig
        ) : EventSourceMappingOptionsConfig =
        { EventSourceArn =
            if state1.EventSourceArn.IsSome then
                state1.EventSourceArn
            else
                state2.EventSourceArn
          BatchSize =
            if state1.BatchSize.IsSome then
                state1.BatchSize
            else
                state2.BatchSize
          StartingPosition =
            if state1.StartingPosition.IsSome then
                state1.StartingPosition
            else
                state2.StartingPosition
          Enabled =
            if state1.Enabled.IsSome then
                state1.Enabled
            else
                state2.Enabled
          MaxBatchingWindow =
            if state1.MaxBatchingWindow.IsSome then
                state1.MaxBatchingWindow
            else
                state2.MaxBatchingWindow
          ParallelizationFactor =
            if state1.ParallelizationFactor.IsSome then
                state1.ParallelizationFactor
            else
                state2.ParallelizationFactor }

    member _.Run(config: EventSourceMappingOptionsConfig) =
        let opts = EventSourceMappingOptions()

        let arn =
            match config.EventSourceArn with
            | Some a -> a
            | None -> failwith "eventSourceArn is required for EventSourceMappingOptions"

        opts.EventSourceArn <- arn
        config.BatchSize |> Option.iter (fun v -> opts.BatchSize <- v)
        config.StartingPosition |> Option.iter (fun v -> opts.StartingPosition <- v)
        config.Enabled |> Option.iter (fun v -> opts.Enabled <- v)
        config.MaxBatchingWindow |> Option.iter (fun d -> opts.MaxBatchingWindow <- d)

        config.ParallelizationFactor
        |> Option.iter (fun v -> opts.ParallelizationFactor <- v)

        opts

    [<CustomOperation("eventSourceArn")>]
    member _.EventSourceArn(config: EventSourceMappingOptionsConfig, arn: string) =
        { config with
            EventSourceArn = Some arn }

    [<CustomOperation("batchSize")>]
    member _.BatchSize(config: EventSourceMappingOptionsConfig, size: int) = { config with BatchSize = Some size }

    [<CustomOperation("startingPosition")>]
    member _.StartingPosition(config: EventSourceMappingOptionsConfig, pos: StartingPosition) =
        { config with
            StartingPosition = Some pos }

    [<CustomOperation("enabled")>]
    member _.Enabled(config: EventSourceMappingOptionsConfig, value: bool) = { config with Enabled = Some value }

    [<CustomOperation("maxBatchingWindow")>]
    member _.MaxBatchingWindow(config: EventSourceMappingOptionsConfig, window: Duration) =
        { config with
            MaxBatchingWindow = Some window }

    [<CustomOperation("parallelizationFactor")>]
    member _.ParallelizationFactor(config: EventSourceMappingOptionsConfig, value: int) =
        { config with
            ParallelizationFactor = Some value }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module EventSourceMappingBuilders =
    let eventSourceMapping = EventSourceMappingOptionsBuilder()
