namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.S3

// ============================================================================
// ImportSourceSpecification Builder DSL
// ============================================================================

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

type GlobalSecondaryIndexConfig =
    { IndexName: string
      PartitionKey: string * AttributeType
      SortKey: (string * AttributeType) option
      ProjectionType: ProjectionType option
      NonKeyAttributes: string list option
      ReadCapacity: float option
      WriteCapacity: float option }

type LocalSecondaryIndexConfig =
    { IndexName: string
      SortKey: string * AttributeType
      ProjectionType: ProjectionType option
      NonKeyAttributes: string list option }


// ============================================================================
// DynamoDB Table Configuration DSL
// ============================================================================

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
      GlobalSecondaryIndexes: GlobalSecondaryIndexConfig list
      LocalSecondaryIndexes: LocalSecondaryIndexConfig list
      TableClass: TableClass option
      ContributorInsightsEnabled: bool option
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
      GlobalSecondaryIndexes: GlobalSecondaryIndexConfig list
      LocalSecondaryIndexes: LocalSecondaryIndexConfig list
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
          PointInTimeRecovery = Some true // Security: Enable point-in-time recovery for data protection
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsEnabled = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED // Security: Always encrypt at rest
          EncryptionKey = None
          Grant = None }

    member _.Yield(spec: IImportSourceSpecification) : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true // Security: Enable point-in-time recovery for data protection
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsEnabled = None
          ImportSource = Some spec
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED // Security: Always encrypt at rest
          EncryptionKey = None
          Grant = None }

    member _.Zero() : TableConfig =
        { TableName = name
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = Some true // Security: Enable point-in-time recovery for data protection
          TimeToLiveAttribute = None
          GlobalSecondaryIndexes = []
          LocalSecondaryIndexes = []
          TableClass = None
          ContributorInsightsEnabled = None
          ImportSource = None
          Stream = None
          KinesisStream = None
          Encryption = Some TableEncryption.AWS_MANAGED // Security: Always encrypt at rest
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
            ContributorInsightsEnabled =
                config2.ContributorInsightsEnabled
                |> Option.orElse config1.ContributorInsightsEnabled
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

        // Production defaults: PAY_PER_REQUEST billing mode (Alex DeBrie / Rick Houlihan best practice)
        props.BillingMode <- config.BillingMode |> Option.defaultValue BillingMode.PAY_PER_REQUEST

        config.RemovalPolicy
        |> Option.iter (fun policy -> props.RemovalPolicy <- System.Nullable<RemovalPolicy>(policy))

        // Production defaults: Point-in-time recovery enabled (Alex DeBrie / Rick Houlihan best practice)
        let pitrEnabled = config.PointInTimeRecovery |> Option.defaultValue true

        if pitrEnabled then
            props.PointInTimeRecoverySpecification <-
                PointInTimeRecoverySpecification(PointInTimeRecoveryEnabled = true)

        // Time-to-live attribute (use TimeToLiveAttribute property)
        config.TimeToLiveAttribute
        |> Option.iter (fun attr -> props.TimeToLiveAttribute <- attr)

        // Table class
        config.TableClass |> Option.iter (fun tc -> props.TableClass <- tc)

        // Contributor insights
        config.ContributorInsightsEnabled
        |> Option.iter (fun enabled ->
            props.ContributorInsightsSpecification <- ContributorInsightsSpecification(Enabled = enabled))

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

    /// <summary>Adds a Global Secondary Index (GSI) to the table. Essential for Alex DeBrie's single-table design.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexName">The name of the GSI.</param>
    /// <param name="partitionKey">The GSI partition key (attribute name and type).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "pk" AttributeType.STRING
    ///     sortKey "sk" AttributeType.STRING
    ///     globalSecondaryIndex "GSI1" ("gsi1pk", AttributeType.STRING)
    /// }
    /// </code>
    [<CustomOperation("globalSecondaryIndex")>]
    member _.GlobalSecondaryIndex(config: TableConfig, indexName: string, partitionKey: string * AttributeType) =
        let gsi =
            { IndexName = indexName
              PartitionKey = partitionKey
              SortKey = None
              ProjectionType = None
              NonKeyAttributes = None
              ReadCapacity = None
              WriteCapacity = None }

        { config with
            GlobalSecondaryIndexes = config.GlobalSecondaryIndexes @ [ gsi ] }

    /// <summary>Adds a Global Secondary Index with a sort key.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexName">The name of the GSI.</param>
    /// <param name="partitionKey">The GSI partition key (attribute name and type).</param>
    /// <param name="sortKey">The GSI sort key (attribute name and type).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "pk" AttributeType.STRING
    ///     sortKey "sk" AttributeType.STRING
    ///     globalSecondaryIndexWithSort "GSI1" ("gsi1pk", AttributeType.STRING) ("gsi1sk", AttributeType.STRING)
    /// }
    /// </code>
    [<CustomOperation("globalSecondaryIndexWithSort")>]
    member _.GlobalSecondaryIndexWithSort
        (
            config: TableConfig,
            indexName: string,
            partitionKey: string * AttributeType,
            sortKey: string * AttributeType
        ) =
        let gsi =
            { IndexName = indexName
              PartitionKey = partitionKey
              SortKey = Some sortKey
              ProjectionType = None
              NonKeyAttributes = None
              ReadCapacity = None
              WriteCapacity = None }

        { config with
            GlobalSecondaryIndexes = config.GlobalSecondaryIndexes @ [ gsi ] }

    /// <summary>Adds a Global Secondary Index with custom projection.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexName">The name of the GSI.</param>
    /// <param name="partitionKey">The GSI partition key (attribute name and type).</param>
    /// <param name="sortKey">The GSI sort key (optional).</param>
    /// <param name="projectionType">The projection type (ALL, KEYS_ONLY, or INCLUDE).</param>
    /// <param name="nonKeyAttributes">Additional attributes to include (for INCLUDE projection).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "pk" AttributeType.STRING
    ///     globalSecondaryIndexWithProjection "GSI1" ("gsi1pk", AttributeType.STRING) None ProjectionType.KEYS_ONLY []
    /// }
    /// </code>
    [<CustomOperation("globalSecondaryIndexWithProjection")>]
    member _.GlobalSecondaryIndexWithProjection
        (
            config: TableConfig,
            indexName: string,
            partitionKey: string * AttributeType,
            sortKey: (string * AttributeType) option,
            projectionType: ProjectionType,
            nonKeyAttributes: string list
        ) =
        let gsi =
            { IndexName = indexName
              PartitionKey = partitionKey
              SortKey = sortKey
              ProjectionType = Some projectionType
              NonKeyAttributes =
                if List.isEmpty nonKeyAttributes then
                    None
                else
                    Some nonKeyAttributes
              ReadCapacity = None
              WriteCapacity = None }

        { config with
            GlobalSecondaryIndexes = config.GlobalSecondaryIndexes @ [ gsi ] }

    /// <summary>Adds a Local Secondary Index (LSI) to the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexName">The name of the LSI.</param>
    /// <param name="sortKey">The LSI sort key (attribute name and type).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "pk" AttributeType.STRING
    ///     sortKey "sk" AttributeType.STRING
    ///     localSecondaryIndex "LSI1" ("lsi1sk", AttributeType.NUMBER)
    /// }
    /// </code>
    [<CustomOperation("localSecondaryIndex")>]
    member _.LocalSecondaryIndex(config: TableConfig, indexName: string, sortKey: string * AttributeType) =
        let lsi =
            { IndexName = indexName
              SortKey = sortKey
              ProjectionType = None
              NonKeyAttributes = None }

        { config with
            LocalSecondaryIndexes = config.LocalSecondaryIndexes @ [ lsi ] }

    /// <summary>Adds a Local Secondary Index with custom projection.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="indexName">The name of the LSI.</param>
    /// <param name="sortKey">The LSI sort key (attribute name and type).</param>
    /// <param name="projectionType">The projection type (ALL, KEYS_ONLY, or INCLUDE).</param>
    /// <param name="nonKeyAttributes">Additional attributes to include (for INCLUDE projection).</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "pk" AttributeType.STRING
    ///     localSecondaryIndexWithProjection "LSI1" ("lsi1sk", AttributeType.NUMBER) ProjectionType.ALL []
    /// }
    /// </code>
    [<CustomOperation("localSecondaryIndexWithProjection")>]
    member _.LocalSecondaryIndexWithProjection
        (
            config: TableConfig,
            indexName: string,
            sortKey: string * AttributeType,
            projectionType: ProjectionType,
            nonKeyAttributes: string list
        ) =
        let lsi =
            { IndexName = indexName
              SortKey = sortKey
              ProjectionType = Some projectionType
              NonKeyAttributes =
                if List.isEmpty nonKeyAttributes then
                    None
                else
                    Some nonKeyAttributes }

        { config with
            LocalSecondaryIndexes = config.LocalSecondaryIndexes @ [ lsi ] }

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

    /// <summary>Enables or disables CloudWatch Contributor Insights for the table.</summary>
    /// <param name="config">The current table configuration.</param>
    /// <param name="enabled">Whether to enable contributor insights.</param>
    /// <code lang="fsharp">
    /// table "MyTable" {
    ///     partitionKey "id" AttributeType.STRING
    ///     contributorInsights true
    /// }
    /// </code>
    [<CustomOperation("contributorInsights")>]
    member _.ContributorInsights(config: TableConfig, enabled: bool) =
        { config with
            ContributorInsightsEnabled = Some enabled }

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
