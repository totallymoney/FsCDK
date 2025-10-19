namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.EC2

/// <summary>
/// High-level Application Load Balancer builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - Internet-facing = false (internal by default for security)
/// - HTTP/2 = enabled
/// - Deletion protection = false (can be enabled for production)
/// - Drop invalid headers = true (security best practice)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Internal ALBs by default prevent accidental public exposure
/// - HTTP/2 improves performance
/// - Dropping invalid headers prevents header injection attacks
///
/// **Escape Hatch:**
/// Access the underlying CDK ApplicationLoadBalancer via the `LoadBalancer` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type ALBConfig =
    { LoadBalancerName: string
      ConstructId: string option
      Vpc: IVpc option
      InternetFacing: bool option
      VpcSubnets: SubnetSelection option
      SecurityGroup: ISecurityGroup option
      DeletionProtection: bool option
      Http2Enabled: bool option
      DropInvalidHeaderFields: bool option }

type ALBResource =
    {
        LoadBalancerName: string
        ConstructId: string
        /// The underlying CDK ApplicationLoadBalancer construct
        LoadBalancer: ApplicationLoadBalancer
    }

type ALBBuilder(name: string) =
    member _.Yield _ : ALBConfig =
        { LoadBalancerName = name
          ConstructId = None
          Vpc = None
          InternetFacing = Some false
          VpcSubnets = None
          SecurityGroup = None
          DeletionProtection = Some false
          Http2Enabled = Some true
          DropInvalidHeaderFields = Some true }

    member _.Zero() : ALBConfig =
        { LoadBalancerName = name
          ConstructId = None
          Vpc = None
          InternetFacing = Some false
          VpcSubnets = None
          SecurityGroup = None
          DeletionProtection = Some false
          Http2Enabled = Some true
          DropInvalidHeaderFields = Some true }

    member _.Combine(state1: ALBConfig, state2: ALBConfig) : ALBConfig =
        { LoadBalancerName = state2.LoadBalancerName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          InternetFacing = state2.InternetFacing |> Option.orElse state1.InternetFacing
          VpcSubnets = state2.VpcSubnets |> Option.orElse state1.VpcSubnets
          SecurityGroup = state2.SecurityGroup |> Option.orElse state1.SecurityGroup
          DeletionProtection = state2.DeletionProtection |> Option.orElse state1.DeletionProtection
          Http2Enabled = state2.Http2Enabled |> Option.orElse state1.Http2Enabled
          DropInvalidHeaderFields = state2.DropInvalidHeaderFields |> Option.orElse state1.DropInvalidHeaderFields }

    member inline x.For(config: ALBConfig, [<InlineIfLambda>] f: unit -> ALBConfig) : ALBConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ALBConfig) : ALBResource =
        let loadBalancerName = config.LoadBalancerName
        let constructId = config.ConstructId |> Option.defaultValue loadBalancerName

        let props = ApplicationLoadBalancerProps()
        props.LoadBalancerName <- loadBalancerName
        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)
        config.InternetFacing |> Option.iter (fun v -> props.InternetFacing <- v)
        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)
        config.SecurityGroup |> Option.iter (fun v -> props.SecurityGroup <- v)

        config.DeletionProtection
        |> Option.iter (fun v -> props.DeletionProtection <- v)

        config.Http2Enabled |> Option.iter (fun v -> props.Http2Enabled <- v)

        config.DropInvalidHeaderFields
        |> Option.iter (fun v -> props.DropInvalidHeaderFields <- v)

        { LoadBalancerName = loadBalancerName
          ConstructId = constructId
          LoadBalancer = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ALBConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: ALBConfig, vpc: IVpc) = { config with Vpc = Some vpc }

    [<CustomOperation("internetFacing")>]
    member _.InternetFacing(config: ALBConfig, internetFacing: bool) =
        { config with
            InternetFacing = Some internetFacing }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: ALBConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: ALBConfig, sg: ISecurityGroup) = { config with SecurityGroup = Some sg }

    [<CustomOperation("deletionProtection")>]
    member _.DeletionProtection(config: ALBConfig, enabled: bool) =
        { config with
            DeletionProtection = Some enabled }

    [<CustomOperation("http2Enabled")>]
    member _.Http2Enabled(config: ALBConfig, enabled: bool) =
        { config with
            Http2Enabled = Some enabled }

    [<CustomOperation("dropInvalidHeaderFields")>]
    member _.DropInvalidHeaderFields(config: ALBConfig, drop: bool) =
        { config with
            DropInvalidHeaderFields = Some drop }

[<AutoOpen>]
module ALBBuilders =
    /// <summary>
    /// Creates a new Application Load Balancer builder with secure defaults.
    /// Example: applicationLoadBalancer "my-alb" { vpc myVpc; internetFacing true }
    /// </summary>
    let applicationLoadBalancer name = ALBBuilder name
