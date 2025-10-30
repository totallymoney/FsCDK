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
      Props: VpcProps
      mutable Vpc: IVpc }

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

    member _.Combine(a: VpcConfig, b: VpcConfig) : VpcConfig =
        { VpcName = a.VpcName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          MaxAzs =
            match a.MaxAzs with
            | Some _ -> a.MaxAzs
            | None -> b.MaxAzs
          NatGateways =
            match a.NatGateways with
            | Some _ -> a.NatGateways
            | None -> b.NatGateways
          SubnetConfiguration = a.SubnetConfiguration @ b.SubnetConfiguration
          EnableDnsHostnames =
            match a.EnableDnsHostnames with
            | Some _ -> a.EnableDnsHostnames
            | None -> b.EnableDnsHostnames
          EnableDnsSupport =
            match a.EnableDnsSupport with
            | Some _ -> a.EnableDnsSupport
            | None -> b.EnableDnsSupport
          DefaultInstanceTenancy =
            match a.DefaultInstanceTenancy with
            | Some _ -> a.DefaultInstanceTenancy
            | None -> b.DefaultInstanceTenancy
          IpAddresses =
            match a.IpAddresses with
            | Some _ -> a.IpAddresses
            | None -> b.IpAddresses
          RemovalPolicy =
            match a.RemovalPolicy with
            | Some _ -> a.RemovalPolicy
            | None -> b.RemovalPolicy }

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

        // Default subnet configuration with public and private subnets
        if config.SubnetConfiguration.IsEmpty then
            props.SubnetConfiguration <-
                [| SubnetConfiguration(Name = "Public", SubnetType = SubnetType.PUBLIC, CidrMask = 24)
                   SubnetConfiguration(Name = "Private", SubnetType = SubnetType.PRIVATE_WITH_EGRESS, CidrMask = 24) |]
        else
            props.SubnetConfiguration <- config.SubnetConfiguration |> List.toArray

        config.DefaultInstanceTenancy
        |> Option.iter (fun t -> props.DefaultInstanceTenancy <- t)

        config.IpAddresses |> Option.iter (fun ip -> props.IpAddresses <- ip)

        { VpcName = config.VpcName
          ConstructId = constructId
          Props = props
          Vpc = null }

    /// <summary>Sets the construct ID for the VPC.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     constructId "MyCustomVpc"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: VpcConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the maximum number of Availability Zones to use.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="maxAzs">The maximum number of AZs (default: 2 for HA).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     maxAzs 3
    /// }
    /// </code>
    [<CustomOperation("maxAzs")>]
    member _.MaxAzs(config: VpcConfig, maxAzs: int) = { config with MaxAzs = Some maxAzs }

    /// <summary>Sets the number of NAT Gateways.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="natGateways">The number of NAT gateways (default: 1 for cost optimization).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     natGateways 2
    /// }
    /// </code>
    [<CustomOperation("natGateways")>]
    member _.NatGateways(config: VpcConfig, natGateways: int) =
        { config with
            NatGateways = Some natGateways }

    /// <summary>Adds a subnet configuration.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="subnetConfig">The subnet configuration.</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     subnet (SubnetConfiguration(Name = "Isolated", SubnetType = SubnetType.PRIVATE_ISOLATED, CidrMask = 28))
    /// }
    /// </code>
    [<CustomOperation("subnet")>]
    member _.Subnet(config: VpcConfig, subnetConfig: SubnetConfiguration) =
        { config with
            SubnetConfiguration = subnetConfig :: config.SubnetConfiguration }

    /// <summary>Sets whether to enable DNS hostnames.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="enabled">Whether DNS hostnames are enabled (default: true).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     enableDnsHostnames true
    /// }
    /// </code>
    [<CustomOperation("enableDnsHostnames")>]
    member _.EnableDnsHostnames(config: VpcConfig, enabled: bool) =
        { config with
            EnableDnsHostnames = Some enabled }

    /// <summary>Sets whether to enable DNS support.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="enabled">Whether DNS support is enabled (default: true).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     enableDnsSupport true
    /// }
    /// </code>
    [<CustomOperation("enableDnsSupport")>]
    member _.EnableDnsSupport(config: VpcConfig, enabled: bool) =
        { config with
            EnableDnsSupport = Some enabled }

    /// <summary>Sets the default instance tenancy.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="tenancy">The instance tenancy.</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     defaultInstanceTenancy DefaultInstanceTenancy.DEDICATED
    /// }
    /// </code>
    [<CustomOperation("defaultInstanceTenancy")>]
    member _.DefaultInstanceTenancy(config: VpcConfig, tenancy: DefaultInstanceTenancy) =
        { config with
            DefaultInstanceTenancy = Some tenancy }

    /// <summary>Sets the IP address configuration.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="ipAddresses">The IP addresses configuration.</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///    ipAddresses myIpAddressesConfig
    /// }
    /// </code>
    [<CustomOperation("ipAddresses")>]
    member _.IpAddresses(config: VpcConfig, ipAddresses: IIpAddresses) =
        { config with
            IpAddresses = Some ipAddresses }

    /// <summary>Sets the CIDR block for the VPC.</summary>
    /// <param name="config">The current VPC configuration.</param>
    /// <param name="cidr">The CIDR block (e.g., "10.0.0.0/16").</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     cidr "cidrBlock"
    /// }
    /// </code>
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
      Props: SecurityGroupProps
      mutable SecurityGroup: ISecurityGroup }

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

    member inline x.For
        (
            config: SecurityGroupConfig,
            [<InlineIfLambda>] f: unit -> SecurityGroupConfig
        ) : SecurityGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: SecurityGroupConfig, b: SecurityGroupConfig) : SecurityGroupConfig =
        { SecurityGroupName = a.SecurityGroupName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          Description =
            match a.Description with
            | Some _ -> a.Description
            | None -> b.Description
          AllowAllOutbound =
            match a.AllowAllOutbound with
            | Some _ -> a.AllowAllOutbound
            | None -> b.AllowAllOutbound
          DisableInlineRules =
            match a.DisableInlineRules with
            | Some _ -> a.DisableInlineRules
            | None -> b.DisableInlineRules }

    member _.Run(config: SecurityGroupConfig) : SecurityGroupSpec =
        let props = SecurityGroupProps()
        let constructId = config.ConstructId |> Option.defaultValue config.SecurityGroupName

        // VPC is required
        props.Vpc <-
            match config.Vpc with
            | Some vpcRef -> vpcRef
            | None -> invalidArg "vpc" "VPC is required for Security Group"

        // AWS Best Practice: Least privilege - don't allow all outbound by default
        // Users should explicitly allow what they need
        props.AllowAllOutbound <- config.AllowAllOutbound |> Option.defaultValue false

        config.Description |> Option.iter (fun desc -> props.Description <- desc)

        config.DisableInlineRules
        |> Option.iter (fun d -> props.DisableInlineRules <- d)

        { SecurityGroupName = config.SecurityGroupName
          ConstructId = constructId
          Props = props
          SecurityGroup = null }

    /// <summary>Sets the construct ID for the Security Group.</summary>
    /// <param name="config">The current security group configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     constructId "MyCustomSG"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SecurityGroupConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Security Group.</summary>
    /// <param name="config">The current security group configuration.</param>
    /// <param name="vpc">The VPC.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     vpc myVpc
    /// }
    /// </code>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: SecurityGroupConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Sets the description for the Security Group.</summary>
    /// <param name="config">The current security group configuration.</param>
    /// <param name="description">The description.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     description "Security group for my application"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: SecurityGroupConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets whether to allow all outbound traffic.</summary>
    /// <param name="config">The current security group configuration.</param>
    /// <param name="allow">Whether to allow all outbound (default: false for least privilege).</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     allowAllOutbound false
    /// }
    /// </code>
    [<CustomOperation("allowAllOutbound")>]
    member _.AllowAllOutbound(config: SecurityGroupConfig, allow: bool) =
        { config with
            AllowAllOutbound = Some allow }

    /// <summary>Sets whether to disable inline rules.</summary>
    /// <param name="config">The current security group configuration.</param>
    /// <param name="disable">Whether to disable inline rules.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     disableInlineRules true
    /// }
    /// </code>
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
    let vpc name = VpcBuilder(name)

    /// <summary>Creates a Security Group configuration.</summary>
    /// <param name="name">The Security Group name.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     vpc myVpc
    ///     description "Security group for my application"
    ///     allowAllOutbound false
    /// }
    /// </code>
    let securityGroup name = SecurityGroupBuilder(name)
