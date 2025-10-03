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
    { Bucket: IBucket option
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

    member _.Delay(f: unit -> ImportSourceConfig) : ImportSourceConfig = f ()

    member _.Combine(state1: ImportSourceConfig, state2: ImportSourceConfig) : ImportSourceConfig =
        { Bucket = state2.Bucket |> Option.orElse state1.Bucket
          InputFormat = state2.InputFormat |> Option.orElse state1.InputFormat
          BucketOwner = state2.BucketOwner |> Option.orElse state1.BucketOwner
          CompressionType = state2.CompressionType |> Option.orElse state1.CompressionType
          KeyPrefix = state2.KeyPrefix |> Option.orElse state1.KeyPrefix }

    member x.For(config: ImportSourceConfig, f: unit -> ImportSourceConfig) : ImportSourceConfig =
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

        spec.Bucket <- bucket
        spec.InputFormat <- input

        config.BucketOwner |> Option.iter (fun o -> spec.BucketOwner <- o)
        config.CompressionType |> Option.iter (fun c -> spec.CompressionType <- c)
        config.KeyPrefix |> Option.iter (fun k -> spec.KeyPrefix <- k)

        spec :> IImportSourceSpecification

    [<CustomOperation("bucket")>]
    member _.Bucket(config: ImportSourceConfig, bucket: IBucket) = { config with Bucket = Some bucket }

    [<CustomOperation("inputFormat")>]
    member _.InputFormat(config: ImportSourceConfig, input: InputFormat) =
        { config with InputFormat = Some input }

    [<CustomOperation("bucketOwner")>]
    member _.BucketOwner(config: ImportSourceConfig, owner: string) =
        { config with BucketOwner = Some owner }

    [<CustomOperation("compressionType")>]
    member _.CompressionType(config: ImportSourceConfig, c: InputCompressionType) =
        { config with CompressionType = Some c }

    [<CustomOperation("keyPrefix")>]
    member _.KeyPrefix(config: ImportSourceConfig, prefix: string) = { config with KeyPrefix = Some prefix }

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
      KinesisStream: IStream option }

type TableSpec =
    { TableName: string
      ConstructId: string
      Props: TableProps }

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
          KinesisStream = None }

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
          KinesisStream = None }

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
          KinesisStream = None }

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
            KinesisStream = config2.KinesisStream |> Option.orElse config1.KinesisStream }

    member x.For(config: TableConfig, f: unit -> TableConfig) : TableConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Delay(f: unit -> TableConfig) : TableConfig = f ()

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

        { TableName = tableName
          ConstructId = constructId
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TableConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("partitionKey")>]
    member _.PartitionKey(config: TableConfig, name: string, attrType: AttributeType) =
        { config with
            PartitionKey = Some(name, attrType) }

    [<CustomOperation("sortKey")>]
    member _.SortKey(config: TableConfig, name: string, attrType: AttributeType) =
        { config with
            SortKey = Some(name, attrType) }

    [<CustomOperation("billingMode")>]
    member _.BillingMode(config: TableConfig, mode: BillingMode) = { config with BillingMode = Some mode }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: TableConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

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
          KinesisStream = None }

    [<CustomOperation("stream")>]
    member _.Stream(config: TableConfig, streamType: StreamViewType) =
        { config with Stream = Some streamType }

    [<CustomOperation("kinesisStream")>]
    member _.KinesisStream(config: TableConfig, stream: IStream) =
        { config with
            KinesisStream = Some stream }
