// This is a template for future support for CloudHSM.
// HSM is Hardware Security Module
// E.g., a chip & pin machine in the cloud, for digital bank signatures

// As this is an expensive and non-commonly used service,
// this template is just a startup point
// if someone ever needs to implement it.

namespace FsCDK

(*
open Amazon.CDK.AWS.CloudHSMV2
open Amazon.CDK.AWS.EC2

// ============================================================================
// CloudHSM Configuration DSL
// ============================================================================

/// <summary>
/// High-level CloudHSM Cluster builder for hardware security module deployment.
///
/// **Security Considerations:**
/// - CloudHSM provides FIPS 140-2 Level 3 validated HSMs
/// - Suitable for cryptographic key management and regulatory compliance
/// - Runs in your VPC for network isolation
/// - High availability requires multiple HSMs across AZs
///
/// **Cost Warning:**
/// CloudHSM is an expensive service (~$1.60/hour per HSM + data transfer)
/// Only use when regulatory requirements mandate hardware-based key storage
///
/// **Use Cases:**
/// - PCI-DSS compliance for payment processing
/// - HIPAA requirements for healthcare data
/// - Government/financial services with strict key management needs
/// </summary>
type CloudHSMClusterConfig =
    { ClusterName: string
      ConstructId: string option
      Vpc: VpcRef option
      HsmType: string option
      SubnetIds: string list }

type CloudHSMClusterSpec =
    { ClusterName: string
      ConstructId: string
      Props: CfnClusterProps }

type CloudHSMClusterBuilder(name: string) =
    member _.Yield _ : CloudHSMClusterConfig =
        { ClusterName = name
          ConstructId = None
          Vpc = None
          HsmType = Some "hsm1.medium" // Default HSM type
          SubnetIds = [] }

    member _.Zero() : CloudHSMClusterConfig =
        { ClusterName = name
          ConstructId = None
          Vpc = None
          HsmType = Some "hsm1.medium"
          SubnetIds = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudHSMClusterConfig): CloudHSMClusterConfig = f ()

    member _.Combine(state1: CloudHSMClusterConfig, state2: CloudHSMClusterConfig): CloudHSMClusterConfig =
        { ClusterName = state1.ClusterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          HsmType = state2.HsmType |> Option.orElse state1.HsmType
          SubnetIds =
            if state2.SubnetIds.IsEmpty then state1.SubnetIds
            else state2.SubnetIds }

    member inline x.For
        (
            config: CloudHSMClusterConfig,
            [<InlineIfLambda>] f: unit -> CloudHSMClusterConfig
        ) : CloudHSMClusterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudHSMClusterConfig) : CloudHSMClusterSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.ClusterName

        if config.SubnetIds.IsEmpty then
            failwith "CloudHSM cluster requires at least one subnet ID"

        let props = CfnClusterProps()
        props.HsmType <- config.HsmType |> Option.defaultValue "hsm1.medium"
        props.SubnetIds <- config.SubnetIds |> List.toArray

        { ClusterName = config.ClusterName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the CloudHSM cluster.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudHSMClusterConfig, id: string) =
        { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the CloudHSM cluster.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: CloudHSMClusterConfig, vpc: VpcRef) =
        { config with Vpc = Some vpc }

    /// <summary>Sets the HSM type (hsm1.medium is default and most common).</summary>
    [<CustomOperation("hsmType")>]
    member _.HsmType(config: CloudHSMClusterConfig, hsmType: string) =
        { config with HsmType = Some hsmType }

    /// <summary>Sets the subnet IDs for HSM instances (use private subnets across multiple AZs).</summary>
    [<CustomOperation("subnetIds")>]
    member _.SubnetIds(config: CloudHSMClusterConfig, subnetIds: string list) =
        { config with SubnetIds = subnetIds }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
    *)
module CloudHSMBuilders =
    let cloudHsmCluster =
        //CloudHSMClusterBuilder
        ()
