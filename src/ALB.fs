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
      SecurityGroup: SecurityGroupRef option
      DeletionProtection: bool option
      Http2Enabled: bool option
      DropInvalidHeaderFields: bool option }

type ALBSpec =
    { LoadBalancerName: string
      ConstructId: string
      mutable LoadBalancer: ApplicationLoadBalancer
      Props: ApplicationLoadBalancerProps }

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

    member _.Run(config: ALBConfig) : ALBSpec =
        let loadBalancerName = config.LoadBalancerName
        let constructId = config.ConstructId |> Option.defaultValue loadBalancerName

        let props = ApplicationLoadBalancerProps()
        props.LoadBalancerName <- loadBalancerName

        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)

        config.InternetFacing |> Option.iter (fun v -> props.InternetFacing <- v)
        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)

        config.SecurityGroup
        |> Option.iter (fun v -> props.SecurityGroup <- VpcHelpers.resolveSecurityGroupRef v)

        config.DeletionProtection
        |> Option.iter (fun v -> props.DeletionProtection <- v)

        config.Http2Enabled |> Option.iter (fun v -> props.Http2Enabled <- v)

        config.DropInvalidHeaderFields
        |> Option.iter (fun v -> props.DropInvalidHeaderFields <- v)

        { LoadBalancerName = loadBalancerName
          ConstructId = constructId
          LoadBalancer = null
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ALBConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: ALBConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    [<CustomOperation("internetFacing")>]
    member _.InternetFacing(config: ALBConfig, internetFacing: bool) =
        { config with
            InternetFacing = Some internetFacing }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: ALBConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: ALBConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroup = Some(SecurityGroupRef.SecurityGroupInterface sg) }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: ALBConfig, sg: SecurityGroupSpec) =
        { config with
            SecurityGroup = Some(SecurityGroupRef.SecurityGroupSpecRef sg) }

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

/// <summary>
/// High-level ALB Target Group builder following AWS best practices.
///
/// **Default Settings:**
/// - Protocol = HTTP
/// - Port = 80
/// - Target type = IP (for Fargate)
/// - Deregistration delay = 30 seconds
/// - Health check enabled with sensible defaults
///
/// **Rationale:**
/// - IP target type required for Fargate tasks
/// - Shorter deregistration delay improves deployment speed
/// - Health checks ensure traffic only goes to healthy targets
/// </summary>
type ALBTargetGroupConfig =
    { TargetGroupName: string
      ConstructId: string option
      Vpc: IVpc option
      Port: int voption
      Protocol: ApplicationProtocol voption
      TargetType: TargetType voption
      DeregistrationDelay: Duration option
      HealthCheckPath: string option
      HealthCheckInterval: Duration option
      HealthCheckTimeout: Duration option
      HealthyThresholdCount: int voption
      UnhealthyThresholdCount: int voption }

type ALBTargetGroupResource =
    {
        TargetGroupName: string
        ConstructId: string
        /// The underlying CDK ApplicationTargetGroup construct
        TargetGroup: ApplicationTargetGroup
    }

type ALBTargetGroupBuilder(name: string) =
    member _.Yield _ : ALBTargetGroupConfig =
        { TargetGroupName = name
          ConstructId = None
          Vpc = None
          Port = ValueSome 80
          Protocol = ValueSome ApplicationProtocol.HTTP
          TargetType = ValueSome TargetType.IP
          DeregistrationDelay = Some(Duration.Seconds 30.0)
          HealthCheckPath = Some "/health"
          HealthCheckInterval = Some(Duration.Seconds 30.0)
          HealthCheckTimeout = Some(Duration.Seconds 5.0)
          HealthyThresholdCount = ValueSome 2
          UnhealthyThresholdCount = ValueSome 3 }

    member _.Zero() : ALBTargetGroupConfig =
        { TargetGroupName = name
          ConstructId = None
          Vpc = None
          Port = ValueSome 80
          Protocol = ValueSome ApplicationProtocol.HTTP
          TargetType = ValueSome TargetType.IP
          DeregistrationDelay = Some(Duration.Seconds(30.0))
          HealthCheckPath = Some "/health"
          HealthCheckInterval = Some(Duration.Seconds(30.0))
          HealthCheckTimeout = Some(Duration.Seconds(5.0))
          HealthyThresholdCount = ValueSome 2
          UnhealthyThresholdCount = ValueSome 3 }

    member _.Combine(state1: ALBTargetGroupConfig, state2: ALBTargetGroupConfig) : ALBTargetGroupConfig =
        { TargetGroupName = state2.TargetGroupName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          Port = state2.Port |> ValueOption.orElse state1.Port
          Protocol = state2.Protocol |> ValueOption.orElse state1.Protocol
          TargetType = state2.TargetType |> ValueOption.orElse state1.TargetType
          DeregistrationDelay = state2.DeregistrationDelay |> Option.orElse state1.DeregistrationDelay
          HealthCheckPath = state2.HealthCheckPath |> Option.orElse state1.HealthCheckPath
          HealthCheckInterval = state2.HealthCheckInterval |> Option.orElse state1.HealthCheckInterval
          HealthCheckTimeout = state2.HealthCheckTimeout |> Option.orElse state1.HealthCheckTimeout
          HealthyThresholdCount = state2.HealthyThresholdCount |> ValueOption.orElse state1.HealthyThresholdCount
          UnhealthyThresholdCount =
            state2.UnhealthyThresholdCount
            |> ValueOption.orElse state1.UnhealthyThresholdCount }

    member inline x.For
        (
            config: ALBTargetGroupConfig,
            [<InlineIfLambda>] f: unit -> ALBTargetGroupConfig
        ) : ALBTargetGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ALBTargetGroupConfig) : ALBTargetGroupResource =
        let targetGroupName = config.TargetGroupName
        let constructId = config.ConstructId |> Option.defaultValue targetGroupName

        let props = ApplicationTargetGroupProps()
        props.TargetGroupName <- targetGroupName

        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)

        config.Port
        |> ValueOption.iter (fun v -> props.Port <- System.Nullable<float>(float v))

        config.Protocol |> ValueOption.iter (fun v -> props.Protocol <- v)
        config.TargetType |> ValueOption.iter (fun v -> props.TargetType <- v)

        config.DeregistrationDelay
        |> Option.iter (fun v -> props.DeregistrationDelay <- v)

        // Configure health check
        let hc = HealthCheck()
        config.HealthCheckPath |> Option.iter (fun v -> hc.Path <- v)
        config.HealthCheckInterval |> Option.iter (fun v -> hc.Interval <- v)
        config.HealthCheckTimeout |> Option.iter (fun v -> hc.Timeout <- v)

        config.HealthyThresholdCount
        |> ValueOption.iter (fun v -> hc.HealthyThresholdCount <- System.Nullable<float>(float v))

        config.UnhealthyThresholdCount
        |> ValueOption.iter (fun v -> hc.UnhealthyThresholdCount <- System.Nullable<float>(float v))

        props.HealthCheck <- hc

        { TargetGroupName = targetGroupName
          ConstructId = constructId
          TargetGroup = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ALBTargetGroupConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: ALBTargetGroupConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    [<CustomOperation("port")>]
    member _.Port(config: ALBTargetGroupConfig, port: int) = { config with Port = ValueSome port }

    [<CustomOperation("protocol")>]
    member _.Protocol(config: ALBTargetGroupConfig, protocol: ApplicationProtocol) =
        { config with
            Protocol = ValueSome protocol }

    [<CustomOperation("targetType")>]
    member _.TargetType(config: ALBTargetGroupConfig, targetType: TargetType) =
        { config with
            TargetType = ValueSome targetType }

    [<CustomOperation("deregistrationDelay")>]
    member _.DeregistrationDelay(config: ALBTargetGroupConfig, delay: Duration) =
        { config with
            DeregistrationDelay = Some delay }

    [<CustomOperation("healthCheckPath")>]
    member _.HealthCheckPath(config: ALBTargetGroupConfig, path: string) =
        { config with
            HealthCheckPath = Some path }

    [<CustomOperation("healthCheckInterval")>]
    member _.HealthCheckInterval(config: ALBTargetGroupConfig, interval: Duration) =
        { config with
            HealthCheckInterval = Some interval }

    [<CustomOperation("healthCheckTimeout")>]
    member _.HealthCheckTimeout(config: ALBTargetGroupConfig, timeout: Duration) =
        { config with
            HealthCheckTimeout = Some timeout }

    [<CustomOperation("healthyThresholdCount")>]
    member _.HealthyThresholdCount(config: ALBTargetGroupConfig, count: int) =
        { config with
            HealthyThresholdCount = ValueSome count }

    [<CustomOperation("unhealthyThresholdCount")>]
    member _.UnhealthyThresholdCount(config: ALBTargetGroupConfig, count: int) =
        { config with
            UnhealthyThresholdCount = ValueSome count }

/// <summary>
/// High-level ALB Listener builder following AWS best practices.
///
/// **Default Settings:**
/// - Protocol = HTTP
/// - Port = 80
/// - Default action = fixed response (503)
///
/// **Rationale:**
/// - Fixed response by default prevents unhandled requests
/// - Explicit target group configuration required
/// </summary>
type ALBListenerConfig =
    { ConstructId: string option
      LoadBalancer: IApplicationLoadBalancer option
      Port: int voption
      Protocol: ApplicationProtocol voption
      DefaultTargetGroups: IApplicationTargetGroup list
      Certificates: IListenerCertificate list
      SslPolicy: SslPolicy voption }

type ALBListenerResource =
    {
        ConstructId: string
        /// The underlying CDK ApplicationListener construct
        Listener: ApplicationListener
    }

type ALBListenerBuilder(loadBalancer: IApplicationLoadBalancer) =
    member _.Yield _ : ALBListenerConfig =
        { ConstructId = None
          LoadBalancer = Some loadBalancer
          Port = ValueSome 80
          Protocol = ValueSome ApplicationProtocol.HTTP
          DefaultTargetGroups = []
          Certificates = []
          SslPolicy = ValueNone }

    member _.Zero() : ALBListenerConfig =
        { ConstructId = None
          LoadBalancer = Some loadBalancer
          Port = ValueSome 80
          Protocol = ValueSome ApplicationProtocol.HTTP
          DefaultTargetGroups = []
          Certificates = []
          SslPolicy = ValueNone }

    member _.Combine(state1: ALBListenerConfig, state2: ALBListenerConfig) : ALBListenerConfig =
        { ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          LoadBalancer = state2.LoadBalancer |> Option.orElse state1.LoadBalancer
          Port = state2.Port |> ValueOption.orElse state1.Port
          Protocol = state2.Protocol |> ValueOption.orElse state1.Protocol
          DefaultTargetGroups =
            if state2.DefaultTargetGroups.IsEmpty then
                state1.DefaultTargetGroups
            else
                state2.DefaultTargetGroups @ state1.DefaultTargetGroups
          Certificates =
            if state2.Certificates.IsEmpty then
                state1.Certificates
            else
                state2.Certificates @ state1.Certificates
          SslPolicy = state2.SslPolicy |> ValueOption.orElse state1.SslPolicy }

    member inline x.For
        (
            config: ALBListenerConfig,
            [<InlineIfLambda>] f: unit -> ALBListenerConfig
        ) : ALBListenerConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ALBListenerConfig) : ALBListenerResource =
        let constructId = config.ConstructId |> Option.defaultValue "Listener"

        let props = ApplicationListenerProps()

        match config.LoadBalancer with
        | Some lb -> props.LoadBalancer <- lb
        | None -> failwith "LoadBalancer is required for ALB Listener"

        config.Port
        |> ValueOption.iter (fun v -> props.Port <- System.Nullable<float>(float v))

        config.Protocol |> ValueOption.iter (fun v -> props.Protocol <- v)

        if not config.DefaultTargetGroups.IsEmpty then
            props.DefaultTargetGroups <- config.DefaultTargetGroups |> Array.ofList

        if not config.Certificates.IsEmpty then
            props.Certificates <- config.Certificates |> Array.ofList

        config.SslPolicy |> ValueOption.iter (fun v -> props.SslPolicy <- v)

        { ConstructId = constructId
          Listener = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ALBListenerConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("port")>]
    member _.Port(config: ALBListenerConfig, port: int) = { config with Port = ValueSome port }

    [<CustomOperation("protocol")>]
    member _.Protocol(config: ALBListenerConfig, protocol: ApplicationProtocol) =
        { config with
            Protocol = ValueSome protocol }

    [<CustomOperation("defaultTargetGroup")>]
    member _.DefaultTargetGroup(config: ALBListenerConfig, targetGroup: IApplicationTargetGroup) =
        { config with
            DefaultTargetGroups = targetGroup :: config.DefaultTargetGroups }

    [<CustomOperation("certificate")>]
    member _.Certificate(config: ALBListenerConfig, certificate: IListenerCertificate) =
        { config with
            Certificates = certificate :: config.Certificates }

    [<CustomOperation("sslPolicy")>]
    member _.SslPolicy(config: ALBListenerConfig, policy: SslPolicy) =
        { config with
            SslPolicy = ValueSome policy }

[<AutoOpen>]
module ALBBuilders =
    /// <summary>
    /// Creates a new Application Load Balancer builder with secure defaults.
    /// Example: applicationLoadBalancer "my-alb" { vpc myVpc; internetFacing true }
    /// </summary>
    let applicationLoadBalancer name = ALBBuilder(name)

    /// <summary>
    /// Creates a new ALB Target Group builder with secure defaults.
    /// Example: albTargetGroup "my-tg" { vpc myVpc; port 8080; healthCheckPath "/health" }
    /// </summary>
    let albTargetGroup name = ALBTargetGroupBuilder name

    /// <summary>
    /// Creates a new ALB Listener builder.
    /// Example: albListener myAlb { port 80; defaultTargetGroup myTg }
    /// </summary>
    let albListener loadBalancer = ALBListenerBuilder loadBalancer
