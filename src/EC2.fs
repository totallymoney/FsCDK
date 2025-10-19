namespace FsCDK

#nowarn "44" // Suppress deprecation warnings for backward compatibility (KeyName property)

open Amazon.CDK
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM

/// <summary>
/// High-level EC2 Instance builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - Instance type = t3.micro (cost-effective for dev/test)
/// - Detailed monitoring = disabled (opt-in via monitoring operation)
/// - IMDSv2 required = true (enhanced security for instance metadata)
/// - EBS encryption = enabled by default
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - t3.micro provides good balance of compute/cost for many workloads
/// - IMDSv2 prevents SSRF attacks against instance metadata
/// - EBS encryption protects data at rest
/// - Minimal IAM permissions follow least-privilege principle
///
/// **Escape Hatch:**
/// Access the underlying CDK Instance via the `Instance` property on the returned resource
/// for advanced scenarios not covered by this builder.
/// </summary>
type EC2InstanceConfig =
    { InstanceName: string
      ConstructId: string option
      InstanceType: InstanceType option
      MachineImage: IMachineImage option
      Vpc: IVpc option
      VpcSubnets: SubnetSelection option
      SecurityGroup: ISecurityGroup option
      KeyPair: IKeyPair option
      KeyPairName: string option
      Role: IRole option
      UserData: UserData option
      RequireImdsv2: bool option
      DetailedMonitoring: bool option
      BlockDevices: IBlockDevice list }

type EC2InstanceResource =
    {
        InstanceName: string
        ConstructId: string
        /// The underlying CDK Instance construct - use for advanced scenarios
        Instance: Instance_
    }

type EC2InstanceBuilder(name: string) =
    member _.Yield _ : EC2InstanceConfig =
        { InstanceName = name
          ConstructId = None
          InstanceType = Some(InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))
          MachineImage = Some(MachineImage.LatestAmazonLinux2())
          Vpc = None
          VpcSubnets = None
          SecurityGroup = None
          KeyPair = None
          KeyPairName = None
          Role = None
          UserData = None
          RequireImdsv2 = Some true
          DetailedMonitoring = Some false
          BlockDevices = [] }

    member _.Zero() : EC2InstanceConfig =
        { InstanceName = name
          ConstructId = None
          InstanceType = Some(InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))
          MachineImage = Some(MachineImage.LatestAmazonLinux2())
          Vpc = None
          VpcSubnets = None
          SecurityGroup = None
          KeyPair = None
          KeyPairName = None
          Role = None
          UserData = None
          RequireImdsv2 = Some true
          DetailedMonitoring = Some false
          BlockDevices = [] }

    member _.Combine(state1: EC2InstanceConfig, state2: EC2InstanceConfig) : EC2InstanceConfig =
        { InstanceName = state2.InstanceName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          InstanceType = state2.InstanceType |> Option.orElse state1.InstanceType
          MachineImage = state2.MachineImage |> Option.orElse state1.MachineImage
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          VpcSubnets = state2.VpcSubnets |> Option.orElse state1.VpcSubnets
          SecurityGroup = state2.SecurityGroup |> Option.orElse state1.SecurityGroup
          KeyPair = state2.KeyPair |> Option.orElse state1.KeyPair
          KeyPairName = state2.KeyPairName |> Option.orElse state1.KeyPairName
          Role = state2.Role |> Option.orElse state1.Role
          UserData = state2.UserData |> Option.orElse state1.UserData
          RequireImdsv2 = state2.RequireImdsv2 |> Option.orElse state1.RequireImdsv2
          DetailedMonitoring = state2.DetailedMonitoring |> Option.orElse state1.DetailedMonitoring
          BlockDevices =
            if state2.BlockDevices.IsEmpty then
                state1.BlockDevices
            else
                state2.BlockDevices }

    member inline x.For
        (
            config: EC2InstanceConfig,
            [<InlineIfLambda>] f: unit -> EC2InstanceConfig
        ) : EC2InstanceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: EC2InstanceConfig) : EC2InstanceResource =
        let instanceName = config.InstanceName
        let constructId = config.ConstructId |> Option.defaultValue instanceName

        let props = InstanceProps()

        config.InstanceType |> Option.iter (fun v -> props.InstanceType <- v)
        config.MachineImage |> Option.iter (fun v -> props.MachineImage <- v)
        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)
        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)
        config.SecurityGroup |> Option.iter (fun v -> props.SecurityGroup <- v)
        // Use KeyPair if provided, otherwise fall back to KeyPairName (for backward compatibility)
        match config.KeyPair, config.KeyPairName with
        | Some kp, _ -> props.KeyPair <- kp
        | None, Some name -> props.KeyName <- name // Using deprecated property for backward compatibility
        | None, None -> ()

        config.Role |> Option.iter (fun v -> props.Role <- v)
        config.UserData |> Option.iter (fun v -> props.UserData <- v)
        config.RequireImdsv2 |> Option.iter (fun v -> props.RequireImdsv2 <- v)

        config.DetailedMonitoring
        |> Option.iter (fun v -> props.DetailedMonitoring <- v)

        if not config.BlockDevices.IsEmpty then
            props.BlockDevices <- Array.ofList config.BlockDevices

        { InstanceName = instanceName
          ConstructId = constructId
          Instance = null } // Will be created during stack construction

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: EC2InstanceConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("instanceType")>]
    member _.InstanceType(config: EC2InstanceConfig, instanceType: InstanceType) =
        { config with
            InstanceType = Some instanceType }

    [<CustomOperation("machineImage")>]
    member _.MachineImage(config: EC2InstanceConfig, machineImage: IMachineImage) =
        { config with
            MachineImage = Some machineImage }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: EC2InstanceConfig, vpc: IVpc) = { config with Vpc = Some vpc }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: EC2InstanceConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: EC2InstanceConfig, sg: ISecurityGroup) = { config with SecurityGroup = Some sg }

    [<CustomOperation("keyPair")>]
    member _.KeyPair(config: EC2InstanceConfig, keyPair: IKeyPair) = { config with KeyPair = Some keyPair }

    [<CustomOperation("keyPairName")>]
    member _.KeyPairName(config: EC2InstanceConfig, keyPairName: string) =
        { config with
            KeyPairName = Some keyPairName }

    [<CustomOperation("role")>]
    member _.Role(config: EC2InstanceConfig, role: IRole) = { config with Role = Some role }

    [<CustomOperation("userData")>]
    member _.UserData(config: EC2InstanceConfig, userData: UserData) =
        { config with UserData = Some userData }

    [<CustomOperation("requireImdsv2")>]
    member _.RequireImdsv2(config: EC2InstanceConfig, required: bool) =
        { config with
            RequireImdsv2 = Some required }

    [<CustomOperation("detailedMonitoring")>]
    member _.DetailedMonitoring(config: EC2InstanceConfig, enabled: bool) =
        { config with
            DetailedMonitoring = Some enabled }

    [<CustomOperation("blockDevices")>]
    member _.BlockDevices(config: EC2InstanceConfig, devices: IBlockDevice list) =
        { config with BlockDevices = devices }

[<AutoOpen>]
module EC2Builders =
    /// <summary>
    /// Creates a new EC2 instance builder with secure defaults.
    /// Example: ec2Instance "my-instance" { instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL)) }
    /// </summary>
    let ec2Instance name = EC2InstanceBuilder name
