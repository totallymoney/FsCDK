namespace FsCDK

open Amazon.CDK.AWS.EC2

// ============================================================================
// EC2 SubnetSelection Builder DSL
// ============================================================================

type SubnetSelectionConfig =
    { SubnetType: SubnetType option
      AvailabilityZones: string list option }

type SubnetSelectionBuilder() =
    member _.Yield(_: unit) : SubnetSelectionConfig =
        { SubnetType = None
          AvailabilityZones = None }

    member _.Zero() : SubnetSelectionConfig =
        { SubnetType = None
          AvailabilityZones = None }

    member _.Combine(a: SubnetSelectionConfig, b: SubnetSelectionConfig) : SubnetSelectionConfig =
        { SubnetType = (if a.SubnetType.IsSome then a.SubnetType else b.SubnetType)
          AvailabilityZones =
            (if a.AvailabilityZones.IsSome then
                 a.AvailabilityZones
             else
                 b.AvailabilityZones) }

    member inline _.Delay(f: unit -> SubnetSelectionConfig) = f ()
    member inline x.For(state: SubnetSelectionConfig, f: unit -> SubnetSelectionConfig) = x.Combine(state, f ())

    member _.Run(cfg: SubnetSelectionConfig) : SubnetSelection =
        let s = SubnetSelection()
        cfg.SubnetType |> Option.iter (fun t -> s.SubnetType <- t)

        cfg.AvailabilityZones
        |> Option.iter (fun az -> s.AvailabilityZones <- (az |> List.toArray))

        s

    [<CustomOperation("subnetType")>]
    member _.SubnetType(cfg: SubnetSelectionConfig, value: SubnetType) = { cfg with SubnetType = Some value }

    [<CustomOperation("availabilityZones")>]
    member _.AvailabilityZones(cfg: SubnetSelectionConfig, azs: string list) =
        { cfg with
            AvailabilityZones = Some azs }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module SubnetSelectionBuilders =
    let subnetSelection = SubnetSelectionBuilder()
