namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EC2

/// <summary>
/// High-level Elastic IP builder following AWS best practices.
///
/// **Default Settings:**
/// - Domain = VPC (required for VPC instances)
/// - No instance association by default (explicit attachment)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - VPC is the standard domain for modern AWS architectures
/// - Explicit association prevents accidental attachments
/// - Static IPs are expensive, use only when necessary
///
/// **Use Cases:**
/// - NAT Gateways (automatic EIP allocation)
/// - Static IPs for public-facing services
/// - Whitelisting with third-party services
///
/// **Escape Hatch:**
/// Access the underlying CDK CfnEIP via the `ElasticIP` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type ElasticIPConfig =
    { EipName: string
      ConstructId: string option
      Domain: string option
      InstanceId: string option
      NetworkInterfaceId: string option
      PublicIpv4Pool: string option
      Tags: (string * string) list }

type ElasticIPResource =
    {
        EipName: string
        ConstructId: string
        /// The underlying CDK CfnEIP construct
        ElasticIP: CfnEIP
    }

    /// Gets the allocated Elastic IP address
    member this.IpAddress = this.ElasticIP.AttrPublicIp

    /// Gets the allocation ID
    member this.AllocationId = this.ElasticIP.AttrAllocationId

type ElasticIPBuilder(name: string) =
    member _.Yield _ : ElasticIPConfig =
        { EipName = name
          ConstructId = None
          Domain = Some "vpc"
          InstanceId = None
          NetworkInterfaceId = None
          PublicIpv4Pool = None
          Tags = [] }

    member _.Zero() : ElasticIPConfig =
        { EipName = name
          ConstructId = None
          Domain = Some "vpc"
          InstanceId = None
          NetworkInterfaceId = None
          PublicIpv4Pool = None
          Tags = [] }

    member _.Combine(state1: ElasticIPConfig, state2: ElasticIPConfig) : ElasticIPConfig =
        { EipName = state2.EipName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Domain = state2.Domain |> Option.orElse state1.Domain
          InstanceId = state2.InstanceId |> Option.orElse state1.InstanceId
          NetworkInterfaceId = state2.NetworkInterfaceId |> Option.orElse state1.NetworkInterfaceId
          PublicIpv4Pool = state2.PublicIpv4Pool |> Option.orElse state1.PublicIpv4Pool
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline x.For(config: ElasticIPConfig, [<InlineIfLambda>] f: unit -> ElasticIPConfig) : ElasticIPConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ElasticIPConfig) : ElasticIPResource =
        let eipName = config.EipName
        let constructId = config.ConstructId |> Option.defaultValue eipName

        let props = CfnEIPProps()
        config.Domain |> Option.iter (fun v -> props.Domain <- v)
        config.InstanceId |> Option.iter (fun v -> props.InstanceId <- v)

        config.NetworkInterfaceId
        |> Option.iter (fun v -> props.NetworkBorderGroup <- v)

        config.PublicIpv4Pool |> Option.iter (fun v -> props.PublicIpv4Pool <- v)

        if not config.Tags.IsEmpty then
            props.Tags <-
                config.Tags
                |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                |> Array.ofList

        { EipName = eipName
          ConstructId = constructId
          ElasticIP = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ElasticIPConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("domain")>]
    member _.Domain(config: ElasticIPConfig, domain: string) = { config with Domain = Some domain }

    [<CustomOperation("instanceId")>]
    member _.InstanceId(config: ElasticIPConfig, instanceId: string) =
        { config with
            InstanceId = Some instanceId }

    [<CustomOperation("networkInterfaceId")>]
    member _.NetworkInterfaceId(config: ElasticIPConfig, networkInterfaceId: string) =
        { config with
            NetworkInterfaceId = Some networkInterfaceId }

    [<CustomOperation("publicIpv4Pool")>]
    member _.PublicIpv4Pool(config: ElasticIPConfig, pool: string) =
        { config with
            PublicIpv4Pool = Some pool }

    [<CustomOperation("tag")>]
    member _.Tag(config: ElasticIPConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: ElasticIPConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// Helper functions for Elastic IP operations
module ElasticIPHelpers =

    /// Creates an EIP for use with EC2-Classic (legacy)
    let classic () = "standard"

    /// Creates an EIP for use with VPC (modern default)
    let vpc () = "vpc"

[<AutoOpen>]
module ElasticIPBuilders =
    /// <summary>
    /// Creates a new Elastic IP builder with VPC defaults.
    /// Example: elasticIp "my-static-ip" { tag "Environment" "Production" }
    /// </summary>
    let elasticIp name = ElasticIPBuilder name
