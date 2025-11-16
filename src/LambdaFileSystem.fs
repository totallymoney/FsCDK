namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.EFS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.KMS

// ============================================================================
// Lambda FileSystem Builder DSL
// ============================================================================

type LambdaFileSystemConfig =
    { AccessPoint: IAccessPoint option
      LocalMountPath: string option }

type LambdaFileSystemBuilder() =
    member _.Yield _ : LambdaFileSystemConfig =
        { AccessPoint = None
          LocalMountPath = None }

    member _.Zero() : LambdaFileSystemConfig =
        { AccessPoint = None
          LocalMountPath = None }

    member _.Combine(a: LambdaFileSystemConfig, b: LambdaFileSystemConfig) : LambdaFileSystemConfig =
        { AccessPoint =
            if a.AccessPoint.IsSome then
                a.AccessPoint
            else
                b.AccessPoint
          LocalMountPath =
            if a.LocalMountPath.IsSome then
                a.LocalMountPath
            else
                b.LocalMountPath }

    member inline _.Delay(f: unit -> LambdaFileSystemConfig) = f ()
    member inline x.For(state: LambdaFileSystemConfig, f: unit -> LambdaFileSystemConfig) = x.Combine(state, f ())

    member _.Run(cfg: LambdaFileSystemConfig) : Amazon.CDK.AWS.Lambda.FileSystem =
        match cfg.AccessPoint, cfg.LocalMountPath with
        | Some ap, Some path -> Amazon.CDK.AWS.Lambda.FileSystem.FromEfsAccessPoint(ap, path)
        | _ -> failwith "Both accessPoint and localMountPath are required for Lambda FileSystem"

    [<CustomOperation("localMountPath")>]
    member _.LocalMountPath(cfg: LambdaFileSystemConfig, path: string) = { cfg with LocalMountPath = Some path }

    // Complex type as implicit yield
    member _.Yield(ap: IAccessPoint) : LambdaFileSystemConfig =
        { AccessPoint = Some ap
          LocalMountPath = None }

// ============================================================================
// EFS FileSystem Builder DSL
// ============================================================================

type EfsFileSystemConfig =
    { Stack: Stack option
      Id: string
      Vpc: IVpc option
      RemovalPolicy: RemovalPolicy option
      Encrypted: bool option
      KmsKey: KMSKeyRef option
      PerformanceMode: PerformanceMode option
      ThroughputMode: ThroughputMode option
      ProvisionedThroughputPerSecond: float option
      SecurityGroup: SecurityGroupRef option }

type EfsFileSystemBuilder(id: string) =
    member _.Yield _ : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Zero() : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Run(config: EfsFileSystemConfig) : IFileSystem =
        match config.Stack, config.Vpc with
        | Some stack, Some vpc ->
            let props = FileSystemProps()
            props.Vpc <- vpc
            config.RemovalPolicy |> Option.iter (fun r -> props.RemovalPolicy <- r)
            config.Encrypted |> Option.iter (fun e -> props.Encrypted <- e)

            config.KmsKey
            |> Option.iter (fun v ->
                props.KmsKey <-
                    match v with
                    | KMSKeyRef.KMSKeyInterface i -> i
                    | KMSKeyRef.KMSKeySpecRef pr ->
                        match pr.Key with
                        | Some k -> k
                        | None -> failwith $"Key {pr.KeyName} has to be resolved first")

            config.PerformanceMode |> Option.iter (fun p -> props.PerformanceMode <- p)
            config.ThroughputMode |> Option.iter (fun t -> props.ThroughputMode <- t)

            config.ProvisionedThroughputPerSecond
            |> Option.iter (fun t -> props.ProvisionedThroughputPerSecond <- Size.Mebibytes(t))

            config.SecurityGroup
            |> Option.iter (fun sg -> props.SecurityGroup <- VpcHelpers.resolveSecurityGroupRef sg)

            FileSystem(stack, config.Id, props)
        | _ -> failwith "Both stack and vpc are required for FileSystem"

    member _.Combine(a: EfsFileSystemConfig, b: EfsFileSystemConfig) : EfsFileSystemConfig =
        { Stack = if a.Stack.IsSome then a.Stack else b.Stack
          Id = a.Id
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

    // Implicit yields for complex types
    member _.Yield(stack: Stack) : EfsFileSystemConfig =
        { Stack = Some stack
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(vpc: IVpc) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = Some vpc
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(key: IKey) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = Some(KMSKeyRef.KMSKeyInterface key)
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(key: KMSKeySpec) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = Some(KMSKeyRef.KMSKeySpecRef key)
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = None }

    member _.Yield(sg: ISecurityGroup) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = Some(SecurityGroupRef.SecurityGroupInterface sg) }

    member _.Yield(sg: SecurityGroupSpec) : EfsFileSystemConfig =
        { Stack = None
          Id = id
          Vpc = None
          RemovalPolicy = None
          Encrypted = None
          KmsKey = None
          PerformanceMode = None
          ThroughputMode = None
          ProvisionedThroughputPerSecond = None
          SecurityGroup = Some(SecurityGroupRef.SecurityGroupSpecRef sg) }

// ============================================================================
// EFS AccessPoint Builder DSL
// ============================================================================

type AccessPointConfig =
    { Stack: Stack
      Id: string
      Props: AccessPointProps }

type AccessPointBuilder(id: string) =
    member _.Yield _ : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps() }

    member _.Zero() : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps() }

    member _.Run(config: AccessPointConfig) : IAccessPoint =
        match isNull (box config.Stack), isNull (box config.Props.FileSystem) with
        | true, _ -> failwith "Stack is required for AccessPointBuilder"
        | _, true -> failwith "FileSystem is required for AccessPointBuilder"
        | _ -> AccessPoint(config.Stack, config.Id, config.Props)

    // Implicit yields for complex types
    member _.Yield(stack: Stack) : AccessPointConfig =
        { Stack = stack
          Id = id
          Props = AccessPointProps() }

    member _.Yield(fs: IFileSystem) : AccessPointConfig =
        { Stack = Unchecked.defaultof<Stack>
          Id = id
          Props = AccessPointProps(FileSystem = fs) }

    member _.Combine(a: AccessPointConfig, b: AccessPointConfig) : AccessPointConfig =
        let stack = if isNull (box a.Stack) then b.Stack else a.Stack
        let props = AccessPointProps()

        if not (isNull (box a.Props.FileSystem)) then
            props.FileSystem <- a.Props.FileSystem
        elif not (isNull (box b.Props.FileSystem)) then
            props.FileSystem <- b.Props.FileSystem

        if not (isNull (box a.Props.Path)) then
            props.Path <- a.Props.Path
        elif not (isNull (box b.Props.Path)) then
            props.Path <- b.Props.Path

        if not (isNull (box a.Props.PosixUser)) then
            props.PosixUser <- a.Props.PosixUser
        elif not (isNull (box b.Props.PosixUser)) then
            props.PosixUser <- b.Props.PosixUser

        if not (isNull (box a.Props.CreateAcl)) then
            props.CreateAcl <- a.Props.CreateAcl
        elif not (isNull (box b.Props.CreateAcl)) then
            props.CreateAcl <- b.Props.CreateAcl

        { Stack = stack
          Id = id
          Props = props }

    member inline _.Delay(f: unit -> AccessPointConfig) = f ()
    member inline x.For(state: AccessPointConfig, f: unit -> AccessPointConfig) = x.Combine(state, f ())

    // Custom operations only for primitive values
    [<CustomOperation("path")>]
    member _.Path(config: AccessPointConfig, value: string) =
        config.Props.Path <- value
        config

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointConfig, uid: string, gid: string) =
        config.Props.PosixUser <- PosixUser(Uid = uid, Gid = gid)
        config

    [<CustomOperation("createAcl")>]
    member _.CreateAcl(config: AccessPointConfig, ownerGid: string, ownerUid: string, permissions: string) =
        config.Props.CreateAcl <- Acl(OwnerGid = ownerGid, OwnerUid = ownerUid, Permissions = permissions)
        config

// ============================================================================
// EFS AccessPointProps Builder DSL
// ============================================================================

type AccessPointPropsConfig =
    { FileSystem: IFileSystem
      Path: string option
      PosixUser: PosixUser option
      CreateAcl: Acl option }

type AccessPointPropsBuilder(fileSystem: IFileSystem) =
    member _.Yield _ : AccessPointPropsConfig =
        { FileSystem = fileSystem
          Path = None
          PosixUser = None
          CreateAcl = None }

    member _.Zero() : AccessPointPropsConfig =
        { FileSystem = fileSystem
          Path = None
          PosixUser = None
          CreateAcl = None }

    member _.Combine(a: AccessPointPropsConfig, b: AccessPointPropsConfig) : AccessPointPropsConfig =
        { FileSystem = a.FileSystem
          Path = Option.orElse a.Path b.Path
          PosixUser = Option.orElse a.PosixUser b.PosixUser
          CreateAcl = Option.orElse a.CreateAcl b.CreateAcl }

    member inline _.Delay(f: unit -> AccessPointPropsConfig) = f ()

    member _.Run(config: AccessPointPropsConfig) =
        let props = AccessPointProps(FileSystem = config.FileSystem)
        config.Path |> Option.iter (fun p -> props.Path <- p)
        config.PosixUser |> Option.iter (fun u -> props.PosixUser <- u)
        config.CreateAcl |> Option.iter (fun a -> props.CreateAcl <- a)
        props

    [<CustomOperation("path")>]
    member _.Path(config: AccessPointPropsConfig, value: string) = { config with Path = Some value }

    [<CustomOperation("posixUser")>]
    member _.PosixUser(config: AccessPointPropsConfig, uid: string, gid: string) =
        let user = PosixUser(Gid = gid, Uid = uid)
        { config with PosixUser = Some user }

    [<CustomOperation("createAcl")>]
    member _.CreateAcl(config: AccessPointPropsConfig, ownerGid: string, ownerUid: string, permissions: string) =
        let acl = Acl(OwnerGid = ownerGid, OwnerUid = ownerUid, Permissions = permissions)
        { config with CreateAcl = Some acl }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module LambdaFileSystemBuilders =
    let lambdaFileSystem = LambdaFileSystemBuilder()
    let efsFileSystem id = EfsFileSystemBuilder(id)
    let accessPoint id = AccessPointBuilder(id)
    let accessPointProps fs = AccessPointPropsBuilder(fs)
