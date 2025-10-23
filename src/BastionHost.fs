namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EC2

// ============================================================================
// Bastion Host Configuration DSL
// ============================================================================

/// <summary>
/// High-level Bastion Host builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - Instance type = t3.nano (minimal compute for SSH access)
/// - Machine image = Amazon Linux 2023
/// - Requires IMDSv2 = true (enhanced security)
/// - Subnet type = PUBLIC (for external SSH access)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - t3.nano is cost-effective for bastion workloads
/// - Amazon Linux 2023 has latest security patches
/// - IMDSv2 prevents SSRF attacks against instance metadata
/// - Public subnet placement allows external access
///
/// **Security Note:**
/// Bastion hosts should use strict security groups and key-based authentication.
/// Consider AWS Systems Manager Session Manager as a more secure alternative.
///
/// **Escape Hatch:**
/// Access the underlying CDK BastionHostLinux via the `BastionHost` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type BastionHostConfig =
    { BastionName: string
      ConstructId: string option
      Vpc: VpcRef option
      InstanceType: InstanceType option
      MachineImage: IMachineImage option
      SubnetSelection: SubnetSelection option
      SecurityGroup: SecurityGroupRef option
      InstanceName: string option
      RequireImdsv2: bool option }

type BastionHostSpec =
    { BastionName: string
      ConstructId: string
      Props: BastionHostLinuxProps
      mutable BastionHost: BastionHostLinux option }

    /// Gets the underlying BastionHostLinux resource. Must be called after the stack is built.
    member this.Resource =
        match this.BastionHost with
        | Some bastion -> bastion
        | None ->
            failwith
                $"BastionHost '{this.BastionName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type BastionHostBuilder(name: string) =
    member _.Yield _ : BastionHostConfig =
        { BastionName = name
          ConstructId = None
          Vpc = None
          InstanceType = Some(InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO))
          MachineImage = Some(MachineImage.LatestAmazonLinux2023())
          SubnetSelection = Some(SubnetSelection(SubnetType = SubnetType.PUBLIC))
          SecurityGroup = None
          InstanceName = None
          RequireImdsv2 = Some true }

    member _.Zero() : BastionHostConfig =
        { BastionName = name
          ConstructId = None
          Vpc = None
          InstanceType = Some(InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO))
          MachineImage = Some(MachineImage.LatestAmazonLinux2023())
          SubnetSelection = Some(SubnetSelection(SubnetType = SubnetType.PUBLIC))
          SecurityGroup = None
          InstanceName = None
          RequireImdsv2 = Some true }

    member inline _.Delay([<InlineIfLambda>] f: unit -> BastionHostConfig) : BastionHostConfig = f ()

    member inline x.For
        (
            config: BastionHostConfig,
            [<InlineIfLambda>] f: unit -> BastionHostConfig
        ) : BastionHostConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: BastionHostConfig, b: BastionHostConfig) : BastionHostConfig =
        { BastionName = a.BastionName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          InstanceType =
            match a.InstanceType with
            | Some _ -> a.InstanceType
            | None -> b.InstanceType
          MachineImage =
            match a.MachineImage with
            | Some _ -> a.MachineImage
            | None -> b.MachineImage
          SubnetSelection =
            match a.SubnetSelection with
            | Some _ -> a.SubnetSelection
            | None -> b.SubnetSelection
          SecurityGroup =
            match a.SecurityGroup with
            | Some _ -> a.SecurityGroup
            | None -> b.SecurityGroup
          InstanceName =
            match a.InstanceName with
            | Some _ -> a.InstanceName
            | None -> b.InstanceName
          RequireImdsv2 =
            match a.RequireImdsv2 with
            | Some _ -> a.RequireImdsv2
            | None -> b.RequireImdsv2 }

    member _.Run(config: BastionHostConfig) : BastionHostSpec =
        let props = BastionHostLinuxProps()
        let constructId = config.ConstructId |> Option.defaultValue config.BastionName

        // VPC is required
        match config.Vpc with
        | Some vpcRef -> props.Vpc <- VpcHelpers.resolveVpcRef vpcRef
        | None -> printfn "Warning: VPC is required for Network Load Balancer"

        // AWS Best Practice: Use minimal instance type for cost optimization
        props.InstanceType <-
            config.InstanceType
            |> Option.defaultValue (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO))

        // AWS Best Practice: Latest Amazon Linux for security patches
        props.MachineImage <-
            config.MachineImage
            |> Option.defaultValue (MachineImage.LatestAmazonLinux2023())

        // AWS Best Practice: Require IMDSv2 for enhanced security
        props.RequireImdsv2 <- config.RequireImdsv2 |> Option.defaultValue true

        config.SubnetSelection |> Option.iter (fun s -> props.SubnetSelection <- s)

        config.SecurityGroup
        |> Option.iter (fun sg -> props.SecurityGroup <- VpcHelpers.resolveSecurityGroupRef sg)

        config.InstanceName |> Option.iter (fun n -> props.InstanceName <- n)

        { BastionName = config.BastionName
          ConstructId = constructId
          Props = props
          BastionHost = None }

    /// <summary>Sets the construct ID for the Bastion Host.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: BastionHostConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the Bastion Host.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: BastionHostConfig, vpc: IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    /// <summary>Sets the VPC for the Bastion Host from a VpcSpec.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: BastionHostConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

    /// <summary>Sets the instance type.</summary>
    [<CustomOperation("instanceType")>]
    member _.InstanceType(config: BastionHostConfig, instanceType: InstanceType) =
        { config with
            InstanceType = Some instanceType }

    /// <summary>Sets the machine image.</summary>
    [<CustomOperation("machineImage")>]
    member _.MachineImage(config: BastionHostConfig, image: IMachineImage) =
        { config with
            MachineImage = Some image }

    /// <summary>Sets the subnet selection.</summary>
    [<CustomOperation("subnetSelection")>]
    member _.SubnetSelection(config: BastionHostConfig, selection: SubnetSelection) =
        { config with
            SubnetSelection = Some selection }

    /// <summary>Sets the security group.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: BastionHostConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroup = Some(SecurityGroupInterface sg) }

    /// <summary>Sets the security group from a SecurityGroupSpec.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: BastionHostConfig, sgSpec: SecurityGroupSpec) =
        { config with
            SecurityGroup = Some(SecurityGroupSpecRef sgSpec) }

    /// <summary>Sets the instance name.</summary>
    [<CustomOperation("instanceName")>]
    member _.InstanceName(config: BastionHostConfig, name: string) =
        { config with InstanceName = Some name }

    /// <summary>Sets whether to require IMDSv2.</summary>
    [<CustomOperation("requireImdsv2")>]
    member _.RequireImdsv2(config: BastionHostConfig, require: bool) =
        { config with
            RequireImdsv2 = Some require }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module BastionHostBuilders =
    /// <summary>Creates a Bastion Host with AWS security best practices.</summary>
    /// <param name="name">The bastion host name.</param>
    /// <code lang="fsharp">
    /// bastionHost "MyBastion" {
    ///     vpc myVpc
    ///     instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO))
    ///     instanceName "bastion-host"
    /// }
    /// </code>
    let bastionHost (name: string) = BastionHostBuilder name
