namespace FsCDK

open Amazon.CDK.AWS.S3

type BucketMetricsConfig =
    { Id: string option
      Prefix: string option
      TagFilters: Map<string, obj> option }

type BucketMetricsBuilder() =
    member _.Yield _ : BucketMetricsConfig =
        { Id = None
          Prefix = None
          TagFilters = None }

    member _.Zero() : BucketMetricsConfig =
        { Id = None
          Prefix = None
          TagFilters = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> BucketMetricsConfig) : BucketMetricsConfig = f ()

    member _.Combine(state1: BucketMetricsConfig, state2: BucketMetricsConfig) : BucketMetricsConfig =
        { Id = state2.Id |> Option.orElse state1.Id
          Prefix = state2.Prefix |> Option.orElse state1.Prefix
          TagFilters = state1.TagFilters |> Option.orElse state2.TagFilters }

    member inline x.For
        (
            config: BucketMetricsConfig,
            [<InlineIfLambda>] f: unit -> BucketMetricsConfig
        ) : BucketMetricsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: BucketMetricsConfig) : IBucketMetrics =
        let metrics = BucketMetrics()

        let id =
            match config.Id with
            | Some i -> i
            | None -> failwith "metrics.id is required"

        metrics.Id <- id

        config.Prefix |> Option.iter (fun p -> metrics.Prefix <- p)

        config.TagFilters |> Option.iter (fun tf -> metrics.TagFilters <- tf)

        metrics :> IBucketMetrics

    [<CustomOperation("id")>]
    member _.Id(config: BucketMetricsConfig, id: string) = { config with Id = Some id }

    [<CustomOperation("prefix")>]
    member _.Prefix(config: BucketMetricsConfig, prefix: string) = { config with Prefix = Some prefix }

    [<CustomOperation("tagFilters")>]
    member _.TagFilters(config: BucketMetricsConfig, filters: (string * obj) seq) =
        { config with
            TagFilters = Some(filters |> Map.ofSeq) }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module S3BucketMetricsBuilders =
    let metrics = BucketMetricsBuilder()
