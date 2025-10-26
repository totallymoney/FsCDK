namespace FsCDK

open Amazon.CDK.AWS.EC2

// ============================================================================
// EC2 SubnetSelection Builder DSL
// ============================================================================

type SubnetSelectionConfig =
    { SubnetType: SubnetType option
      AvailabilityZones: string seq option }

type SubnetSelectionBuilder() =
    member _.Yield _ : SubnetSelectionConfig =
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
        let subnetSelection = SubnetSelection()
        cfg.SubnetType |> Option.iter (fun t -> subnetSelection.SubnetType <- t)

        cfg.AvailabilityZones
        |> Option.iter (fun az -> subnetSelection.AvailabilityZones <- (az |> Seq.toArray))

        subnetSelection

    /// <summary>Sets the subnet type for the subnet selection.</summary>
    /// <param name="cfg">The current subnet selection configuration.</param>
    /// <param name="value">The subnet type (e.g., PUBLIC, PRIVATE_WITH_NAT, PRIVATE_ISOLATED).</param>
    /// <code lang="fsharp">
    /// subnetSelection {
    ///     subnetType SubnetType.PUBLIC
    /// }
    /// </code>
    [<CustomOperation("subnetType")>]
    member _.SubnetType(cfg: SubnetSelectionConfig, value: SubnetType) = { cfg with SubnetType = Some value }

    /// <summary>Sets the availability zones for the subnet selection.</summary>
    /// <param name="cfg">The current subnet selection configuration.</param>
    /// <param name="azs">The list of availability zone names (e.g., ["us-east-1a"; "us-east-1b"]).</param>
    /// <code lang="fsharp">
    /// subnetSelection {
    ///     availabilityZones [ "us-east-1a"; "us-east-1b" ]
    /// }
    /// </code>
    [<CustomOperation("availabilityZones")>]
    member _.AvailabilityZones(cfg: SubnetSelectionConfig, azs: string list) =
        { cfg with
            AvailabilityZones = Some azs }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module SubnetSelectionBuilders =

    /// <summary>Creates an AWS CDK SubnetSelection configuration.</summary>
    /// <code lang="fsharp">
    /// subnetSelection {
    ///     subnetType SubnetType.PUBLIC
    ///     availabilityZones [ "us-east-1a"; "us-east-1b" ]
    /// }
    /// </code>
    let subnetSelection = SubnetSelectionBuilder()
