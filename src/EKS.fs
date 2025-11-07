namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EKS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.KMS

// ============================================================================
// Elastic Kubernetes Service (EKS) Cluster Configuration DSL
// ============================================================================

/// <summary>
/// High-level EKS Cluster builder following AWS best practices.
///
/// **Default Security Settings:**
/// - a Kubernetes version = latest stable
/// - Endpoint access = PUBLIC_AND_PRIVATE
/// - Cluster logging = enabled for all log types
/// - Encryption = enabled with AWS managed key
///
/// **Rationale: **
/// These defaults follow AWS Well-Architected Framework:
/// - Latest K8s version for security patches
/// - Public and private access for flexibility
/// - Comprehensive logging for troubleshooting
/// - Encryption at rest for data protection
///
/// **Escape Hatch: **
/// Access the underlying CDK Cluster via the `Cluster` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type EKSClusterConfig =
    { ClusterName: string
      ConstructId: string option
      Version: KubernetesVersion option
      Vpc: IVpc option
      VpcSubnets: ISubnetSelection list
      DefaultCapacity: int option
      DefaultCapacityInstance: InstanceType option
      MastersRole: IRole option
      EndpointAccess: EndpointAccess option
      ClusterLogging: ClusterLoggingTypes seq
      SecretsEncryptionKey: IKey option
      AlbController: AlbControllerOptions option
      CoreDnsComputeType: CoreDnsComputeType option }

type EKSClusterSpec =
    { ClusterName: string
      ConstructId: string
      Props: ClusterProps
      mutable Cluster: Cluster }

type EKSClusterBuilder(name: string) =
    member _.Yield _ : EKSClusterConfig =
        { ClusterName = name
          ConstructId = None
          Version = Some KubernetesVersion.V1_28
          Vpc = None
          VpcSubnets = []
          DefaultCapacity = Some 2
          DefaultCapacityInstance = None
          MastersRole = None
          EndpointAccess = Some EndpointAccess.PUBLIC_AND_PRIVATE
          ClusterLogging =
            [ ClusterLoggingTypes.API
              ClusterLoggingTypes.AUDIT
              ClusterLoggingTypes.AUTHENTICATOR
              ClusterLoggingTypes.CONTROLLER_MANAGER
              ClusterLoggingTypes.SCHEDULER ]
          SecretsEncryptionKey = None
          AlbController = None
          CoreDnsComputeType = None }

    member _.Zero() : EKSClusterConfig =
        { ClusterName = name
          ConstructId = None
          Version = Some KubernetesVersion.V1_28
          Vpc = None
          VpcSubnets = []
          DefaultCapacity = Some 2
          DefaultCapacityInstance = None
          MastersRole = None
          EndpointAccess = Some EndpointAccess.PUBLIC_AND_PRIVATE
          ClusterLogging =
            [ ClusterLoggingTypes.API
              ClusterLoggingTypes.AUDIT
              ClusterLoggingTypes.AUTHENTICATOR
              ClusterLoggingTypes.CONTROLLER_MANAGER
              ClusterLoggingTypes.SCHEDULER ]
          SecretsEncryptionKey = None
          AlbController = None
          CoreDnsComputeType = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> EKSClusterConfig) : EKSClusterConfig = f ()

    member inline x.For(config: EKSClusterConfig, [<InlineIfLambda>] f: unit -> EKSClusterConfig) : EKSClusterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: EKSClusterConfig, b: EKSClusterConfig) : EKSClusterConfig =
        { ClusterName = a.ClusterName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Version =
            match a.Version with
            | Some _ -> a.Version
            | None -> b.Version
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          VpcSubnets = a.VpcSubnets @ b.VpcSubnets
          DefaultCapacity =
            match a.DefaultCapacity with
            | Some _ -> a.DefaultCapacity
            | None -> b.DefaultCapacity
          DefaultCapacityInstance =
            match a.DefaultCapacityInstance with
            | Some _ -> a.DefaultCapacityInstance
            | None -> b.DefaultCapacityInstance
          MastersRole =
            match a.MastersRole with
            | Some _ -> a.MastersRole
            | None -> b.MastersRole
          EndpointAccess =
            match a.EndpointAccess with
            | Some _ -> a.EndpointAccess
            | None -> b.EndpointAccess
          ClusterLogging =
            if not (Seq.isEmpty b.ClusterLogging) then
                b.ClusterLogging
            else
                a.ClusterLogging
          SecretsEncryptionKey =
            match a.SecretsEncryptionKey with
            | Some _ -> a.SecretsEncryptionKey
            | None -> b.SecretsEncryptionKey
          AlbController =
            match a.AlbController with
            | Some _ -> a.AlbController
            | None -> b.AlbController
          CoreDnsComputeType =
            match a.CoreDnsComputeType with
            | Some _ -> a.CoreDnsComputeType
            | None -> b.CoreDnsComputeType }

    member _.Run(config: EKSClusterConfig) : EKSClusterSpec =
        let props = ClusterProps()
        let constructId = config.ConstructId |> Option.defaultValue config.ClusterName

        props.ClusterName <- config.ClusterName

        // VPC is required
        props.Vpc <-
            match config.Vpc with
            | Some vpcRef -> vpcRef
            | None -> invalidArg "vpc" "VPC is required for EKS Cluster"

        // AWS Best Practice: Use latest stable Kubernetes version
        props.Version <- config.Version |> Option.defaultValue KubernetesVersion.V1_28

        // AWS Best Practice: Enable both public and private endpoint access
        props.EndpointAccess <- config.EndpointAccess |> Option.defaultValue EndpointAccess.PUBLIC_AND_PRIVATE

        // AWS Best Practice: Default to 2 nodes for HA
        props.DefaultCapacity <- config.DefaultCapacity |> Option.defaultValue 2 |> double

        config.DefaultCapacityInstance
        |> Option.iter (fun i -> props.DefaultCapacityInstance <- i)

        config.MastersRole |> Option.iter (fun r -> props.MastersRole <- r)

        if not (List.isEmpty config.VpcSubnets) then
            props.VpcSubnets <- config.VpcSubnets |> List.toArray

        if not (Seq.isEmpty config.ClusterLogging) then
            props.ClusterLogging <- config.ClusterLogging |> Seq.toArray

        config.SecretsEncryptionKey
        |> Option.iter (fun v -> props.SecretsEncryptionKey <- v)

        config.AlbController |> Option.iter (fun alb -> props.AlbController <- alb)

        config.CoreDnsComputeType
        |> Option.iter (fun c -> props.CoreDnsComputeType <- c)

        { ClusterName = config.ClusterName
          ConstructId = constructId
          Props = props
          Cluster = null }

    /// <summary>Sets the construct ID for the EKS cluster.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: EKSClusterConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the Kubernetes version.</summary>
    [<CustomOperation("version")>]
    member _.Version(config: EKSClusterConfig, version: KubernetesVersion) = { config with Version = Some version }

    /// <summary>Sets the VPC for the cluster.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: EKSClusterConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Adds VPC subnets for the cluster.</summary>
    [<CustomOperation("vpcSubnet")>]
    member _.VpcSubnet(config: EKSClusterConfig, subnet: ISubnetSelection) =
        { config with
            VpcSubnets = subnet :: config.VpcSubnets }

    /// <summary>Sets the default node capacity.</summary>
    [<CustomOperation("defaultCapacity")>]
    member _.DefaultCapacity(config: EKSClusterConfig, capacity: int) =
        { config with
            DefaultCapacity = Some capacity }

    /// <summary>Sets the default capacity instance type.</summary>
    [<CustomOperation("defaultCapacityInstance")>]
    member _.DefaultCapacityInstance(config: EKSClusterConfig, instanceType: InstanceType) =
        { config with
            DefaultCapacityInstance = Some instanceType }

    /// <summary>Sets the masters role.</summary>
    [<CustomOperation("mastersRole")>]
    member _.MastersRole(config: EKSClusterConfig, role: IRole) = { config with MastersRole = Some role }

    /// <summary>Sets the endpoint access mode.</summary>
    [<CustomOperation("endpointAccess")>]
    member _.EndpointAccess(config: EKSClusterConfig, access: EndpointAccess) =
        { config with
            EndpointAccess = Some access }

    /// <summary>Set cluster logging. Default: API/AUDIT/AUTHENTICATOR/CONTROLLER_MANAGER/SCHEDULER</summary>
    [<CustomOperation("setClusterLogging")>]
    member _.SetClusterLogging(config: EKSClusterConfig, loggingTypes: ClusterLoggingTypes seq) =
        { config with
            ClusterLogging = loggingTypes }

    /// <summary>Sets the ALB controller options.</summary>
    [<CustomOperation("albController")>]
    member _.AlbController(config: EKSClusterConfig, options: AlbControllerOptions) =
        { config with
            AlbController = Some options }

    /// <summary>Sets the CoreDNS compute type.</summary>
    [<CustomOperation("coreDnsComputeType")>]
    member _.CoreDnsComputeType(config: EKSClusterConfig, computeType: CoreDnsComputeType) =
        { config with
            CoreDnsComputeType = Some computeType }

    /// <summary>Sets the secrets encryption key.</summary>
    [<CustomOperation("encryptionKey")>]
    member _.SecretsEncryptionKey(config: EKSClusterConfig, key: IKey) =
        { config with
            SecretsEncryptionKey = Some(key) }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module EKSBuilders =
    /// <summary>Creates an EKS cluster with AWS best practices.</summary>
    /// <param name="name">The cluster name.</param>
    /// <code lang="fsharp">
    /// eksCluster "MyCluster" {
    ///     vpc myVpc
    ///     version KubernetesVersion.V1_28
    ///     defaultCapacity 3
    /// }
    /// </code>
    let eksCluster name = EKSClusterBuilder(name)
