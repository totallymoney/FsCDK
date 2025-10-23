namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ElasticLoadBalancingV2

// ============================================================================
// Network Load Balancer Configuration DSL
// ============================================================================

/// <summary>
/// High-level Network Load Balancer builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Internet-facing = false (internal by default for security)
/// - Cross-zone load balancing = enabled (for high availability)
/// - Deletion protection = false (opt-in for production)
/// - IP address type = IPv4
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Internal by default prevents accidental public exposure
/// - Cross-zone balancing improves availability and fault tolerance
/// - IPv4 provides broadest compatibility
///
/// **Escape Hatch:**
/// Access the underlying CDK NetworkLoadBalancer via the `LoadBalancer` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type NetworkLoadBalancerConfig =
    { LoadBalancerName: string
      ConstructId: string option
      Vpc: VpcRef option
      InternetFacing: bool option
      VpcSubnets: Amazon.CDK.AWS.EC2.SubnetSelection option
      CrossZoneEnabled: bool option
      DeletionProtection: bool option
      IpAddressType: IpAddressType option
      LoadBalancerName_: string option }

type NetworkLoadBalancerSpec =
    { LoadBalancerName: string
      ConstructId: string
      Props: NetworkLoadBalancerProps
      mutable LoadBalancer: INetworkLoadBalancer option }

    /// Gets the underlying INetworkLoadBalancer resource. Must be called after the stack is built.
    member this.Resource =
        match this.LoadBalancer with
        | Some nlb -> nlb
        | None ->
            failwith
                $"NetworkLoadBalancer '{this.LoadBalancerName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type NetworkLoadBalancerRef =
    | NetworkLoadBalancerInterface of INetworkLoadBalancer
    | NetworkLoadBalancerSpecRef of NetworkLoadBalancerSpec

module NetworkLoadBalancerHelpers =
    let resolveNetworkLoadBalancerRef (ref: NetworkLoadBalancerRef) =
        match ref with
        | NetworkLoadBalancerInterface nlb -> nlb
        | NetworkLoadBalancerSpecRef spec ->
            match spec.LoadBalancer with
            | Some nlb -> nlb
            | None ->
                failwith
                    $"NetworkLoadBalancer '{spec.LoadBalancerName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type NetworkLoadBalancerBuilder(name: string) =
    member _.Yield _ : NetworkLoadBalancerConfig =
        { LoadBalancerName = name
          ConstructId = None
          Vpc = None
          InternetFacing = Some false
          VpcSubnets = None
          CrossZoneEnabled = Some true
          DeletionProtection = Some false
          IpAddressType = Some IpAddressType.IPV4
          LoadBalancerName_ = None }

    member _.Zero() : NetworkLoadBalancerConfig =
        { LoadBalancerName = name
          ConstructId = None
          Vpc = None
          InternetFacing = Some false
          VpcSubnets = None
          CrossZoneEnabled = Some true
          DeletionProtection = Some false
          IpAddressType = Some IpAddressType.IPV4
          LoadBalancerName_ = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> NetworkLoadBalancerConfig) : NetworkLoadBalancerConfig = f ()

    member inline x.For
        (
            config: NetworkLoadBalancerConfig,
            [<InlineIfLambda>] f: unit -> NetworkLoadBalancerConfig
        ) : NetworkLoadBalancerConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: NetworkLoadBalancerConfig, b: NetworkLoadBalancerConfig) : NetworkLoadBalancerConfig =
        { LoadBalancerName = a.LoadBalancerName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          InternetFacing =
            match a.InternetFacing with
            | Some _ -> a.InternetFacing
            | None -> b.InternetFacing
          VpcSubnets =
            match a.VpcSubnets with
            | Some _ -> a.VpcSubnets
            | None -> b.VpcSubnets
          CrossZoneEnabled =
            match a.CrossZoneEnabled with
            | Some _ -> a.CrossZoneEnabled
            | None -> b.CrossZoneEnabled
          DeletionProtection =
            match a.DeletionProtection with
            | Some _ -> a.DeletionProtection
            | None -> b.DeletionProtection
          IpAddressType =
            match a.IpAddressType with
            | Some _ -> a.IpAddressType
            | None -> b.IpAddressType
          LoadBalancerName_ =
            match a.LoadBalancerName_ with
            | Some _ -> a.LoadBalancerName_
            | None -> b.LoadBalancerName_ }

    member _.Run(config: NetworkLoadBalancerConfig) : NetworkLoadBalancerSpec =
        let props = NetworkLoadBalancerProps()
        let constructId = config.ConstructId |> Option.defaultValue config.LoadBalancerName

        // VPC is required
        match config.Vpc with
        | Some vpcRef -> props.Vpc <- VpcHelpers.resolveVpcRef vpcRef
        | None -> printfn "Warning: VPC is required for Network Load Balancer"

        // AWS Best Practice: Internal by default for security
        props.InternetFacing <- config.InternetFacing |> Option.defaultValue false

        // AWS Best Practice: Enable cross-zone load balancing for HA
        props.CrossZoneEnabled <- config.CrossZoneEnabled |> Option.defaultValue true

        // AWS Best Practice: Deletion protection off by default (enable in production)
        props.DeletionProtection <- config.DeletionProtection |> Option.defaultValue false

        props.IpAddressType <-
            match config.IpAddressType with
            | Some ipat -> System.Nullable ipat
            | None -> System.Nullable()

        props.IpAddressType <- config.IpAddressType |> Option.defaultValue IpAddressType.IPV4

        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)
        config.LoadBalancerName_ |> Option.iter (fun n -> props.LoadBalancerName <- n)

        { LoadBalancerName = config.LoadBalancerName
          ConstructId = constructId
          Props = props
          LoadBalancer = None }

    /// <summary>Sets the construct ID for the Network Load Balancer.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: NetworkLoadBalancerConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Network Load Balancer.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: NetworkLoadBalancerConfig, vpc: Amazon.CDK.AWS.EC2.IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    /// <summary>Sets the VPC for the Network Load Balancer from a VpcSpec.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: NetworkLoadBalancerConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

    /// <summary>Sets whether the load balancer is internet-facing.</summary>
    /// <param name="internetFacing">True for internet-facing, false for internal (default: false).</param>
    [<CustomOperation("internetFacing")>]
    member _.InternetFacing(config: NetworkLoadBalancerConfig, internetFacing: bool) =
        { config with
            InternetFacing = Some internetFacing }

    /// <summary>Sets the VPC subnets for the load balancer.</summary>
    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: NetworkLoadBalancerConfig, subnets: Amazon.CDK.AWS.EC2.SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    /// <summary>Enables or disables cross-zone load balancing.</summary>
    /// <param name="enabled">Whether to enable cross-zone load balancing (default: true).</param>
    [<CustomOperation("crossZoneEnabled")>]
    member _.CrossZoneEnabled(config: NetworkLoadBalancerConfig, enabled: bool) =
        { config with
            CrossZoneEnabled = Some enabled }

    /// <summary>Enables or disables deletion protection.</summary>
    /// <param name="enabled">Whether to enable deletion protection (default: false).</param>
    [<CustomOperation("deletionProtection")>]
    member _.DeletionProtection(config: NetworkLoadBalancerConfig, enabled: bool) =
        { config with
            DeletionProtection = Some enabled }

    /// <summary>Sets the IP address type.</summary>
    [<CustomOperation("ipAddressType")>]
    member _.IpAddressType(config: NetworkLoadBalancerConfig, addressType: IpAddressType) =
        { config with
            IpAddressType = Some addressType }

    /// <summary>Sets the load balancer name.</summary>
    [<CustomOperation("loadBalancerName")>]
    member _.LoadBalancerName(config: NetworkLoadBalancerConfig, name: string) =
        { config with
            LoadBalancerName_ = Some name }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module NetworkLoadBalancerBuilders =
    /// <summary>Creates a Network Load Balancer with AWS best practices.</summary>
    /// <param name="name">The load balancer name.</param>
    /// <code lang="fsharp">
    /// networkLoadBalancer "MyNLB" {
    ///     vpc myVpc
    ///     internetFacing false
    ///     crossZoneEnabled true
    /// }
    /// </code>
    let networkLoadBalancer (name: string) = NetworkLoadBalancerBuilder name
