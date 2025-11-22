namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EFS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.KMS
open Amazon.CDK.AWS.Lambda
open Constructs

// ============================================================================
// Lambda FileSystem Builder DSL
// ============================================================================

type LambdaFileSystemConfig =
    { Arn: string option
      LocalMountPath: string option
      Connections: Connections_ option
      Dependency: IDependable array option
      Policies: PolicyStatement array option }

type LambdaFileSystemBuilder() =
    member _.Yield(_: unit) : LambdaFileSystemConfig =
        { Arn = None
          LocalMountPath = None
          Connections = None
          Dependency = None
          Policies = None }

    member _.Zero() : LambdaFileSystemConfig =
        { Arn = None
          LocalMountPath = None
          Connections = None
          Dependency = None
          Policies = None }

    member _.Combine(a: LambdaFileSystemConfig, b: LambdaFileSystemConfig) : LambdaFileSystemConfig =
        { Arn = Option.orElse a.Arn b.Arn
          LocalMountPath =
            if a.LocalMountPath.IsSome then
                a.LocalMountPath
            else
                b.LocalMountPath
          Connections =
            if a.Connections.IsSome then
                a.Connections
            else
                b.Connections
          Dependency = if a.Dependency.IsSome then a.Dependency else b.Dependency
          Policies = if a.Policies.IsSome then a.Policies else b.Policies }

    member inline _.Delay(f: unit -> LambdaFileSystemConfig) = f ()
    member inline x.For(state: LambdaFileSystemConfig, f: unit -> LambdaFileSystemConfig) = x.Combine(state, f ())

    member _.Run(config: LambdaFileSystemConfig) : FileSystem =

        let fsConfig = FileSystemConfig()

        config.Arn |> Option.iter (fun arn -> fsConfig.Arn <- arn)

        config.LocalMountPath
        |> Option.iter (fun path -> fsConfig.LocalMountPath <- path)

        config.Connections |> Option.iter (fun conn -> fsConfig.Connections <- conn)

        config.Dependency |> Option.iter (fun dep -> fsConfig.Dependency <- dep)

        config.Policies |> Option.iter (fun pol -> fsConfig.Policies <- pol)

        Amazon.CDK.AWS.Lambda.FileSystem(fsConfig)

    [<CustomOperation("arn")>]
    member _.Arn(config: LambdaFileSystemConfig, arn: string) = { config with Arn = Some arn }

    [<CustomOperation("localMountPath")>]
    member _.LocalMountPath(config: LambdaFileSystemConfig, path: string) =
        { config with
            LocalMountPath = Some path }

    [<CustomOperation("connections")>]
    member _.Connections(config: LambdaFileSystemConfig, conn: Connections_) = { config with Connections = Some conn }

    [<CustomOperation("dependency")>]
    member _.Dependency(config: LambdaFileSystemConfig, dep: IDependable array) = { config with Dependency = Some dep }

    [<CustomOperation("policies")>]
    member _.Policies(config: LambdaFileSystemConfig, pol: PolicyStatement array) = { config with Policies = Some pol }


// ============================================================================
// EFS FileSystem Builder DSL
// ============================================================================

type EfsFileSystemConfig =
    { Name: string
      ConstructId: string option
      Vpc: IVpc option
      RemovalPolicy: RemovalPolicy option
      Encrypted: bool option
      KmsKey: IKey option
      PerformanceMode: PerformanceMode option
      ThroughputMode: ThroughputMode option
      ProvisionedThroughputPerSecond: float option
      SecurityGroup: ISecurityGroup option }

type EfsFileSystemSpec =
    { ConstructId: string
      Props: FileSystemProps
      mutable FileSystem: IFileSystem option }

type EfsFileSystemBuilder(name) =
    member _.Yield(_: unit) : EfsFileSystemConfig =
        { Name = name
          ConstructId = None
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Zero() : EfsFileSystemConfig =
        { Name = name
          Vpc = None
          ConstructId = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Combine(a: EfsFileSystemConfig, b: EfsFileSystemConfig) : EfsFileSystemConfig =
        { Name = name
          ConstructId = Option.orElse a.ConstructId b.ConstructId
          Vpc = if a.Vpc.IsSome then a.Vpc else b.Vpc
          RemovalPolicy =
            if a.RemovalPolicy.IsSome then
                a.RemovalPolicy
            else
                b.RemovalPolicy
          Encrypted = if a.Encrypted.IsSome then a.Encrypted else b.Encrypted
          KmsKey = if a.KmsKey.IsSome then a.KmsKey else b.KmsKey
          PerformanceMode =
            if a.PerformanceMode.IsSome then
                a.PerformanceMode
            else
                b.PerformanceMode
          ThroughputMode =
            if a.ThroughputMode.IsSome then
                a.ThroughputMode
            else
                b.ThroughputMode
          ProvisionedThroughputPerSecond =
            if a.ProvisionedThroughputPerSecond.IsSome then
                a.ProvisionedThroughputPerSecond
            else
                b.ProvisionedThroughputPerSecond
          SecurityGroup =
            if a.SecurityGroup.IsSome then
                a.SecurityGroup
            else
                b.SecurityGroup }

    member _.Run(config: EfsFileSystemConfig) : EfsFileSystemSpec =
        let props = FileSystemProps()

        let constructId = config.ConstructId |> Option.defaultValue config.Name

        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)
        config.RemovalPolicy |> Option.iter (fun r -> props.RemovalPolicy <- r)
        config.Encrypted |> Option.iter (fun e -> props.Encrypted <- e)

        config.KmsKey |> Option.iter (fun k -> props.KmsKey <- k)

        config.PerformanceMode |> Option.iter (fun p -> props.PerformanceMode <- p)
        config.ThroughputMode |> Option.iter (fun t -> props.ThroughputMode <- t)

        config.ProvisionedThroughputPerSecond
        |> Option.iter (fun t -> props.ProvisionedThroughputPerSecond <- Size.Mebibytes(t))

        config.SecurityGroup |> Option.iter (fun sg -> props.SecurityGroup <- sg)

        { ConstructId = constructId
          Props = props
          FileSystem = None }


    member inline _.Delay(f: unit -> EfsFileSystemConfig) = f ()

    member inline x.For(state: EfsFileSystemConfig, f: unit -> EfsFileSystemConfig) = x.Combine(state, f ())

    [<CustomOperation("encrypted")>]
    member _.Encrypted(config: EfsFileSystemConfig, value: bool) = { config with Encrypted = Some value }

    [<CustomOperation("performanceMode")>]
    member _.PerformanceMode(config: EfsFileSystemConfig, mode: PerformanceMode) =
        { config with
            PerformanceMode = Some mode }

    [<CustomOperation("throughputMode")>]
    member _.ThroughputMode(config: EfsFileSystemConfig, mode: ThroughputMode) =
        { config with
            ThroughputMode = Some mode }

    [<CustomOperation("provisionedThroughputPerSecond")>]
    member _.ProvisionedThroughputPerSecond(config: EfsFileSystemConfig, throughput: float) =
        { config with
            ProvisionedThroughputPerSecond = Some throughput }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: EfsFileSystemConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: EfsFileSystemConfig, vpc: IVpc) = { config with Vpc = Some vpc }

    [<CustomOperation("kmsKey")>]
    member _.KmsKey(config: EfsFileSystemConfig, key: IKey) = { config with KmsKey = Some key }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: EfsFileSystemConfig, sg: ISecurityGroup) = { config with SecurityGroup = Some sg }

// ============================================================================
// EFS AccessPoint Builder DSL
// ============================================================================

type AccessPointConfig =
    { FileSystem: IFileSystem option
      ClientToken: string option
      CreateAcl: IAcl option
      Path: string option
      PosixUser: IPosixUser option }

type AccessPointSpec =
    { ConstructId: string
      Props: AccessPointProps
      mutable AccessPoint: IAccessPoint option }

type AccessPointBuilder(id: string) =
    member _.Yield(_: unit) : AccessPointConfig =
        { FileSystem = None
          ClientToken = None
          CreateAcl = None
          Path = None
          PosixUser = None }

    member _.Zero() : AccessPointConfig =
        { FileSystem = None
          ClientToken = None
          CreateAcl = None
          Path = None
          PosixUser = None }

    member _.Combine(a: AccessPointConfig, b: AccessPointConfig) : AccessPointConfig =
        { FileSystem = if a.FileSystem.IsSome then a.FileSystem else b.FileSystem
          ClientToken =
            if a.ClientToken.IsSome then
                a.ClientToken
            else
                b.ClientToken
          CreateAcl = if a.CreateAcl.IsSome then a.CreateAcl else b.CreateAcl
          Path = if a.Path.IsSome then a.Path else b.Path
          PosixUser = if a.PosixUser.IsSome then a.PosixUser else b.PosixUser }

    member _.Run(config: AccessPointConfig) : AccessPointSpec =
        let constructId = config.ClientToken |> Option.defaultValue id
        let props = AccessPointProps()

        config.FileSystem |> Option.iter (fun fs -> props.FileSystem <- fs)

        config.CreateAcl |> Option.iter (fun acl -> props.CreateAcl <- acl)

        config.PosixUser |> Option.iter (fun user -> props.PosixUser <- user)

        { ConstructId = constructId
          Props = props
          AccessPoint = None }


    member inline _.Delay(f: unit -> AccessPointConfig) = f ()
    member inline x.For(state: AccessPointConfig, f: unit -> AccessPointConfig) = x.Combine(state, f ())

    [<CustomOperation("fileSystem")>]
    member _.FileSystem(config: AccessPointConfig, fs: IFileSystem) = { config with FileSystem = Some fs }

    [<CustomOperation("clientToken")>]
    member _.ClientToken(config: AccessPointConfig, token: string) =
        { config with ClientToken = Some token }

    [<CustomOperation("createAcl")>]
    member _.CreateAcl(config: AccessPointConfig, acl: Acl) = { config with CreateAcl = Some acl }

    [<CustomOperation("createAcl")>]
    member this.CreateAcl(config: AccessPointConfig, ownerGid: string, ownerUid: string, permissions: string) =
        { config with
            CreateAcl = Some(Acl(OwnerGid = ownerGid, OwnerUid = ownerUid, Permissions = permissions)) }

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointConfig, user: IPosixUser) = { config with PosixUser = Some user }

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointConfig, uid: string, gid: string) =
        { config with
            PosixUser = Some(PosixUser(Gid = gid, Uid = uid)) }

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointConfig, uid: string, gid: string, secondaryGids: string array) =
        { config with
            PosixUser = Some(PosixUser(Gid = gid, Uid = uid, SecondaryGids = secondaryGids)) }

    [<CustomOperation("path")>]
    member _.Path(config: AccessPointConfig, path: string) = { config with Path = Some path }


[<AutoOpen>]
module LambdaFileSystemBuilders =
    let lambdaFileSystem = LambdaFileSystemBuilder()
    let efsFileSystem name = EfsFileSystemBuilder(name)
    let accessPoint id = AccessPointBuilder(id)
