namespace FsCDK

open System.Collections.Generic
open Amazon.CDK.AWS.S3

type BucketMetricsConfig =
    { Id: string option
      Prefix: string option
      TagFilters: (string * obj) list }

type BucketMetricsBuilder() =
    member _.Yield _ : BucketMetricsConfig =
        { Id = None
          Prefix = None
          TagFilters = [] }

    member _.Zero() : BucketMetricsConfig =
        { Id = None
          Prefix = None
          TagFilters = [] }

    member _.Delay(f: unit -> BucketMetricsConfig) : BucketMetricsConfig = f ()

    member _.Combine(state1: BucketMetricsConfig, state2: BucketMetricsConfig) : BucketMetricsConfig =
        { Id = state2.Id |> Option.orElse state1.Id
          Prefix = state2.Prefix |> Option.orElse state1.Prefix
          TagFilters = state2.TagFilters @ state1.TagFilters }

    member x.For(config: BucketMetricsConfig, f: unit -> BucketMetricsConfig) : BucketMetricsConfig =
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

        let tagFilters = config.TagFilters |> Map.ofList |> Dictionary

        metrics.TagFilters <- tagFilters

        metrics :> IBucketMetrics

    [<CustomOperation("id")>]
    member _.Id(config: BucketMetricsConfig, id: string) = { config with Id = Some id }

    [<CustomOperation("prefix")>]
    member _.Prefix(config: BucketMetricsConfig, prefix: string) = { config with Prefix = Some prefix }

    [<CustomOperation("tagFilters")>]
    member _.TagFilters(config: BucketMetricsConfig, filters: (string * obj) list) =
        { config with TagFilters = filters }
