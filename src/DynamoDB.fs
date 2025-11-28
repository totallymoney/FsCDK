namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.S3

// ============================================================================
//  DynamoDB Configuration DSL
// ============================================================================

type GlobalSecondaryIndexConfig =
    { IndexName: string
      ContributorInsightsSpecification: IContributorInsightsSpecification option
      MaxReadRequestUnits: float option
      MaxWriteRequestUnits: float option
      ReadCapacity: float option
      WarmThroughput: IWarmThroughput option
      WriteCapacity: float option
      NonKeyAttributes: string list
      ProjectionType: ProjectionType option
      PartitionKey: IAttribute option
      SortKey: IAttribute option }


type GlobalSecondaryIndexBuilder(indexName: string) =

    member _.Yield(_: unit) : GlobalSecondaryIndexConfig =
        { IndexName = indexName
          ContributorInsightsSpecification = None
          MaxReadRequestUnits = None
          MaxWriteRequestUnits = None
          ReadCapacity = None
          WarmThroughput = None
          WriteCapacity = None
          NonKeyAttributes = []
          ProjectionType = None
          PartitionKey = None
          SortKey = None }

    member _.Zero() : GlobalSecondaryIndexConfig =
        { IndexName = indexName
          ContributorInsightsSpecification = None
          MaxReadRequestUnits = None
          MaxWriteRequestUnits = None
          ReadCapacity = None
          WarmThroughput = None
          WriteCapacity = None
          NonKeyAttributes = []
          ProjectionType = None
          PartitionKey = None
          SortKey = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> GlobalSecondaryIndexConfig) : GlobalSecondaryIndexConfig = f ()

    member _.Combine
        (
            config1: GlobalSecondaryIndexConfig,
            config2: GlobalSecondaryIndexConfig
        ) : GlobalSecondaryIndexConfig =
        { config1 with
            ContributorInsightsSpecification =
                config2.ContributorInsightsSpecification
                |> Option.orElse config1.ContributorInsightsSpecification
            MaxReadRequestUnits = config2.MaxReadRequestUnits |> Option.orElse config1.MaxReadRequestUnits
            MaxWriteRequestUnits = config2.MaxWriteRequestUnits |> Option.orElse config1.MaxWriteRequestUnits
            ReadCapacity = config2.ReadCapacity |> Option.orElse config1.ReadCapacity
            WarmThroughput = config2.WarmThroughput |> Option.orElse config1.WarmThroughput
            WriteCapacity = config2.WriteCapacity |> Option.orElse config1.WriteCapacity
            NonKeyAttributes = config1.NonKeyAttributes @ config2.NonKeyAttributes
            ProjectionType = config2.ProjectionType |> Option.orElse config1.ProjectionType
            PartitionKey = config2.PartitionKey |> Option.orElse config1.PartitionKey
            SortKey = config2.SortKey |> Option.orElse config1.SortKey }


    member inline x.For
        (
            config: GlobalSecondaryIndexConfig,
            [<InlineIfLambda>] f: unit -> GlobalSecondaryIndexConfig
        ) : GlobalSecondaryIndexConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: GlobalSecondaryIndexConfig) : GlobalSecondaryIndexProps =
        let indexName = config.IndexName

        let props = GlobalSecondaryIndexProps()
        props.IndexName <- indexName

        config.ContributorInsightsSpecification
        |> Option.iter (fun spec -> props.ContributorInsightsSpecification <- spec)

        config.MaxReadRequestUnits
        |> Option.iter (fun max -> props.MaxReadRequestUnits <- max)

        config.MaxWriteRequestUnits
        |> Option.iter (fun max -> props.MaxWriteRequestUnits <- max)

        config.ReadCapacity |> Option.iter (fun read -> props.ReadCapacity <- read)

        config.WarmThroughput |> Option.iter (fun warm -> props.WarmThroughput <- warm)

        config.WriteCapacity |> Option.iter (fun write -> props.WriteCapacity <- write)

        if not (List.isEmpty config.NonKeyAttributes) then
            props.NonKeyAttributes <- List.toArray config.NonKeyAttributes

        config.ProjectionType
        |> Option.iter (fun projType -> props.ProjectionType <- projType)

        config.PartitionKey |> Option.iter (fun attr -> props.PartitionKey <- attr)

        config.SortKey |> Option.iter (fun attr -> props.SortKey <- attr)

        props

    /// <summary>Sets the partition key for the Global Secondary Index (GSI).</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="attr">The attribute definition to use as the partition key.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     partitionKey (Attribute(Name = "gsiPk", Type = AttributeType.STRING))
    /// }
    /// </code>
    [<CustomOperation("partitionKey")>]
    member _.PartitionKey(config: GlobalSecondaryIndexConfig, attr: Attribute) =
        { config with PartitionKey = Some attr }

    /// <summary>Sets the partition key for the Global Secondary Index (GSI).</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="attrName">The attribute name for the partition key.</param>
    /// <param name="attrType">The attribute type (STRING, NUMBER, or BINARY).</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     partitionKey "gsiPk" AttributeType.STRING
    /// }
    /// </code>
    [<CustomOperation("partitionKey")>]
    member _.PartitionKey(config: GlobalSecondaryIndexConfig, attrName: string, attrType: AttributeType) =
        { config with
            PartitionKey = Some(Attribute(Name = attrName, Type = attrType)) }

    /// <summary>Sets the sort key for the Global Secondary Index (GSI).</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="attr">The attribute definition to use as the sort key.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     sortKey (Attribute(Name = "gsiSk", Type = AttributeType.NUMBER))
    /// }
    /// </code>
    [<CustomOperation("sortKey")>]
    member _.SortKey(config: GlobalSecondaryIndexConfig, attr: Attribute) = { config with SortKey = Some attr }

    /// <summary>Sets the sort key for the Global Secondary Index (GSI).</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="attrName">The attribute name for the sort key.</param>
    /// <param name="attrType">The attribute type (STRING, NUMBER, or BINARY).</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     sortKey "gsiSk" AttributeType.NUMBER
    /// }
    /// </code>
    [<CustomOperation("sortKey")>]
    member _.SortKey(config: GlobalSecondaryIndexConfig, attrName: string, attrType: AttributeType) =
        { config with
            SortKey = Some(Attribute(Name = attrName, Type = attrType)) }

    /// <summary>Sets the projection type for the GSI.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="projType">The projection type (ALL, KEYS_ONLY, or INCLUDE).</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     projectionType ProjectionType.ALL
    /// }
    /// </code>
    [<CustomOperation("projectionType")>]
    member _.ProjectionType(config: GlobalSecondaryIndexConfig, projType: ProjectionType) =
        { config with
            ProjectionType = Some projType }

    /// <summary>Specifies additional non-key attributes to include in the GSI projection.</summary>
    /// <remarks>Only used when <c>projectionType</c> is <c>INCLUDE</c>.</remarks>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="attrs">The list of non-key attribute names to include.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     projectionType ProjectionType.INCLUDE
    ///     nonKeyAttributes [ "status"; "createdAt" ]
    /// }
    /// </code>
    [<CustomOperation("nonKeyAttributes")>]
    member _.NonKeyAttributes(config: GlobalSecondaryIndexConfig, attrs: string list) =
        { config with NonKeyAttributes = attrs }

    /// <summary>Sets the maximum on-demand read request units for the GSI.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="max">The maximum read request units.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     maxReadRequestUnits 40000
    /// }
    /// </code>
    [<CustomOperation("maxReadRequestUnits")>]
    member _.MaxReadRequestUnits(config: GlobalSecondaryIndexConfig, max: float) =
        { config with
            MaxReadRequestUnits = Some max }

    /// <summary>Sets the maximum on-demand write request units for the GSI.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="max">The maximum write request units.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     maxWriteRequestUnits 40000
    /// }
    /// </code>
    [<CustomOperation("maxWriteRequestUnits")>]
    member _.MaxWriteRequestUnits(config: GlobalSecondaryIndexConfig, max: float) =
        { config with
            MaxWriteRequestUnits = Some max }

    /// <summary>Sets the provisioned read capacity for the GSI.</summary>
    /// <remarks>Only applicable when using PROVISIONED capacity mode.</remarks>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="read">The provisioned read capacity units.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     readCapacity 10
    /// }
    /// </code>
    [<CustomOperation("readCapacity")>]
    member _.ReadCapacity(config: GlobalSecondaryIndexConfig, read: float) =
        { config with ReadCapacity = Some read }

    /// <summary>Sets warm throughput settings for the GSI in on-demand mode.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="warm">The warm throughput configuration.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     warmThroughput (WarmThroughput.fixed 1000.)
    /// }
    /// </code>
    [<CustomOperation("warmThroughput")>]
    member _.WarmThroughput(config: GlobalSecondaryIndexConfig, warm: IWarmThroughput) =
        { config with
            WarmThroughput = Some warm }

    /// <summary>Sets the provisioned write capacity for the GSI.</summary>
    /// <remarks>Only applicable when using PROVISIONED capacity mode.</remarks>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="write">The provisioned write capacity units.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     writeCapacity 10
    /// }
    /// </code>
    [<CustomOperation("writeCapacity")>]
    member _.WriteCapacity(config: GlobalSecondaryIndexConfig, write: float) =
        { config with
            WriteCapacity = Some write }

    /// <summary>Sets the CloudWatch Contributor Insights specification for the GSI.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="spec">The contributor insights specification.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     contributorInsightsSpecification (ContributorInsightsSpecification(Enabled = true))
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsSpecification")>]
    member _.ContributorInsightsSpecification
        (
            config: GlobalSecondaryIndexConfig,
            spec: ContributorInsightsSpecification
        ) =
        { config with
            ContributorInsightsSpecification = Some spec }

    /// <summary>Configures CloudWatch Contributor Insights for the GSI.</summary>
    /// <param name="config">The current global secondary index configuration.</param>
    /// <param name="enabled">Whether contributor insights are enabled.</param>
    /// <param name="mode">The contributor insights mode.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     contributorInsightsSpecification true ContributorInsightsMode.CONTRIBUTOR_INSIGHTS
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsSpecification")>]
    member _.ContributorInsightsSpecification
        (
            config: GlobalSecondaryIndexConfig,
            enabled: bool,
            mode: ContributorInsightsMode
        ) =
        { config with
            ContributorInsightsSpecification = Some(ContributorInsightsSpecification(Enabled = enabled, Mode = mode)) }

    /// <summary>Configures CloudWatch Contributor Insights for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="enabled">Whether to enable contributor insights.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     contributorInsightsEnabled true
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsEnabled")>]
    member _.ContributorInsightsEnabled(config: GlobalSecondaryIndexConfig, enabled: bool) =
        { config with
            ContributorInsightsSpecification = Some(ContributorInsightsSpecification(Enabled = enabled)) }


type ImportSourceConfig =
    { Bucket: IBucket option
      InputFormat: InputFormat option
      BucketOwner: string option
      CompressionType: InputCompressionType option
      KeyPrefix: string option }

type ImportSourceBuilder() =
    member _.Yield(_: unit) : ImportSourceConfig =
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

        spec.Bucket <- bucket

        spec.InputFormat <- input

        config.BucketOwner |> Option.iter (fun o -> spec.BucketOwner <- o)
        config.CompressionType |> Option.iter (fun c -> spec.CompressionType <- c)
        config.KeyPrefix |> Option.iter (fun k -> spec.KeyPrefix <- k)

        spec

    /// <summary>Sets the S3 bucket for the import source.</summary>
    /// <param name="config">The current import source configuration.</param>
    /// <param name="bucket">The S3 bucket to import from.</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     bucket myS3Bucket
    /// }
    /// </code>
    [<CustomOperation("bucket")>]
    member _.Bucket(config: ImportSourceConfig, bucket: IBucket) = { config with Bucket = Some(bucket) }

    /// <summary>Sets the input format for the import.</summary>
    /// <param name="config">The current import source configuration.</param>
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
    /// <param name="config">The current import source configuration.</param>
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
    /// <param name="config">The current import source configuration.</param>
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
    /// <param name="config">The current import source configuration.</param>
    /// <param name="prefix">The S3 key prefix.</param>
    /// <code lang="fsharp">
    /// importSource {
    ///     keyPrefix "data/tables/"
    /// }
    /// </code>
    [<CustomOperation("keyPrefix")>]
    member _.KeyPrefix(config: ImportSourceConfig, prefix: string) = { config with KeyPrefix = Some prefix }

open Amazon.CDK.AWS.KMS

type LocalSecondaryIndexConfig =
    { IndexName: string
      SortKey: IAttribute option
      ProjectionType: ProjectionType option
      NonKeyAttributes: string list }

type LocalSecondaryIndexBuilder(indexName: string) =

    member _.Yield(_: unit) : LocalSecondaryIndexConfig =
        { IndexName = indexName
          SortKey = None
          ProjectionType = None
          NonKeyAttributes = [] }

    member _.Zero() : LocalSecondaryIndexConfig =
        { IndexName = indexName
          SortKey = None
          ProjectionType = None
          NonKeyAttributes = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LocalSecondaryIndexConfig) : LocalSecondaryIndexConfig = f ()

    member _.Combine(c1: LocalSecondaryIndexConfig, c2: LocalSecondaryIndexConfig) : LocalSecondaryIndexConfig =
        { c1 with
            SortKey = c2.SortKey |> Option.orElse c1.SortKey
            ProjectionType = c2.ProjectionType |> Option.orElse c1.ProjectionType
            NonKeyAttributes = c1.NonKeyAttributes @ c2.NonKeyAttributes }

    member inline x.For
        (
            cfg: LocalSecondaryIndexConfig,
            [<InlineIfLambda>] f: unit -> LocalSecondaryIndexConfig
        ) : LocalSecondaryIndexConfig =
        let n = f ()
        x.Combine(cfg, n)

    member _.Run(cfg: LocalSecondaryIndexConfig) : LocalSecondaryIndexProps =
        let props = LocalSecondaryIndexProps()
        props.IndexName <- cfg.IndexName
        cfg.SortKey |> Option.iter (fun sk -> props.SortKey <- sk)
        cfg.ProjectionType |> Option.iter (fun pt -> props.ProjectionType <- pt)

        if not (List.isEmpty cfg.NonKeyAttributes) then
            props.NonKeyAttributes <- List.toArray cfg.NonKeyAttributes

        props

    /// <summary>Sets the sort key for the Local Secondary Index (LSI).</summary>
    /// <param name="config">The current local secondary index configuration.</param>
    /// <param name="attr">The attribute definition to use as the sort key.</param>
    [<CustomOperation("sortKey")>]
    member _.SortKey(config: LocalSecondaryIndexConfig, attr: Attribute) = { config with SortKey = Some attr }

    /// <summary>Sets the sort key for the Local Secondary Index (LSI).</summary>
    /// <param name="config">The current local secondary index configuration.</param>
    /// <param name="attrName">The attribute name for the sort key.</param>
    /// <param name="attrType">The attribute type (STRING, NUMBER, or BINARY).</param>
    [<CustomOperation("sortKey")>]
    member _.SortKey(config: LocalSecondaryIndexConfig, attrName: string, attrType: AttributeType) =
        { config with
            SortKey = Some(Attribute(Name = attrName, Type = attrType)) }

    /// <summary>Sets the projection type for the LSI.</summary>
    /// <param name="config">The current local secondary index configuration.</param>
    /// <param name="projType">The projection type (ALL, KEYS_ONLY, or INCLUDE).</param>
    [<CustomOperation("projectionType")>]
    member _.ProjectionType(config: LocalSecondaryIndexConfig, projType: ProjectionType) =
        { config with
            ProjectionType = Some projType }

    /// <summary>Specifies additional non-key attributes to include in the LSI projection.</summary>
    /// <remarks>Only used when <c>projectionType</c> is <c>INCLUDE</c>.</remarks>
    /// <param name="config">The current local secondary index configuration.</param>
    /// <param name="attrs">The list of non-key attribute names to include.</param>
    [<CustomOperation("nonKeyAttributes")>]
    member _.NonKeyAttributes(config: LocalSecondaryIndexConfig, attrs: string list) =
        { config with NonKeyAttributes = attrs }

type TableGrantAccessType =
    | GrantReadData of IGrantable
    | GrantFullAccess of IGrantable
    | GrantReadWriteData of IGrantable
    | GrantWriteData of IGrantable
    | GrantStreamRead of IGrantable
    | GrantStream of (IGrantable * string list)
    | GrantTableListStreams of IGrantable
    | Grant of (IGrantable * string list)

type TableConfig =
    { TableName: string
      ConstructId: string option
      PartitionKey: (string * AttributeType) option
      SortKey: (string * AttributeType) option
      BillingMode: BillingMode option
      RemovalPolicy: RemovalPolicy option
      PointInTimeRecovery: bool option
      TimeToLiveAttribute: string option
      GlobalSecondaryIndexes: GlobalSecondaryIndexProps list
      LocalSecondaryIndexes: LocalSecondaryIndexProps list
      TableClass: TableClass option
      ContributorInsightsSpecification: ContributorInsightsSpecification option
      ImportSource: IImportSourceSpecification option
      Stream: StreamViewType option
      KinesisStream: IStream option
      Encryption: TableEncryption option
      EncryptionKey: IKey option
      Grant: TableGrantAccessType option }

type TableSpec =
    { TableName: string
      ConstructId: string
      Props: TableProps
      GlobalSecondaryIndexes: GlobalSecondaryIndexProps list
      LocalSecondaryIndexes: LocalSecondaryIndexProps list
      Grant: TableGrantAccessType option
      mutable Table: ITable option }

type TableBuilder(name: string) =
    member _.Yield(_: unit) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsSpecification = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED
          EncryptionKey = None
          Grant = None }

    member _.Yield(spec: IImportSourceSpecification) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsSpecification = None
          ImportSource = Some spec
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED
          EncryptionKey = None
          Grant = None }

    member _.Yield(gsi: GlobalSecondaryIndexProps) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = [ gsi ]
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsSpecification = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED
          EncryptionKey = None
          Grant = None }

    member _.Yield(lsi: LocalSecondaryIndexProps) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = [ lsi ]
          TableClass = None
          ContributorInsightsSpecification = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED
          EncryptionKey = None
          Grant = None }

    member _.Zero() : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsSpecification = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED
          EncryptionKey = None
          Grant = None }

    member _.Combine(config1: TableConfig, config2: TableConfig) : TableConfig =
        { config1 with
            ConstructId = config2.ConstructId |> Option.orElse config1.ConstructId
            PartitionKey = config2.PartitionKey |> Option.orElse config1.PartitionKey
            SortKey = config2.SortKey |> Option.orElse config1.SortKey
            BillingMode = config2.BillingMode |> Option.orElse config1.BillingMode
            RemovalPolicy = config2.RemovalPolicy |> Option.orElse config1.RemovalPolicy
            PointInTimeRecovery = config2.PointInTimeRecovery |> Option.orElse config1.PointInTimeRecovery
            TimeToLiveAttribute = config2.TimeToLiveAttribute |> Option.orElse config1.TimeToLiveAttribute
            GlobalSecondaryIndexes = config1.GlobalSecondaryIndexes @ config2.GlobalSecondaryIndexes
            LocalSecondaryIndexes = config1.LocalSecondaryIndexes @ config2.LocalSecondaryIndexes
            TableClass = config2.TableClass |> Option.orElse config1.TableClass
            ContributorInsightsSpecification =
                config2.ContributorInsightsSpecification
                |> Option.orElse config1.ContributorInsightsSpecification
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
        |> Option.iter (fun policy -> props.RemovalPolicy <- policy)

        let pitrEnabled = config.PointInTimeRecovery |> Option.defaultValue true

        if pitrEnabled then
            props.PointInTimeRecoverySpecification <-
                PointInTimeRecoverySpecification(PointInTimeRecoveryEnabled = true)

        config.TimeToLiveAttribute
        |> Option.iter (fun attr -> props.TimeToLiveAttribute <- attr)

        config.TableClass |> Option.iter (fun tc -> props.TableClass <- tc)

        config.ContributorInsightsSpecification
        |> Option.iter (fun spec -> props.ContributorInsightsSpecification <- spec)

        config.ImportSource |> Option.iter (fun spec -> props.ImportSource <- spec)

        config.Stream |> Option.iter (fun streamType -> props.Stream <- streamType)

        config.KinesisStream
        |> Option.iter (fun kinesisStream -> props.KinesisStream <- kinesisStream)

        config.Encryption |> Option.iter (fun enc -> props.Encryption <- enc)

        config.EncryptionKey |> Option.iter (fun key -> props.EncryptionKey <- key)

        { TableName = tableName
          ConstructId = constructId
          Props = props
          GlobalSecondaryIndexes = config.GlobalSecondaryIndexes
          LocalSecondaryIndexes = config.LocalSecondaryIndexes
          Grant = config.Grant
          Table = None }

    /// <summary>Sets the construct ID for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     constructId "MyTableConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TableConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the partition key for the table.</summary>
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
    /// <param name="mode">The billing mode (PAY_PER_REQUEST or PROVISIONED).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     billingMode BillingMode.PAY_PER_REQUEST
    /// }
    /// </code>
    [<CustomOperation("billingMode")>]
    member _.BillingMode(config: TableConfig, mode: BillingMode) = { config with BillingMode = Some mode }

    /// <summary>Sets the removal policy for the table.</summary>
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
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

    /// <summary>Enables DynamoDB Streams for the table.</summary>
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
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
    /// <param name="config">The current table configuration.</param>
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

    /// <summary>Sets the Time-to-Live attribute for automatic item expiration.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="attributeName">The attribute name that stores the TTL timestamp (Unix epoch seconds).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     timeToLive "expiresAt"
    /// }
    /// </code>
    [<CustomOperation("timeToLive")>]
    member _.TimeToLive(config: TableConfig, attributeName: string) =
        { config with
            TimeToLiveAttribute = Some attributeName }

    /// <summary>Sets the table class for cost optimization.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="tableClass">The table class (STANDARD or STANDARD_INFREQUENT_ACCESS).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     tableClass TableClass.STANDARD_INFREQUENT_ACCESS
    /// }
    /// </code>
    [<CustomOperation("tableClass")>]
    member _.TableClass(config: TableConfig, tableClass: TableClass) =
        { config with
            TableClass = Some tableClass }

    /// <summary>Configures CloudWatch Contributor Insights for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="contributorInsights">Whether to enable contributor insights.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     contributorInsightsSpecification (ContributorInsightsSpecification(Enabled = true))
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsSpecification")>]
    member _.ContributorInsightsSpecification
        (
            config: TableConfig,
            contributorInsights: ContributorInsightsSpecification
        ) =
        { config with
            ContributorInsightsSpecification = Some contributorInsights }

    /// <summary>Configures CloudWatch Contributor Insights for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="enabled">Whether to enable contributor insights.</param>
    /// <param name="mode">The contributor insights mode.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     contributorInsightsSpecification true ContributorInsightsMode.CONTRIBUTOR_INSIGHTS
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsSpecification")>]
    member _.ContributorInsightsSpecification(config: TableConfig, enabled: bool, mode: ContributorInsightsMode) =
        { config with
            ContributorInsightsSpecification = Some(ContributorInsightsSpecification(Enabled = enabled, Mode = mode)) }

    /// <summary>Configures CloudWatch Contributor Insights for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="enabled">Whether to enable contributor insights.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     contributorInsightsEnabled true
    /// }
    /// </code>
    [<CustomOperation("contributorInsightsEnabled")>]
    member _.ContributorInsightsEnabled(config: TableConfig, enabled: bool) =
        { config with
            ContributorInsightsSpecification = Some(ContributorInsightsSpecification(Enabled = enabled)) }

    /// <summary>Adds a global secondary index to the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexes">The global secondary index specification.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///    partitionKey "id" AttributeType.STRING
    ///    globalSecondaryIndexes [ myGsi1; myGsi2 ]
    /// }
    /// </code>
    [<CustomOperation("globalSecondaryIndexes")>]
    member _.GlobalSecondaryIndexes(config: TableConfig, indexes: GlobalSecondaryIndexProps list) =
        { config with
            GlobalSecondaryIndexes = indexes }

    /// <summary>Adds a local secondary index to the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexes">The local secondary index specification.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///    partitionKey "id" AttributeType.STRING
    ///    localSecondaryIndexes [ myLsi1; myLsi2 ]
    /// }
    /// </code>
    [<CustomOperation("localSecondaryIndexes")>]
    member _.LocalSecondaryIndexes(config: TableConfig, indexes: LocalSecondaryIndexProps list) =
        { config with
            LocalSecondaryIndexes = indexes }

    /// <summary>Grants read data permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive read data permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantReadData lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantReadData")>]
    member _.GrantReadData(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantReadData(grantee)) }

    /// <summary>Grants full access permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive full access permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantFullAccess lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantFullAccess")>]
    member _.GrantFullAccess(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantFullAccess(grantee)) }

    /// <summary>Grants read and write data permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive read and write data permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantReadWriteData lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantReadWriteData")>]
    member _.GrantReadWriteData(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantReadWriteData(grantee)) }

    /// <summary>Grants write data permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive write data permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantWriteData lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantWriteData")>]
    member _.GrantWriteData(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantWriteData(grantee)) }

    /// <summary>Grants stream read permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive stream read permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantStreamRead lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantStreamRead")>]
    member _.GrantStreamRead(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantStreamRead(grantee)) }

    /// <summary>Grants stream permissions to the specified grantee with specific actions.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive stream permissions.</param>
    /// <param name="actions">The list of actions to grant.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantStream lambdaFunction [ "dynamodb:DescribeStream"; "dynamodb:GetRecords" ]
    /// }
    /// </code>
    [<CustomOperation("grantStream")>]
    member _.GrantStream(config: TableConfig, grantee: IGrantable, actions: string list) =
        { config with
            Grant = Some(GrantStream(grantee, actions)) }

    /// <summary>Grants table list streams permissions to the specified grantee.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive table list streams permissions.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grantTableListStreams lambdaFunction
    /// }
    /// </code>
    [<CustomOperation("grantTableListStreams")>]
    member _.GrantTableListStreams(config: TableConfig, grantee: IGrantable) =
        { config with
            Grant = Some(GrantTableListStreams(grantee)) }

    /// <summary>Grants custom permissions to the specified grantee with specific actions.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="grantee">The grantee to receive custom permissions.</param>
    /// <param name="actions">The list of actions to grant.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     grant lambdaFunction [ "dynamodb:CustomAction1"; "dynamodb:CustomAction2" ]
    /// }
    /// </code>
    [<CustomOperation("grant")>]
    member _.Grant(config: TableConfig, grantee: IGrantable, actions: string list) =
        { config with
            Grant = Some(Grant(grantee, actions)) }

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


    /// <summary>Creates global secondary indexes for a DynamoDB table.</summary>
    /// <param name="name">The index name.</param>
    /// <code lang="fsharp">
    /// globalSecondaryIndex "my-index" {
    ///     partitionKey "gsiPk" AttributeType.STRING
    ///     sortKey "gsiSk" AttributeType.NUMBER
    ///     projectionType ProjectionType.ALL
    /// }
    /// </code>
    let globalSecondaryIndex name = GlobalSecondaryIndexBuilder(name)

    /// <summary>Creates local secondary indexes for a DynamoDB table.</summary>
    /// <param name="name">The index name.</param>
    /// <code lang="fsharp">
    /// localSecondaryIndex "my-index" {
    ///     sortKey "lsiSk" AttributeType.NUMBER
    ///     projectionType ProjectionType.ALL
    ///     nonKeyAttributes [ "lsiNonKey1"; "lsiNonKey2" ]
    /// }
    /// </code>
    let localSecondaryIndex name = LocalSecondaryIndexBuilder(name)
