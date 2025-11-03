namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EC2

// ============================================================================
// Resource Reference Types - for cross-referencing resources in the same stack
// ============================================================================

/// Represents a reference to a VPC that can be resolved later
type VpcRef =
    | VpcInterface of IVpc
    | VpcSpecRef of VpcSpec

// Forward declaration - VpcSpec is defined below
and VpcSpec =
    { VpcName: string
      ConstructId: string
      Props: VpcProps
      mutable Vpc: IVpc option }

    /// Gets the underlying IVpc resource. Must be called after the stack is built.
    member this.Resource =
        match this.Vpc with
        | Some vpc -> vpc
        | None ->
            failwith
                $"VPC '{this.VpcName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type SecurityGroupSpec =
    { SecurityGroupName: string
      ConstructId: string
      Props: SecurityGroupProps
      mutable SecurityGroup: ISecurityGroup option }

type SecurityGroupRef =
    | SecurityGroupInterface of ISecurityGroup
    | SecurityGroupSpecRef of SecurityGroupSpec

module VpcHelpers =
    /// Resolves a VPC reference to an IVpc
    let resolveVpcRef (ref: VpcRef) =
        match ref with
        | VpcInterface vpc -> vpc
        | VpcSpecRef spec ->
            match spec.Vpc with
            | Some vpc -> vpc
            | None ->
                failwith
                    $"VPC '{spec.VpcName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

    /// Resolves a VPC reference to an IVpc
    let resolveSecurityGroupRef (ref: SecurityGroupRef) =
        match ref with
        | SecurityGroupInterface sgi -> sgi
        | SecurityGroupSpecRef spec ->
            match spec.SecurityGroup with
            | Some sg -> sg
            | None ->
                failwith
                    $"SecurityGroup '{spec.SecurityGroupName}' has not been created yet. Ensure it's yielded in the stack before referencing it."


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
          Vpc = None }

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
      Vpc: VpcRef option
      Description: string option
      AllowAllOutbound: bool option
      DisableInlineRules: bool option }

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
            | Some vpcRef -> VpcHelpers.resolveVpcRef vpcRef
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
          SecurityGroup = None }

    /// <summary>Sets the construct ID for the Security Group.</summary>
    /// <param name="id">The construct ID.</param>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SecurityGroupConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Security Group.</summary>
    /// <param name="vpc">The VPC.</param>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: SecurityGroupConfig, vpc: IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    /// <summary>Sets the VPC for the Security Group from a VpcSpec.</summary>
    /// <param name="vpcSpec">The VPC specification.</param>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: SecurityGroupConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

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
// VPC Endpoint Configuration DSL
// ============================================================================

type GatewayVpcEndpointConfig =
    { EndpointName: string
      ConstructId: string option
      Vpc: VpcRef option
      Service: IGatewayVpcEndpointService option
      Subnets: SubnetSelection list }

type GatewayVpcEndpointSpec =
    { EndpointName: string
      ConstructId: string
      Props: GatewayVpcEndpointProps
      mutable VpcEndpoint: IGatewayVpcEndpoint option }

    /// Gets the underlying IGatewayVpcEndpoint resource. Must be called after the stack is built.
    member this.Resource =
        match this.VpcEndpoint with
        | Some ep -> ep
        | None ->
            failwith
                $"GatewayVpcEndpoint '{this.EndpointName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type GatewayVpcEndpointBuilder(name: string) =

    member _.Yield _ : GatewayVpcEndpointConfig =
        { EndpointName = name
          ConstructId = None
          Vpc = None
          Service = None
          Subnets = [] }

    member _.Zero() : GatewayVpcEndpointConfig =
        { EndpointName = name
          ConstructId = None
          Vpc = None
          Service = None
          Subnets = [] }

    member _.Combine(state1: GatewayVpcEndpointConfig, state2: GatewayVpcEndpointConfig) : GatewayVpcEndpointConfig =
        { EndpointName = state2.EndpointName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          Service = state2.Service |> Option.orElse state1.Service
          Subnets =
            if state2.Subnets.IsEmpty then
                state1.Subnets
            else
                state2.Subnets @ state1.Subnets }

    member inline _.Delay([<InlineIfLambda>] f: unit -> GatewayVpcEndpointConfig) : GatewayVpcEndpointConfig = f ()

    member inline x.For
        (
            config: GatewayVpcEndpointConfig,
            [<InlineIfLambda>] f: unit -> GatewayVpcEndpointConfig
        ) : GatewayVpcEndpointConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: GatewayVpcEndpointConfig) : GatewayVpcEndpointSpec =
        let endpointName = config.EndpointName
        let constructId = config.ConstructId |> Option.defaultValue endpointName

        let props = GatewayVpcEndpointProps()

        match config.Vpc with
        | Some vpc -> props.Vpc <- VpcHelpers.resolveVpcRef vpc
        | None -> invalidArg "vpc" "VPC is required for Gateway VPC Endpoint"

        match config.Service with
        | Some service -> props.Service <- service
        | None -> invalidArg "service" "Service is required for Gateway VPC Endpoint"

        if not config.Subnets.IsEmpty then
            props.Subnets <- config.Subnets |> List.map (fun s -> s :> ISubnetSelection) |> Array.ofList

        { EndpointName = endpointName
          ConstructId = constructId
          Props = props
          VpcEndpoint = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: GatewayVpcEndpointConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the endpoint.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: GatewayVpcEndpointConfig, vpc: IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    /// <summary>Sets the VPC for the endpoint from a VpcSpec.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: GatewayVpcEndpointConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

    /// <summary>Sets the service for the endpoint (e.g., S3, DynamoDB).</summary>
    [<CustomOperation("service")>]
    member _.Service(config: GatewayVpcEndpointConfig, service: IGatewayVpcEndpointService) =
        { config with Service = Some service }

    /// <summary>Adds subnet selection for the endpoint.</summary>
    [<CustomOperation("subnet")>]
    member _.Subnet(config: GatewayVpcEndpointConfig, subnet: SubnetSelection) =
        { config with
            Subnets = subnet :: config.Subnets }

    /// <summary>Adds multiple subnets for the endpoint.</summary>
    [<CustomOperation("subnets")>]
    member _.Subnets(config: GatewayVpcEndpointConfig, subnets: SubnetSelection list) =
        { config with
            Subnets = subnets @ config.Subnets }

type InterfaceVpcEndpointConfig =
    { EndpointName: string
      ConstructId: string option
      Vpc: VpcRef option
      Service: IInterfaceVpcEndpointService option
      PrivateDnsEnabled: bool option
      SecurityGroups: SecurityGroupRef list
      Subnets: SubnetSelection option }

type InterfaceVpcEndpointSpec =
    { EndpointName: string
      ConstructId: string
      Props: InterfaceVpcEndpointProps
      mutable VpcEndpoint: IInterfaceVpcEndpoint option }

    /// Gets the underlying IInterfaceVpcEndpoint resource. Must be called after the stack is built.
    member this.Resource =
        match this.VpcEndpoint with
        | Some ep -> ep
        | None ->
            failwith
                $"InterfaceVpcEndpoint '{this.EndpointName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type InterfaceVpcEndpointBuilder(name: string) =

    member _.Yield _ : InterfaceVpcEndpointConfig =
        { EndpointName = name
          ConstructId = None
          Vpc = None
          Service = None
          PrivateDnsEnabled = Some true
          SecurityGroups = []
          Subnets = None }

    member _.Zero() : InterfaceVpcEndpointConfig =
        { EndpointName = name
          ConstructId = None
          Vpc = None
          Service = None
          PrivateDnsEnabled = Some true
          SecurityGroups = []
          Subnets = None }

    member _.Combine
        (
            state1: InterfaceVpcEndpointConfig,
            state2: InterfaceVpcEndpointConfig
        ) : InterfaceVpcEndpointConfig =
        { EndpointName = state2.EndpointName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          Service = state2.Service |> Option.orElse state1.Service
          PrivateDnsEnabled = state2.PrivateDnsEnabled |> Option.orElse state1.PrivateDnsEnabled
          SecurityGroups =
            if state2.SecurityGroups.IsEmpty then
                state1.SecurityGroups
            else
                state2.SecurityGroups @ state1.SecurityGroups
          Subnets = state2.Subnets |> Option.orElse state1.Subnets }

    member inline _.Delay([<InlineIfLambda>] f: unit -> InterfaceVpcEndpointConfig) : InterfaceVpcEndpointConfig = f ()

    member inline x.For
        (
            config: InterfaceVpcEndpointConfig,
            [<InlineIfLambda>] f: unit -> InterfaceVpcEndpointConfig
        ) : InterfaceVpcEndpointConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: InterfaceVpcEndpointConfig) : InterfaceVpcEndpointSpec =
        let endpointName = config.EndpointName
        let constructId = config.ConstructId |> Option.defaultValue endpointName

        let props = InterfaceVpcEndpointProps()

        match config.Vpc with
        | Some vpc -> props.Vpc <- VpcHelpers.resolveVpcRef vpc
        | None -> invalidArg "vpc" "VPC is required for Interface VPC Endpoint"

        match config.Service with
        | Some service -> props.Service <- service
        | None -> invalidArg "service" "Service is required for Interface VPC Endpoint"

        config.PrivateDnsEnabled |> Option.iter (fun v -> props.PrivateDnsEnabled <- v)

        if not config.SecurityGroups.IsEmpty then
            props.SecurityGroups <-
                config.SecurityGroups
                |> List.map VpcHelpers.resolveSecurityGroupRef
                |> Array.ofList

        config.Subnets |> Option.iter (fun v -> props.Subnets <- v)

        { EndpointName = endpointName
          ConstructId = constructId
          Props = props
          VpcEndpoint = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: InterfaceVpcEndpointConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the endpoint.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: InterfaceVpcEndpointConfig, vpc: IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    /// <summary>Sets the VPC for the endpoint from a VpcSpec.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: InterfaceVpcEndpointConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

    /// <summary>Sets the service for the endpoint.</summary>
    [<CustomOperation("service")>]
    member _.Service(config: InterfaceVpcEndpointConfig, service: IInterfaceVpcEndpointService) =
        { config with Service = Some service }

    /// <summary>Controls whether to enable private DNS for the endpoint.</summary>
    [<CustomOperation("privateDnsEnabled")>]
    member _.PrivateDnsEnabled(config: InterfaceVpcEndpointConfig, enabled: bool) =
        { config with
            PrivateDnsEnabled = Some enabled }

    /// <summary>Adds a security group to the endpoint.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: InterfaceVpcEndpointConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroups = SecurityGroupInterface sg :: config.SecurityGroups }

    /// <summary>Adds a security group to the endpoint from a SecurityGroupSpec.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: InterfaceVpcEndpointConfig, sg: SecurityGroupSpec) =
        { config with
            SecurityGroups = SecurityGroupSpecRef sg :: config.SecurityGroups }

    /// <summary>Adds multiple security groups to the endpoint.</summary>
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: InterfaceVpcEndpointConfig, sgs: ISecurityGroup list) =
        { config with
            SecurityGroups = (sgs |> List.map SecurityGroupInterface) @ config.SecurityGroups }

    /// <summary>Sets the subnets for the endpoint.</summary>
    [<CustomOperation("subnets")>]
    member _.Subnets(config: InterfaceVpcEndpointConfig, subnets: SubnetSelection) =
        { config with Subnets = Some subnets }

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

    /// <summary>Creates a Gateway VPC Endpoint (for S3, DynamoDB).</summary>
    /// <param name="name">The endpoint name.</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     vpc myVpc
    ///     service GatewayVpcEndpointAwsService.S3
    /// }
    /// </code>
    let gatewayVpcEndpoint (name: string) = GatewayVpcEndpointBuilder(name)

    /// <summary>Creates an Interface VPC Endpoint (for most AWS services).</summary>
    /// <param name="name">The endpoint name.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     vpc myVpc
    ///     service InterfaceVpcEndpointAwsService.SECRETS_MANAGER
    ///     privateDnsEnabled true
    /// }
    /// </code>
    let interfaceVpcEndpoint (name: string) = InterfaceVpcEndpointBuilder(name)
