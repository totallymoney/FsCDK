namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EC2

// ============================================================================
// VPC Configuration DSL
// ============================================================================

type VpcConfig =
    { VpcName: string
      ConstructId: string option
      MaxAzs: int option
      NatGateways: int option
      SubnetConfiguration: ISubnetConfiguration list
      EnableDnsHostnames: bool option
      EnableDnsSupport: bool option
      DefaultInstanceTenancy: DefaultInstanceTenancy option
      IpAddresses: IIpAddresses option
      RemovalPolicy: RemovalPolicy option }

type VpcSpec =
    { VpcName: string
      ConstructId: string
      Props: VpcProps }

type VpcBuilder(name: string) =
    member _.Yield _ : VpcConfig =
        { VpcName = name
          ConstructId = None
          MaxAzs = None
          NatGateways = None
          SubnetConfiguration = []
          EnableDnsHostnames = None
          EnableDnsSupport = None
          DefaultInstanceTenancy = None
          IpAddresses = None
          RemovalPolicy = None }

    member _.Zero() : VpcConfig =
        { VpcName = name
          ConstructId = None
          MaxAzs = None
          NatGateways = None
          SubnetConfiguration = []
          EnableDnsHostnames = None
          EnableDnsSupport = None
          DefaultInstanceTenancy = None
          IpAddresses = None
          RemovalPolicy = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> VpcConfig) : VpcConfig = f ()

    member inline x.For(config: VpcConfig, [<InlineIfLambda>] f: unit -> VpcConfig) : VpcConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: VpcConfig, state2: VpcConfig) : VpcConfig =
        { VpcName = state1.VpcName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          MaxAzs =
            if state1.MaxAzs.IsSome then
                state1.MaxAzs
            else
                state2.MaxAzs
          NatGateways =
            if state1.NatGateways.IsSome then
                state1.NatGateways
            else
                state2.NatGateways
          SubnetConfiguration = state1.SubnetConfiguration @ state2.SubnetConfiguration
          EnableDnsHostnames =
            if state1.EnableDnsHostnames.IsSome then
                state1.EnableDnsHostnames
            else
                state2.EnableDnsHostnames
          EnableDnsSupport =
            if state1.EnableDnsSupport.IsSome then
                state1.EnableDnsSupport
            else
                state2.EnableDnsSupport
          DefaultInstanceTenancy =
            if state1.DefaultInstanceTenancy.IsSome then
                state1.DefaultInstanceTenancy
            else
                state2.DefaultInstanceTenancy
          IpAddresses =
            if state1.IpAddresses.IsSome then
                state1.IpAddresses
            else
                state2.IpAddresses
          RemovalPolicy =
            if state1.RemovalPolicy.IsSome then
                state1.RemovalPolicy
            else
                state2.RemovalPolicy }

    member _.Run(config: VpcConfig) : VpcSpec =
        let props = VpcProps()
        let constructId = config.ConstructId |> Option.defaultValue config.VpcName

        // AWS Best Practice: Default to 2 AZs for high availability
        props.MaxAzs <- config.MaxAzs |> Option.defaultValue 2

        // AWS Best Practice: Default to 1 NAT gateway per AZ for cost optimization
        // Users can increase for higher availability
        props.NatGateways <- config.NatGateways |> Option.defaultValue 1

        // AWS Best Practice: Enable DNS support by default
        props.EnableDnsHostnames <- config.EnableDnsHostnames |> Option.defaultValue true
        props.EnableDnsSupport <- config.EnableDnsSupport |> Option.defaultValue true

        // AWS Best Practice: Default subnet configuration with public and private subnets
        if config.SubnetConfiguration.IsEmpty then
            props.SubnetConfiguration <-
                [| SubnetConfiguration(
                       Name = "Public",
                       SubnetType = SubnetType.PUBLIC,
                       CidrMask = 24
                   )
                   SubnetConfiguration(
                       Name = "Private",
                       SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                       CidrMask = 24
                   ) |]
        else
            props.SubnetConfiguration <- config.SubnetConfiguration |> List.toArray

        config.DefaultInstanceTenancy
        |> Option.iter (fun t -> props.DefaultInstanceTenancy <- t)

        config.IpAddresses |> Option.iter (fun ip -> props.IpAddresses <- ip)

        { VpcName = config.VpcName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the VPC.</summary>
    /// <param name="id">The construct ID.</param>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: VpcConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the maximum number of Availability Zones to use.</summary>
    /// <param name="maxAzs">The maximum number of AZs (default: 2 for HA).</param>
    [<CustomOperation("maxAzs")>]
    member _.MaxAzs(config: VpcConfig, maxAzs: int) = { config with MaxAzs = Some maxAzs }

    /// <summary>Sets the number of NAT Gateways.</summary>
    /// <param name="natGateways">The number of NAT gateways (default: 1 for cost optimization).</param>
    [<CustomOperation("natGateways")>]
    member _.NatGateways(config: VpcConfig, natGateways: int) =
        { config with
            NatGateways = Some natGateways }

    /// <summary>Adds a subnet configuration.</summary>
    /// <param name="subnetConfig">The subnet configuration.</param>
    [<CustomOperation("subnet")>]
    member _.Subnet(config: VpcConfig, subnetConfig: SubnetConfiguration) =
        { config with
            SubnetConfiguration = subnetConfig :: config.SubnetConfiguration }

    /// <summary>Sets whether to enable DNS hostnames.</summary>
    /// <param name="enabled">Whether DNS hostnames are enabled (default: true).</param>
    [<CustomOperation("enableDnsHostnames")>]
    member _.EnableDnsHostnames(config: VpcConfig, enabled: bool) =
        { config with
            EnableDnsHostnames = Some enabled }

    /// <summary>Sets whether to enable DNS support.</summary>
    /// <param name="enabled">Whether DNS support is enabled (default: true).</param>
    [<CustomOperation("enableDnsSupport")>]
    member _.EnableDnsSupport(config: VpcConfig, enabled: bool) =
        { config with
            EnableDnsSupport = Some enabled }

    /// <summary>Sets the default instance tenancy.</summary>
    /// <param name="tenancy">The instance tenancy.</param>
    [<CustomOperation("defaultInstanceTenancy")>]
    member _.DefaultInstanceTenancy(config: VpcConfig, tenancy: DefaultInstanceTenancy) =
        { config with
            DefaultInstanceTenancy = Some tenancy }

    /// <summary>Sets the IP address configuration.</summary>
    /// <param name="ipAddresses">The IP addresses configuration.</param>
    [<CustomOperation("ipAddresses")>]
    member _.IpAddresses(config: VpcConfig, ipAddresses: IIpAddresses) =
        { config with
            IpAddresses = Some ipAddresses }

    /// <summary>Sets the CIDR block for the VPC.</summary>
    /// <param name="cidr">The CIDR block (e.g., "10.0.0.0/16").</param>
    [<CustomOperation("cidr")>]
    member _.Cidr(config: VpcConfig, cidr: string) =
        { config with
            IpAddresses = Some(IpAddresses.Cidr(cidr)) }

// ============================================================================
// Security Group Configuration DSL
// ============================================================================

type SecurityGroupConfig =
    { SecurityGroupName: string
      ConstructId: string option
      Vpc: IVpc option
      Description: string option
      AllowAllOutbound: bool option
      DisableInlineRules: bool option }

type SecurityGroupSpec =
    { SecurityGroupName: string
      ConstructId: string
      Props: SecurityGroupProps }

type SecurityGroupBuilder(name: string) =
    member _.Yield _ : SecurityGroupConfig =
        { SecurityGroupName = name
          ConstructId = None
          Vpc = None
          Description = None
          AllowAllOutbound = None
          DisableInlineRules = None }

    member _.Zero() : SecurityGroupConfig =
        { SecurityGroupName = name
          ConstructId = None
          Vpc = None
          Description = None
          AllowAllOutbound = None
          DisableInlineRules = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SecurityGroupConfig) : SecurityGroupConfig = f ()

    member inline x.For(config: SecurityGroupConfig, [<InlineIfLambda>] f: unit -> SecurityGroupConfig) : SecurityGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: SecurityGroupConfig, state2: SecurityGroupConfig) : SecurityGroupConfig =
        { SecurityGroupName = state1.SecurityGroupName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          Vpc = if state1.Vpc.IsSome then state1.Vpc else state2.Vpc
          Description =
            if state1.Description.IsSome then
                state1.Description
            else
                state2.Description
          AllowAllOutbound =
            if state1.AllowAllOutbound.IsSome then
                state1.AllowAllOutbound
            else
                state2.AllowAllOutbound
          DisableInlineRules =
            if state1.DisableInlineRules.IsSome then
                state1.DisableInlineRules
            else
                state2.DisableInlineRules }

    member _.Run(config: SecurityGroupConfig) : SecurityGroupSpec =
        let props = SecurityGroupProps()
        let constructId = config.ConstructId |> Option.defaultValue config.SecurityGroupName

        // VPC is required
        props.Vpc <-
            match config.Vpc with
            | Some vpc -> vpc
            | None -> failwith "VPC is required for Security Group"

        // AWS Best Practice: Least privilege - don't allow all outbound by default
        // Users should explicitly allow what they need
        props.AllowAllOutbound <- config.AllowAllOutbound |> Option.defaultValue false

        config.Description
        |> Option.iter (fun desc -> props.Description <- desc)

        config.DisableInlineRules
        |> Option.iter (fun d -> props.DisableInlineRules <- d)

        { SecurityGroupName = config.SecurityGroupName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the Security Group.</summary>
    /// <param name="id">The construct ID.</param>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SecurityGroupConfig, id: string) =
        { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Security Group.</summary>
    /// <param name="vpc">The VPC.</param>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: SecurityGroupConfig, vpc: IVpc) = { config with Vpc = Some vpc }

    /// <summary>Sets the description for the Security Group.</summary>
    /// <param name="description">The description.</param>
    [<CustomOperation("description")>]
    member _.Description(config: SecurityGroupConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets whether to allow all outbound traffic.</summary>
    /// <param name="allow">Whether to allow all outbound (default: false for least privilege).</param>
    [<CustomOperation("allowAllOutbound")>]
    member _.AllowAllOutbound(config: SecurityGroupConfig, allow: bool) =
        { config with
            AllowAllOutbound = Some allow }

    /// <summary>Sets whether to disable inline rules.</summary>
    /// <param name="disable">Whether to disable inline rules.</param>
    [<CustomOperation("disableInlineRules")>]
    member _.DisableInlineRules(config: SecurityGroupConfig, disable: bool) =
        { config with
            DisableInlineRules = Some disable }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module VpcBuilders =
    /// <summary>Creates a VPC configuration with AWS best practices.</summary>
    /// <param name="name">The VPC name.</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     maxAzs 2
    ///     natGateways 1
    ///     cidr "10.0.0.0/16"
    /// }
    /// </code>
    let vpc (name: string) = VpcBuilder(name)

    /// <summary>Creates a Security Group configuration.</summary>
    /// <param name="name">The Security Group name.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     vpc myVpc
    ///     description "Security group for my application"
    ///     allowAllOutbound false
    /// }
    /// </code>
    let securityGroup (name: string) = SecurityGroupBuilder(name)
