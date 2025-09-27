namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB

// ============================================================================
// DynamoDB Table Configuration DSL
// ============================================================================

// Table configuration DSL
type TableConfig =
    { TableName: string option
      ConstructId: string option // Optional custom construct ID
      PartitionKey: (string * AttributeType) option
      SortKey: (string * AttributeType) option
      BillingMode: BillingMode option
      RemovalPolicy: RemovalPolicy option
      PointInTimeRecovery: bool option }

type TableSpec =
    { TableName: string
      ConstructId: string // Construct ID for CDK
      Props: TableProps }

type TableBuilder() =
    member _.Yield _ : TableConfig =
        { TableName = None
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None }

    member _.Zero() : TableConfig =
        { TableName = None
          ConstructId = None
          PartitionKey = None
          SortKey = None
          BillingMode = None
          RemovalPolicy = None
          PointInTimeRecovery = None }

    member _.Run(config: TableConfig) : TableSpec =
        // Table name is required
        let tableName =
            match config.TableName with
            | Some name -> name
            | None -> failwith "Table name is required"

        // Construct ID defaults to table name if not specified
        let constructId = config.ConstructId |> Option.defaultValue tableName

        let props = TableProps(TableName = tableName)

        // Set a partition key (required)
        match config.PartitionKey with
        | Some(name, attrType) -> props.PartitionKey <- Attribute(Name = name, Type = attrType)
        | None -> failwith "Partition key is required for DynamoDB table"

        // Set optional properties
        config.SortKey
        |> Option.iter (fun (name, attrType) -> props.SortKey <- Attribute(Name = name, Type = attrType))
        // Only set if explicitly configured
        config.BillingMode |> Option.iter (fun mode -> props.BillingMode <- mode)

        config.RemovalPolicy
        |> Option.iter (fun policy -> props.RemovalPolicy <- policy)

        // Note: PointInTimeRecoverySpecification needs the enabled flag set
        config.PointInTimeRecovery
        |> Option.iter (fun enabled ->
            if enabled then
                props.PointInTimeRecoverySpecification <-
                    PointInTimeRecoverySpecification(PointInTimeRecoveryEnabled = true))

        { TableName = tableName
          ConstructId = constructId
          Props = props }

    [<CustomOperation("name")>]
    member _.Name(config: TableConfig, tableName: string) =
        { config with
            TableName = Some tableName }

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
