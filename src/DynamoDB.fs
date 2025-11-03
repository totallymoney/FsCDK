namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.S3

// ============================================================================
// DynamoDB Table Configuration DSL
// ============================================================================

// ============================================================================
// ImportSourceSpecification Builder DSL
// ============================================================================

type ImportSourceSpec = { Source: IImportSourceSpecification }

type ImportSourceConfig =
    { Bucket: BucketRef option
      InputFormat: InputFormat option
      BucketOwner: string option
      CompressionType: InputCompressionType option
      KeyPrefix: string option }

type ImportSourceBuilder() =
    member _.Yield _ : ImportSourceConfig =
        { Bucket = None
          InputFormat = None
          BucketOwner = None
          CompressionType = None
          KeyPrefix = None }

    member _.Zero() : ImportSourceConfig =
        { Bucket = None
          InputFormat = None
          BucketOwner = None
          CompressionType = None
          KeyPrefix = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> ImportSourceConfig) : ImportSourceConfig = f ()

    member _.Combine(state1: ImportSourceConfig, state2: ImportSourceConfig) : ImportSourceConfig =
        { Bucket = state2.Bucket |> Option.orElse state1.Bucket
          InputFormat = state2.InputFormat |> Option.orElse state1.InputFormat
          BucketOwner = state2.BucketOwner |> Option.orElse state1.BucketOwner
          CompressionType = state2.CompressionType |> Option.orElse state1.CompressionType
          KeyPrefix = state2.KeyPrefix |> Option.orElse state1.KeyPrefix }

    member inline x.For
        (
            config: ImportSourceConfig,
            [<InlineIfLambda>] f: unit -> ImportSourceConfig
        ) : ImportSourceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ImportSourceConfig) : IImportSourceSpecification =
        let spec = ImportSourceSpecification()

        let bucket =
            match config.Bucket with
            | Some b -> b
            | None -> failwith "ImportSource.bucket is required"

        let input =
            match config.InputFormat with
            | Some i -> i
            | None -> failwith "ImportSource.inputFormat is required"

        spec.Bucket <-
            match bucket with
            | BucketRef.BucketInterface b -> b
            | BucketRef.BucketSpecRef b ->
                match b.Bucket with
                | None ->
                    failwith
                        $"Bucket '{b.BucketName}' has not been created yet. Ensure it's yielded in the stack before referencing it."
                | Some bu -> bu


        spec.InputFormat <- input

        config.BucketOwner |> Option.iter (fun o -> spec.BucketOwner <- o)
        config.CompressionType |> Option.iter (fun c -> spec.CompressionType <- c)
        config.KeyPrefix |> Option.iter (fun k -> spec.KeyPrefix <- k)

        spec :> IImportSourceSpecification

    /// <summary>Sets the S3 bucket for the import source.</summary>
    /// <param name="bucket">The S3 bucket to import from.</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     bucket myS3Bucket
    /// }
    /// </code>
    [<CustomOperation("bucket")>]
    member _.Bucket(config: ImportSourceConfig, bucket: IBucket) =
        { config with
            Bucket = Some(BucketRef.BucketInterface bucket) }

    [<CustomOperation("bucket")>]
    member _.Bucket(config: ImportSourceConfig, bucket: BucketSpec) =
        { config with
            Bucket = Some(BucketRef.BucketSpecRef bucket) }

    /// <summary>Sets the input format for the import.</summary>
    /// <param name="input">The input format (e.g., CSV, DynamoDB JSON, ION).</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     inputFormat InputFormat.csv()
    /// }
    /// </code>
    [<CustomOperation("inputFormat")>]
    member _.InputFormat(config: ImportSourceConfig, input: InputFormat) =
        { config with InputFormat = Some input }

    /// <summary>Sets the owner of the S3 bucket.</summary>
    /// <param name="owner">The AWS account ID that owns the bucket.</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     bucketOwner "123456789012"
    /// }
    /// </code>
    [<CustomOperation("bucketOwner")>]
    member _.BucketOwner(config: ImportSourceConfig, owner: string) =
        { config with BucketOwner = Some owner }

    /// <summary>Sets the compression type for the import files.</summary>
    /// <param name="c">The compression type (GZIP, ZSTD, or NONE).</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     compressionType InputCompressionType.GZIP
    /// }
    /// </code>
    [<CustomOperation("compressionType")>]
    member _.CompressionType(config: ImportSourceConfig, c: InputCompressionType) =
        { config with CompressionType = Some c }

    /// <summary>Sets the key prefix for filtering S3 objects.</summary>
    /// <param name="prefix">The S3 key prefix.</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     keyPrefix "data/tables/"
    /// }
    /// </code>
    [<CustomOperation("keyPrefix")>]
    member _.KeyPrefix(config: ImportSourceConfig, prefix: string) = { config with KeyPrefix = Some prefix }

open Amazon.CDK.AWS.KMS

type TableConfig =
    { TableName: string
      ConstructId: string option
      PartitionKey: (string * AttributeType) option
      SortKey: (string * AttributeType) option
      BillingMode: BillingMode option
      RemovalPolicy: RemovalPolicy option
      PointInTimeRecovery: bool option
      ImportSource: IImportSourceSpecification option
      Stream: StreamViewType option
      KinesisStream: IStream option
      Encryption: TableEncryption option
      EncryptionKey: IKey option }

type TableSpec =
    { TableName: string
      ConstructId: string
      Props: TableProps
      mutable Table: ITable option }

type TableBuilder(name: string) =
    member _.Yield _ : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = None
          EncryptionKey = None }

    member _.Yield(spec: ImportSourceSpec) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None
          ImportSource = Some spec.Source
          Stream = None
          KinesisStream = None
          Encryption = None
          EncryptionKey = None }

    member _.Zero() : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = None
          EncryptionKey = None }

    member _.Combine(config1: TableConfig, config2: TableConfig) : TableConfig =
        { config1 with
            ConstructId = config2.ConstructId |> Option.orElse config1.ConstructId
            PartitionKey = config2.PartitionKey |> Option.orElse config1.PartitionKey
            SortKey = config2.SortKey |> Option.orElse config1.SortKey
            BillingMode = config2.BillingMode |> Option.orElse config1.BillingMode
            RemovalPolicy = config2.RemovalPolicy |> Option.orElse config1.RemovalPolicy
            PointInTimeRecovery = config2.PointInTimeRecovery |> Option.orElse config1.PointInTimeRecovery
            ImportSource = config2.ImportSource |> Option.orElse config1.ImportSource
            Stream = config2.Stream |> Option.orElse config1.Stream
            KinesisStream = config2.KinesisStream |> Option.orElse config1.KinesisStream
            Encryption = config2.Encryption |> Option.orElse config1.Encryption
            EncryptionKey = config2.EncryptionKey |> Option.orElse config1.EncryptionKey }

    member inline x.For(config: TableConfig, [<InlineIfLambda>] f: unit -> TableConfig) : TableConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member inline _.Delay([<InlineIfLambda>] f: unit -> TableConfig) : TableConfig = f ()

    member _.Run(config: TableConfig) : TableSpec =
        let tableName = config.TableName

        let constructId = config.ConstructId |> Option.defaultValue tableName

        let props = TableProps(TableName = tableName)

        match config.PartitionKey with
        | Some(name, attrType) -> props.PartitionKey <- Attribute(Name = name, Type = attrType)
        | None -> failwith "Partition key is required for DynamoDB table"

        config.SortKey
        |> Option.iter (fun (name, attrType) -> props.SortKey <- Attribute(Name = name, Type = attrType))

        config.BillingMode |> Option.iter (fun mode -> props.BillingMode <- mode)

        config.RemovalPolicy
        |> Option.iter (fun policy -> props.RemovalPolicy <- System.Nullable<RemovalPolicy>(policy))

        config.PointInTimeRecovery
        |> Option.iter (fun enabled ->
            if enabled then
                props.PointInTimeRecoverySpecification <-
                    PointInTimeRecoverySpecification(PointInTimeRecoveryEnabled = true))

        config.ImportSource |> Option.iter (fun spec -> props.ImportSource <- spec)

        config.Stream |> Option.iter (fun streamType -> props.Stream <- streamType)

        config.KinesisStream
        |> Option.iter (fun kinesisStream -> props.KinesisStream <- kinesisStream)

        config.Encryption |> Option.iter (fun enc -> props.Encryption <- enc)

        config.EncryptionKey |> Option.iter (fun key -> props.EncryptionKey <- key)

        { TableName = tableName
          ConstructId = constructId
          Props = props
          Table = None }

    /// <summary>Sets the construct ID for the table.</summary>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     constructId "MyTableConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TableConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the partition key for the table.</summary>
    /// <param name="name">The attribute name for the partition key.</param>
    /// <param name="attrType">The attribute type (STRING, NUMBER, or BINARY).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    /// }
    /// </code>
    [<CustomOperation("partitionKey")>]
    member _.PartitionKey(config: TableConfig, name: string, attrType: AttributeType) =
        { config with
            PartitionKey = Some(name, attrType) }

    /// <summary>Sets the sort key for the table.</summary>
    /// <param name="name">The attribute name for the sort key.</param>
    /// <param name="attrType">The attribute type (STRING, NUMBER, or BINARY).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "userId" AttributeType.STRING
    ///     sortKey "timestamp" AttributeType.NUMBER
    /// }
    /// </code>
    [<CustomOperation("sortKey")>]
    member _.SortKey(config: TableConfig, name: string, attrType: AttributeType) =
        { config with
            SortKey = Some(name, attrType) }

    /// <summary>Sets the billing mode for the table.</summary>
    /// <param name="mode">The billing mode (PAY_PER_REQUEST or PROVISIONED).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     billingMode BillingMode.PAY_PER_REQUEST
    /// }
    /// </code>
    [<CustomOperation("billingMode")>]
    member _.BillingMode(config: TableConfig, mode: BillingMode) = { config with BillingMode = Some mode }

    /// <summary>Sets the removal policy for the table.</summary>
    /// <param name="policy">The removal policy (DESTROY, RETAIN, or SNAPSHOT).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     removalPolicy RemovalPolicy.DESTROY
    /// }
    /// </code>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: TableConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    /// <summary>Enables or disables point-in-time recovery.</summary>
    /// <param name="enabled">Whether point-in-time recovery is enabled.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     pointInTimeRecovery true
    /// }
    /// </code>
    [<CustomOperation("pointInTimeRecovery")>]
    member _.PointInTimeRecovery(config: TableConfig, enabled: bool) =
        { config with
            PointInTimeRecovery = Some enabled }

    member _.Yield(spec: IImportSourceSpecification) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None
          ImportSource = Some spec
          Stream = None
          KinesisStream = None
          Encryption = None
          EncryptionKey = None }

    /// <summary>Enables DynamoDB Streams for the table.</summary>
    /// <param name="streamType">The stream view type (KEYS_ONLY, NEW_IMAGE, OLD_IMAGE, or NEW_AND_OLD_IMAGES).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     stream StreamViewType.NEW_AND_OLD_IMAGES
    /// }
    /// </code>
    [<CustomOperation("stream")>]
    member _.Stream(config: TableConfig, streamType: StreamViewType) =
        { config with Stream = Some streamType }

    /// <summary>Sets a Kinesis stream for the table.</summary>
    /// <param name="stream">The Kinesis stream to use.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     kinesisStream myKinesisStream
    /// }
    /// </code>
    [<CustomOperation("kinesisStream")>]
    member _.KinesisStream(config: TableConfig, stream: IStream) =
        { config with
            KinesisStream = Some stream }

    /// <summary>Sets the encryption type for the table.</summary>
    /// <param name="encryption">The encryption type (AWS_MANAGED, CUSTOMER_MANAGED, or DEFAULT).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     encryption TableEncryption.AWS_MANAGED
    /// }
    /// </code>
    [<CustomOperation("encryption")>]
    member _.Encryption(config: TableConfig, encryption: TableEncryption) =
        { config with
            Encryption = Some encryption }

    /// <summary>Sets the KMS encryption key for the table.</summary>
    /// <param name="key">The KMS key.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     encryption TableEncryption.CUSTOMER_MANAGED
    ///     encryptionKey myKmsKey
    /// }
    /// </code>
    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: TableConfig, key: IKey) =
        { config with EncryptionKey = Some key }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module DynamoDBBuilders =
    /// <summary>Creates a DynamoDB table configuration.</summary>
    /// <param name="name">The table name.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     billingMode BillingMode.PAY_PER_REQUEST
    /// }
    /// </code>
    let table name = TableBuilder(name)

    /// <summary>Creates an import source specification for DynamoDB table imports.</summary>
    /// <code lang="fsharp">
    /// importSource {
    ///     bucket myBucket
    ///     inputFormat InputFormat.csv()
    ///     keyPrefix "data/"
    /// }
    /// </code>
    let importSource = ImportSourceBuilder()
