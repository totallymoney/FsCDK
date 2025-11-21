namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EC2

// ============================================================================
// Resource Reference Types - for cross-referencing resources in the same stack
// ============================================================================

type VpcSpec =
    { VpcName: string
      ConstructId: string
      Props: VpcProps
      EnableFlowLogs: bool
      FlowLogRetention: Amazon.CDK.AWS.Logs.RetentionDays option
      mutable Vpc: IVpc option }

type SecurityGroupSpec =
    { SecurityGroupName: string
      ConstructId: string
      Props: SecurityGroupProps
      mutable SecurityGroup: ISecurityGroup option }

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
      RemovalPolicy: RemovalPolicy option
      EnableFlowLogs: bool option
      FlowLogRetention: Amazon.CDK.AWS.Logs.RetentionDays option }

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
          RemovalPolicy = None
          EnableFlowLogs = Some true
          FlowLogRetention = Some Amazon.CDK.AWS.Logs.RetentionDays.ONE_WEEK }

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
          RemovalPolicy = None
          EnableFlowLogs = Some true
          FlowLogRetention = Some Amazon.CDK.AWS.Logs.RetentionDays.ONE_WEEK }

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
            | None -> b.RemovalPolicy
          EnableFlowLogs =
            match a.EnableFlowLogs with
            | Some _ -> a.EnableFlowLogs
            | None -> b.EnableFlowLogs
          FlowLogRetention =
            match a.FlowLogRetention with
            | Some _ -> a.FlowLogRetention
            | None -> b.FlowLogRetention }

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
          EnableFlowLogs = config.EnableFlowLogs |> Option.defaultValue true
          FlowLogRetention = config.FlowLogRetention
          Vpc = None }

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
    ///     subnet (SubnetConfiguration(Name = "Public", SubnetType = SubnetType.PUBLIC, CidrMask = 24))
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
    ///     enableDnsHostnames false
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
    ///     enableDnsSupport false
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
    ///     ipAddresses (IpAddresses.Cidr "10.0.0.0/16")
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
    ///     cidr "10.0.0.0/16"
    /// }
    /// </code>
    [<CustomOperation("cidr")>]
    member _.Cidr(config: VpcConfig, cidr: string) =
        { config with
            IpAddresses = Some(IpAddresses.Cidr(cidr)) }

    /// <summary>
    /// Enables or disables VPC Flow Logs for network traffic monitoring.
    /// **Security Best Practice:** Flow logs are enabled by default for security monitoring and compliance.
    /// Flow logs capture information about IP traffic going to and from network interfaces in your VPC.
    /// </summary>
    /// <param name="enabled">Whether to enable flow logs (default: true).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     // Disable flow logs if not needed
    ///     enableFlowLogs false
    /// }
    /// </code>
    [<CustomOperation("enableFlowLogs")>]
    member _.EnableFlowLogs(config: VpcConfig, enabled: bool) =
        { config with
            EnableFlowLogs = Some enabled }

    /// <summary>
    /// Sets the retention period for VPC Flow Logs in CloudWatch.
    /// </summary>
    /// <param name="retention">The retention period (default: ONE_WEEK for cost optimization).</param>
    /// <code lang="fsharp">
    /// vpc "MyVpc" {
    ///     flowLogRetention RetentionDays.ONE_MONTH
    /// }
    /// </code>
    [<CustomOperation("flowLogRetention")>]
    member _.FlowLogRetention(config: VpcConfig, retention: Amazon.CDK.AWS.Logs.RetentionDays) =
        { config with
            FlowLogRetention = Some retention }

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
            | Some vpc -> vpc
            | None -> invalidArg "vpc" "VPC is required for Security Group"

        // AWS Best Practice: the least privilege - don't allow all outbound by default
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
    /// <param name="config">The current Security Group configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     constructId "MyCustomSG"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: SecurityGroupConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Security Group.</summary>
    /// <param name="config">The current Security Group configuration.</param>
    /// <param name="vpc">The VPC.</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     vpc myVpc
    /// }
    /// </code>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: SecurityGroupConfig, vpc: IVpc) = { config with Vpc = Some vpc }

    /// <summary>Sets the description for the Security Group.</summary>
    /// <param name="config">The current Security Group configuration.</param>
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
    /// <param name="config">The current Security Group configuration.</param>
    /// <param name="allow">Whether to allow all outbound (default: false for least privilege).</param>
    /// <code lang="fsharp">
    /// securityGroup "MySecurityGroup" {
    ///     allowAllOutbound true
    /// }
    /// </code>
    [<CustomOperation("allowAllOutbound")>]
    member _.AllowAllOutbound(config: SecurityGroupConfig, allow: bool) =
        { config with
            AllowAllOutbound = Some allow }

    /// <summary>Sets whether to disable inline rules.</summary>
    /// <param name="config">The current Security Group configuration.</param>
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
// VPC Endpoint Configuration DSL
// ============================================================================

type GatewayVpcEndpointConfig =
    { EndpointName: string
      ConstructId: string option
      Vpc: IVpc option
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
        | Some vpc -> props.Vpc <- vpc
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
    /// <param name="config">The current Gateway VPC Endpoint configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     constructId "MyEndpointId"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: GatewayVpcEndpointConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the endpoint.</summary>
    /// <param name="config">The current Gateway VPC Endpoint configuration.</param>
    /// <param name="vpc">The VPC.</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     vpc myVpc
    /// }
    /// </code>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: GatewayVpcEndpointConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Sets the service for the endpoint (e.g., S3, DynamoDB).</summary>
    /// <param name="config">The current Gateway VPC Endpoint configuration.</param>
    /// <param name="service">The gateway endpoint service (e.g., GatewayVpcEndpointAwsService.S3).</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     service GatewayVpcEndpointAwsService.S3
    /// }
    /// </code>
    [<CustomOperation("service")>]
    member _.Service(config: GatewayVpcEndpointConfig, service: IGatewayVpcEndpointService) =
        { config with Service = Some service }

    /// <summary>Adds subnet selection for the endpoint.</summary>
    /// <param name="config">The current Gateway VPC Endpoint configuration.</param>
    /// <param name="subnet">The subnet selection.</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     subnet (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
    /// }
    /// </code>
    [<CustomOperation("subnet")>]
    member _.Subnet(config: GatewayVpcEndpointConfig, subnet: SubnetSelection) =
        { config with
            Subnets = subnet :: config.Subnets }

    /// <summary>Adds multiple subnets for the endpoint.</summary>
    /// <param name="config">The current Gateway VPC Endpoint configuration.</param>
    /// <param name="subnets">The list of subnet selections.</param>
    /// <code lang="fsharp">
    /// gatewayVpcEndpoint "S3Endpoint" {
    ///     subnets [ SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS) ]
    /// }
    /// </code>
    [<CustomOperation("subnets")>]
    member _.Subnets(config: GatewayVpcEndpointConfig, subnets: SubnetSelection list) =
        { config with
            Subnets = subnets @ config.Subnets }

type InterfaceVpcEndpointConfig =
    { EndpointName: string
      ConstructId: string option
      Vpc: IVpc option
      Service: IInterfaceVpcEndpointService option
      PrivateDnsEnabled: bool option
      SecurityGroups: ISecurityGroup list
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
          SecurityGroups = state1.SecurityGroups @ state2.SecurityGroups
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
        | Some vpc -> props.Vpc <- vpc
        | None -> invalidArg "vpc" "VPC is required for Interface VPC Endpoint"

        match config.Service with
        | Some service -> props.Service <- service
        | None -> invalidArg "service" "Service is required for Interface VPC Endpoint"

        config.PrivateDnsEnabled |> Option.iter (fun v -> props.PrivateDnsEnabled <- v)

        if not config.SecurityGroups.IsEmpty then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.Subnets |> Option.iter (fun v -> props.Subnets <- v)

        { EndpointName = endpointName
          ConstructId = constructId
          Props = props
          VpcEndpoint = None }

    /// <summary>Sets a custom construct ID.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     constructId "MyInterfaceEndpointId"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: InterfaceVpcEndpointConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="vpc">The VPC.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     vpc myVpc
    /// }
    /// </code>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: InterfaceVpcEndpointConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Sets the service for the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="service">The interface endpoint service (e.g., InterfaceVpcEndpointAwsService.SECRETS_MANAGER).</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     service InterfaceVpcEndpointAwsService.SECRETS_MANAGER
    /// }
    /// </code>
    [<CustomOperation("service")>]
    member _.Service(config: InterfaceVpcEndpointConfig, service: IInterfaceVpcEndpointService) =
        { config with Service = Some service }

    /// <summary>Controls whether to enable private DNS for the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="enabled">Whether to enable private DNS (default: true).</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     privateDnsEnabled true
    /// }
    /// </code>
    [<CustomOperation("privateDnsEnabled")>]
    member _.PrivateDnsEnabled(config: InterfaceVpcEndpointConfig, enabled: bool) =
        { config with
            PrivateDnsEnabled = Some enabled }

    /// <summary>Adds a security group to the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="sg">The security group.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     securityGroup mySecurityGroup
    /// }
    /// </code>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: InterfaceVpcEndpointConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroups = sg :: config.SecurityGroups }

    /// <summary>Adds multiple security groups to the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="sgs">The list of security groups.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     securityGroups [ sg1; sg2 ]
    /// }
    /// </code>
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: InterfaceVpcEndpointConfig, sgs: ISecurityGroup list) =
        { config with
            SecurityGroups = sgs @ config.SecurityGroups }

    /// <summary>Sets the subnets for the endpoint.</summary>
    /// <param name="config">The current Interface VPC Endpoint configuration.</param>
    /// <param name="subnets">The subnet selection.</param>
    /// <code lang="fsharp">
    /// interfaceVpcEndpoint "SecretsManagerEndpoint" {
    ///     subnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
    /// }
    /// </code>
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
