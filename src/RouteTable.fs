namespace FsCDK

open Amazon.CDK.AWS.EC2
open Amazon.CDK

// ============================================================================
// Route Table Configuration DSL
// ============================================================================

/// <summary>
/// High-level EC2 Route Table builder following AWS networking best practices.
///
/// **Default Settings:**
/// - No default routes (explicit route configuration required)
///
/// **Rationale:**
/// Explicit route configuration follows the principle of least privilege
/// and prevents accidental traffic routing.
///
/// **Escape Hatch:**
/// Access the underlying CDK CfnRouteTable via the `RouteTable` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type RouteTableConfig =
    { RouteTableName: string
      ConstructId: string option
      Vpc: IVpc option
      Tags: (string * string) list }

type RouteTableSpec =
    { RouteTableName: string
      ConstructId: string
      Props: CfnRouteTableProps
      mutable RouteTable: CfnRouteTable option }

    /// Gets the underlying CfnRouteTable resource. Must be called after the stack is built.
    member this.Resource =
        match this.RouteTable with
        | Some rt -> rt
        | None ->
            failwith
                $"RouteTable '{this.RouteTableName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type RouteTableBuilder(name: string) =
    member _.Yield _ : RouteTableConfig =
        { RouteTableName = name
          ConstructId = None
          Vpc = None
          Tags = [] }

    member _.Zero() : RouteTableConfig =
        { RouteTableName = name
          ConstructId = None
          Vpc = None
          Tags = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> RouteTableConfig) : RouteTableConfig = f ()

    member inline x.For(config: RouteTableConfig, [<InlineIfLambda>] f: unit -> RouteTableConfig) : RouteTableConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: RouteTableConfig, b: RouteTableConfig) : RouteTableConfig =
        { RouteTableName = a.RouteTableName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          Tags = a.Tags @ b.Tags }

    member _.Run(config: RouteTableConfig) : RouteTableSpec =
        let props = CfnRouteTableProps()
        let constructId = config.ConstructId |> Option.defaultValue config.RouteTableName

        // VPC is required
        props.VpcId <-
            match config.Vpc with
            | Some vpc -> vpc.VpcId
            | None -> invalidArg "vpc" "VPC is required for Route Table"

        if not (List.isEmpty config.Tags) then
            props.Tags <-
                config.Tags
                |> List.map (fun (key, value) -> CfnTag(Key = key, Value = value) :> ICfnTag)
                |> List.toArray

        { RouteTableName = config.RouteTableName
          ConstructId = constructId
          Props = props
          RouteTable = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: RouteTableConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the route table.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: RouteTableConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Adds a tag to the route table.</summary>
    [<CustomOperation("tag")>]
    member _.Tag(config: RouteTableConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

// ============================================================================
// Route Configuration DSL
// ============================================================================

type RouteConfig =
    { RouteName: string
      ConstructId: string option
      RouteTable: CfnRouteTable option
      DestinationCidrBlock: string option
      DestinationIpv6CidrBlock: string option
      GatewayId: string option
      NatGatewayId: string option
      NetworkInterfaceId: string option
      VpcPeeringConnectionId: string option
      TransitGatewayId: string option }

type RouteSpec =
    { RouteName: string
      ConstructId: string
      Props: CfnRouteProps }

type RouteBuilder(name: string) =
    member _.Yield _ : RouteConfig =
        { RouteName = name
          ConstructId = None
          RouteTable = None
          DestinationCidrBlock = None
          DestinationIpv6CidrBlock = None
          GatewayId = None
          NatGatewayId = None
          NetworkInterfaceId = None
          VpcPeeringConnectionId = None
          TransitGatewayId = None }

    member _.Zero() : RouteConfig =
        { RouteName = name
          ConstructId = None
          RouteTable = None
          DestinationCidrBlock = None
          DestinationIpv6CidrBlock = None
          GatewayId = None
          NatGatewayId = None
          NetworkInterfaceId = None
          VpcPeeringConnectionId = None
          TransitGatewayId = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> RouteConfig) : RouteConfig = f ()

    member _.Combine(a: RouteConfig, b: RouteConfig) : RouteConfig =
        { RouteName = a.RouteName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          RouteTable =
            match a.RouteTable with
            | Some _ -> a.RouteTable
            | None -> b.RouteTable
          DestinationCidrBlock =
            match a.DestinationCidrBlock with
            | Some _ -> a.DestinationCidrBlock
            | None -> b.DestinationCidrBlock
          DestinationIpv6CidrBlock =
            match a.DestinationIpv6CidrBlock with
            | Some _ -> a.DestinationIpv6CidrBlock
            | None -> b.DestinationIpv6CidrBlock
          GatewayId =
            match a.GatewayId with
            | Some _ -> a.GatewayId
            | None -> b.GatewayId
          NatGatewayId =
            match a.NatGatewayId with
            | Some _ -> a.NatGatewayId
            | None -> b.NatGatewayId
          NetworkInterfaceId =
            match a.NetworkInterfaceId with
            | Some _ -> a.NetworkInterfaceId
            | None -> b.NetworkInterfaceId
          VpcPeeringConnectionId =
            match a.VpcPeeringConnectionId with
            | Some _ -> a.VpcPeeringConnectionId
            | None -> b.VpcPeeringConnectionId
          TransitGatewayId =
            match a.TransitGatewayId with
            | Some _ -> a.TransitGatewayId
            | None -> b.TransitGatewayId }

    member _.Run(config: RouteConfig) : RouteSpec =
        let props = CfnRouteProps()
        let constructId = config.ConstructId |> Option.defaultValue config.RouteName

        // Route table is required
        props.RouteTableId <-
            match config.RouteTable with
            | Some rt -> rt.Ref
            | None -> invalidArg "routeTable" "Route table is required for Route"

        // Either IPv4 or IPv6 destination is required
        match config.DestinationCidrBlock, config.DestinationIpv6CidrBlock with
        | None, None -> invalidArg "destination" "Either IPv4 or IPv6 destination CIDR block is required"
        | Some cidr, _ -> props.DestinationCidrBlock <- cidr
        | _, Some ipv6 -> props.DestinationIpv6CidrBlock <- ipv6

        config.GatewayId |> Option.iter (fun id -> props.GatewayId <- id)
        config.NatGatewayId |> Option.iter (fun id -> props.NatGatewayId <- id)

        config.NetworkInterfaceId
        |> Option.iter (fun id -> props.NetworkInterfaceId <- id)

        config.VpcPeeringConnectionId
        |> Option.iter (fun id -> props.VpcPeeringConnectionId <- id)

        config.TransitGatewayId |> Option.iter (fun id -> props.TransitGatewayId <- id)

        { RouteName = config.RouteName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: RouteConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the route table.</summary>
    [<CustomOperation("routeTable")>]
    member _.RouteTable(config: RouteConfig, routeTable: CfnRouteTable) =
        { config with
            RouteTable = Some routeTable }

    /// <summary>Sets the destination IPv4 CIDR block.</summary>
    [<CustomOperation("destinationCidrBlock")>]
    member _.DestinationCidrBlock(config: RouteConfig, cidr: string) =
        { config with
            DestinationCidrBlock = Some cidr }

    /// <summary>Sets the destination IPv6 CIDR block.</summary>
    [<CustomOperation("destinationIpv6CidrBlock")>]
    member _.DestinationIpv6CidrBlock(config: RouteConfig, cidr: string) =
        { config with
            DestinationIpv6CidrBlock = Some cidr }

    /// <summary>Sets the gateway ID (Internet Gateway or Virtual Private Gateway).</summary>
    [<CustomOperation("gatewayId")>]
    member _.GatewayId(config: RouteConfig, id: string) = { config with GatewayId = Some id }

    /// <summary>Sets the NAT gateway ID.</summary>
    [<CustomOperation("natGatewayId")>]
    member _.NatGatewayId(config: RouteConfig, id: string) = { config with NatGatewayId = Some id }

    /// <summary>Sets the network interface ID.</summary>
    [<CustomOperation("networkInterfaceId")>]
    member _.NetworkInterfaceId(config: RouteConfig, id: string) =
        { config with
            NetworkInterfaceId = Some id }

    /// <summary>Sets the VPC peering connection ID.</summary>
    [<CustomOperation("vpcPeeringConnectionId")>]
    member _.VpcPeeringConnectionId(config: RouteConfig, id: string) =
        { config with
            VpcPeeringConnectionId = Some id }

    /// <summary>Sets the transit gateway ID.</summary>
    [<CustomOperation("transitGatewayId")>]
    member _.TransitGatewayId(config: RouteConfig, id: string) =
        { config with
            TransitGatewayId = Some id }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module RouteTableBuilders =
    /// <summary>Creates a route table with AWS networking best practices.</summary>
    /// <param name="name">The route table name.</param>
    /// <code lang="fsharp">
    /// routeTable "MyRouteTable" {
    ///     vpc myVpc
    ///     tag "Name" "custom-route-table"
    /// }
    /// </code>
    let routeTable (name: string) = RouteTableBuilder name

    /// <summary>Creates a route in a route table.</summary>
    /// <param name="name">The route name.</param>
    /// <code lang="fsharp">
    /// route "InternetRoute" {
    ///     routeTable myRouteTable
    ///     destinationCidrBlock "0.0.0.0/0"
    ///     gatewayId myInternetGateway.Ref
    /// }
    /// </code>
    let route (name: string) = RouteBuilder name
