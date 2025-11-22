namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.KMS

// ============================================================================
// Kinesis Stream Configuration DSL
// ============================================================================

/// <summary>
/// High-level Kinesis Stream builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Encryption = enabled with AWS managed key
/// - Retention period = 24 hours
/// - Shard count = 1 (for low throughput)
/// - Stream mode = PROVISIONED
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Encryption at rest for data protection
/// - 24-hour retention balances cost and recovery needs
/// - Single shard for cost optimization (scale as needed)
/// - Provisioned mode for predictable costs
///
/// **Escape Hatch:**
/// Access the underlying CDK Stream via the `Stream` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type KinesisStreamConfig =
    { StreamName: string
      ConstructId: string option
      StreamName_: string option
      ShardCount: int option
      RetentionPeriod: Duration option
      StreamMode: StreamMode option
      Encryption: StreamEncryption option
      EncryptionKey: IKey option
      GrantReads: Amazon.CDK.AWS.IAM.IGrantable list
      GrantWrites: Amazon.CDK.AWS.IAM.IGrantable list }

type KinesisStreamSpec =
    { StreamName: string
      ConstructId: string
      Props: StreamProps
      // Sadly resource with access might not be created yet:
      GrantReads: ResizeArray<Amazon.CDK.AWS.IAM.IGrantable>
      GrantWrites: ResizeArray<Amazon.CDK.AWS.IAM.IGrantable>
      mutable Stream: IStream option }

    /// Gets the underlying IStream resource. Must be called after the stack is built.
    member this.Resource =
        match this.Stream with
        | Some stream -> stream
        | None ->
            failwith
                $"Kinesis Stream '{this.StreamName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type KinesisStreamBuilder(name: string) =
    member _.Yield(_: unit) : KinesisStreamConfig =
        { StreamName = name
          ConstructId = None
          StreamName_ = None
          ShardCount = Some 1
          RetentionPeriod = Some(Duration.Hours(24.0))
          StreamMode = Some StreamMode.PROVISIONED
          Encryption = Some StreamEncryption.MANAGED
          EncryptionKey = None
          GrantReads = List.empty
          GrantWrites = List.empty }

    member _.Zero() : KinesisStreamConfig =
        { StreamName = name
          ConstructId = None
          StreamName_ = None
          ShardCount = Some 1
          RetentionPeriod = Some(Duration.Hours(24.0))
          StreamMode = Some StreamMode.PROVISIONED
          Encryption = Some StreamEncryption.MANAGED
          EncryptionKey = None
          GrantReads = List.empty
          GrantWrites = List.empty }

    member inline _.Delay([<InlineIfLambda>] f: unit -> KinesisStreamConfig) : KinesisStreamConfig = f ()

    member inline x.For
        (
            config: KinesisStreamConfig,
            [<InlineIfLambda>] f: unit -> KinesisStreamConfig
        ) : KinesisStreamConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: KinesisStreamConfig, b: KinesisStreamConfig) : KinesisStreamConfig =
        { StreamName = a.StreamName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          StreamName_ =
            match a.StreamName_ with
            | Some _ -> a.StreamName_
            | None -> b.StreamName_
          ShardCount =
            match a.ShardCount with
            | Some _ -> a.ShardCount
            | None -> b.ShardCount
          RetentionPeriod =
            match a.RetentionPeriod with
            | Some _ -> a.RetentionPeriod
            | None -> b.RetentionPeriod
          StreamMode =
            match a.StreamMode with
            | Some _ -> a.StreamMode
            | None -> b.StreamMode
          Encryption =
            match a.Encryption with
            | Some _ -> a.Encryption
            | None -> b.Encryption
          EncryptionKey =
            (match a.EncryptionKey with
             | Some _ -> a.EncryptionKey
             | None -> b.EncryptionKey)
          GrantReads = a.GrantReads @ b.GrantReads
          GrantWrites = a.GrantWrites @ b.GrantWrites }

    member _.Run(config: KinesisStreamConfig) : KinesisStreamSpec =
        let props = StreamProps()
        let constructId = config.ConstructId |> Option.defaultValue config.StreamName

        config.StreamName_ |> Option.iter (fun n -> props.StreamName <- n)

        // AWS Best Practice: Default to 1 shard for cost optimization
        props.ShardCount <- config.ShardCount |> Option.defaultValue 1 |> double

        // AWS Best Practice: 24-hour retention by default
        props.RetentionPeriod <- config.RetentionPeriod |> Option.defaultValue (Duration.Hours(24.0))

        // AWS Best Practice: Enable encryption with AWS managed key
        props.Encryption <- config.Encryption |> Option.defaultValue StreamEncryption.MANAGED

        config.EncryptionKey |> Option.iter (fun k -> props.EncryptionKey <- k)

        config.StreamMode |> Option.iter (fun m -> props.StreamMode <- m)

        { StreamName = config.StreamName
          ConstructId = constructId
          Props = props
          GrantReads = ResizeArray(config.GrantReads)
          GrantWrites = ResizeArray(config.GrantWrites)
          Stream = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: KinesisStreamConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the stream name.</summary>
    [<CustomOperation("streamName")>]
    member _.StreamName(config: KinesisStreamConfig, name: string) = { config with StreamName_ = Some name }

    /// <summary>Sets the number of shards.</summary>
    [<CustomOperation("shardCount")>]
    member _.ShardCount(config: KinesisStreamConfig, count: int) = { config with ShardCount = Some count }

    /// <summary>Sets the retention period.</summary>
    [<CustomOperation("retentionPeriod")>]
    member _.RetentionPeriod(config: KinesisStreamConfig, period: Duration) =
        { config with
            RetentionPeriod = Some period }

    /// <summary>Sets the stream mode.</summary>
    [<CustomOperation("streamMode")>]
    member _.StreamMode(config: KinesisStreamConfig, mode: StreamMode) = { config with StreamMode = Some mode }

    /// <summary>Uses on-demand stream mode.</summary>
    [<CustomOperation("onDemand")>]
    member _.OnDemand(config: KinesisStreamConfig) =
        { config with
            StreamMode = Some StreamMode.ON_DEMAND
            ShardCount = None }

    /// <summary>Disables encryption.</summary>
    [<CustomOperation("unencrypted")>]
    member _.Unencrypted(config: KinesisStreamConfig) =
        { config with
            Encryption = Some StreamEncryption.UNENCRYPTED }

    /// <summary>Uses a custom KMS key for encryption.</summary>
    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: KinesisStreamConfig, key: Amazon.CDK.AWS.KMS.IKey) =
        { config with
            Encryption = Some StreamEncryption.KMS
            EncryptionKey = Some key }

    /// <summary>Uses a custom KMS key for encryption.</summary>
    [<CustomOperation("encryption")>]
    member _.EncryptionKey(config: KinesisStreamConfig, encryption: StreamEncryption) =
        { config with
            Encryption = Some encryption }

    /// <summary>Grant read for role to stream.</summary>
    [<CustomOperation("grantRead")>]
    member _.GrantRead(config: KinesisStreamConfig, reader: Amazon.CDK.AWS.IAM.IGrantable) =
        { config with
            GrantReads = reader :: config.GrantReads }

    /// <summary>Grant WRITE for role to stream.</summary>
    [<CustomOperation("grantWrite")>]
    member _.GrantWrite(config: KinesisStreamConfig, writer: Amazon.CDK.AWS.IAM.IGrantable) =
        { config with
            GrantWrites = writer :: config.GrantWrites }
// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module KinesisBuilders =
    /// <summary>Creates a Kinesis stream with AWS best practices.</summary>
    /// <param name="name">The stream name.</param>
    /// <code lang="fsharp">
    /// kinesisStream "MyStream" {
    ///     shardCount 2
    ///     retentionPeriod (Duration.Hours(48.0))
    /// }
    /// </code>
    let kinesisStream (name: string) = KinesisStreamBuilder name
