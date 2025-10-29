namespace FsCDK

open Amazon.CDK.AWS.EC2

// ============================================================================
// VPC Gateway Attachment Configuration DSL
// ============================================================================

/// <summary>
/// High-level VPC Gateway Attachment builder for connecting internet and VPN gateways to VPCs.
///
/// **Use Cases: **
/// - Attach an Internet Gateway to enable internet access
/// - Attach a Virtual Private Gateway for VPN connections
///
/// **Rationale: **
/// Explicit gateway attachments provide fine-grained control over network connectivity
/// and follow the principle of explicit configuration.
///
/// **Escape Hatch: **
/// Access the underlying CDK VPCGatewayAttachment via the `Attachment` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type VPCGatewayAttachmentConfig =
    { AttachmentName: string
      ConstructId: string option
      Vpc: IVpc option
      InternetGateway: CfnInternetGateway option
      VpnGateway: CfnVPNGateway option }

type VPCGatewayAttachmentSpec =
    { AttachmentName: string
      ConstructId: string
      VpcId: string
      InternetGatewayId: string option
      VpnGatewayId: string option
      mutable Attachment: CfnVPCGatewayAttachment option }

type VPCGatewayAttachmentBuilder(name: string) =
    member _.Yield _ : VPCGatewayAttachmentConfig =
        { AttachmentName = name
          ConstructId = None
          Vpc = None
          InternetGateway = None
          VpnGateway = None }

    member _.Zero() : VPCGatewayAttachmentConfig =
        { AttachmentName = name
          ConstructId = None
          Vpc = None
          InternetGateway = None
          VpnGateway = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> VPCGatewayAttachmentConfig) : VPCGatewayAttachmentConfig = f ()

    member inline x.For
        (
            config: VPCGatewayAttachmentConfig,
            [<InlineIfLambda>] f: unit -> VPCGatewayAttachmentConfig
        ) : VPCGatewayAttachmentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: VPCGatewayAttachmentConfig, b: VPCGatewayAttachmentConfig) : VPCGatewayAttachmentConfig =
        { AttachmentName = a.AttachmentName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          InternetGateway =
            match a.InternetGateway with
            | Some _ -> a.InternetGateway
            | None -> b.InternetGateway
          VpnGateway =
            match a.VpnGateway with
            | Some _ -> a.VpnGateway
            | None -> b.VpnGateway }

    member _.Run(config: VPCGatewayAttachmentConfig) : VPCGatewayAttachmentSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.AttachmentName

        // VPC is required
        let vpcId =
            match config.Vpc with
            | Some vpcRef ->
                let vpc = vpcRef
                vpc.VpcId
            | None -> invalidArg "vpc" "VPC is required for VPC Gateway Attachment"

        // Either internet gateway or VPN gateway must be specified
        if config.InternetGateway.IsNone && config.VpnGateway.IsNone then
            invalidArg "gateway" "Either Internet Gateway or VPN Gateway must be specified"

        { AttachmentName = config.AttachmentName
          ConstructId = constructId
          VpcId = vpcId
          InternetGatewayId = config.InternetGateway |> Option.map (fun ig -> ig.Ref)
          VpnGatewayId = config.VpnGateway |> Option.map (fun vg -> vg.Ref)
          Attachment = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: VPCGatewayAttachmentConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC to attach the gateway to.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: VPCGatewayAttachmentConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    // /// <summary>Sets the VPC from a VpcSpec.</summary>
    // [<CustomOperation("vpc")>]
    // member _.Vpc(config: VPCGatewayAttachmentConfig, vpcSpec: VpcSpec) =
    //     { config with
    //         Vpc = Some(VpcSpecRef vpcSpec) }

    /// <summary>Sets the Internet Gateway to attach.</summary>
    [<CustomOperation("internetGateway")>]
    member _.InternetGateway(config: VPCGatewayAttachmentConfig, gateway: CfnInternetGateway) =
        { config with
            InternetGateway = Some gateway }

    /// <summary>Sets the VPN Gateway to attach.</summary>
    [<CustomOperation("vpnGateway")>]
    member _.VpnGateway(config: VPCGatewayAttachmentConfig, gateway: CfnVPNGateway) =
        { config with
            VpnGateway = Some gateway }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module VPCGatewayAttachmentBuilders =
    /// <summary>Creates a VPC Gateway Attachment.</summary>
    /// <param name="name">The attachment name.</param>
    /// <code lang="fsharp">
    /// vpcGatewayAttachment "MyIGWAttachment" {
    ///     vpc myVpc
    ///     internetGateway myInternetGateway
    /// }
    /// </code>
    let vpcGatewayAttachment (name: string) = VPCGatewayAttachmentBuilder name
