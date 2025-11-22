namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.AppSync
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Cognito

// ============================================================================
// AppSync Configuration - Types must be defined before Refs
// ============================================================================

/// <summary>
/// High-level AWS AppSync (GraphQL API) builder following AWS best practices.
///
/// **Default Settings:**
/// - AuthorizationMode = API_KEY (for development)
/// - XrayEnabled = true (distributed tracing)
/// - LogLevel = ALL (comprehensive logging)
///
/// **Rationale:**
/// These defaults follow Yan Cui's GraphQL API recommendations:
/// - AppSync provides managed GraphQL with subscriptions
/// - Built-in caching and offline support
/// - Better than API Gateway for complex mobile/web apps
/// - X-Ray tracing for debugging
///
/// **Use Cases:**
/// - Mobile applications (iOS, Android)
/// - Real-time web applications
/// - Offline-first applications
/// - Complex data fetching requirements
/// - GraphQL subscriptions
///
/// **Escape Hatch:**
/// Access the underlying CDK GraphqlApi via the `GraphqlApi` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type AppSyncApiConfig =
    { ApiName: string
      ConstructId: string option
      SchemaDefinition: SchemaFile option
      XrayEnabled: bool voption
      LogLevel: FieldLogLevel voption }

type AppSyncApiSpec =
    { ApiName: string
      ConstructId: string
      Props: GraphqlApiProps
      mutable GraphqlApi: GraphqlApi option }

    /// Gets the API ID
    member this.ApiId =
        match this.GraphqlApi with
        | Some api -> api.ApiId
        | None -> null

    /// Gets the API ARN
    member this.Arn =
        match this.GraphqlApi with
        | Some api -> api.Arn
        | None -> null

    /// Gets the GraphQL endpoint
    member this.GraphqlUrl =
        match this.GraphqlApi with
        | Some api -> api.GraphqlUrl
        | None -> null

type AppSyncApiBuilder(name: string) =
    member _.Yield(_: unit) : AppSyncApiConfig =
        { ApiName = name
          ConstructId = None
          SchemaDefinition = None
          XrayEnabled = ValueSome true
          LogLevel = ValueSome FieldLogLevel.ALL }

    member _.Zero() : AppSyncApiConfig =
        { ApiName = name
          ConstructId = None
          SchemaDefinition = None
          XrayEnabled = ValueSome true
          LogLevel = ValueSome FieldLogLevel.ALL }

    member _.Combine(state1: AppSyncApiConfig, state2: AppSyncApiConfig) : AppSyncApiConfig =
        { ApiName = state2.ApiName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          SchemaDefinition = state2.SchemaDefinition |> Option.orElse state1.SchemaDefinition
          XrayEnabled = state2.XrayEnabled |> ValueOption.orElse state1.XrayEnabled
          LogLevel = state2.LogLevel |> ValueOption.orElse state1.LogLevel }

    member inline _.Delay([<InlineIfLambda>] f: unit -> AppSyncApiConfig) : AppSyncApiConfig = f ()

    member inline x.For(config: AppSyncApiConfig, [<InlineIfLambda>] f: unit -> AppSyncApiConfig) : AppSyncApiConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: AppSyncApiConfig) : AppSyncApiSpec =
        let apiName = config.ApiName
        let constructId = config.ConstructId |> Option.defaultValue apiName

        let props = GraphqlApiProps()
        props.Name <- apiName

        match config.SchemaDefinition with
        | Some schema -> props.Definition <- Definition.FromSchema(schema)
        | None -> () // Schema can be added later via addType/addQuery/addMutation

        config.XrayEnabled
        |> ValueOption.iter (fun v -> props.XrayEnabled <- System.Nullable<bool>(v))

        let logConfig = LogConfig()
        logConfig.FieldLogLevel <- config.LogLevel |> ValueOption.defaultValue FieldLogLevel.ALL
        props.LogConfig <- logConfig

        { ApiName = apiName
          ConstructId = constructId
          Props = props
          GraphqlApi = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: AppSyncApiConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("schema")>]
    member _.Schema(config: AppSyncApiConfig, schemaFile: SchemaFile) =
        { config with
            SchemaDefinition = Some schemaFile }

    [<CustomOperation("schemaFromFile")>]
    member _.SchemaFromFile(config: AppSyncApiConfig, filePath: string) =
        { config with
            SchemaDefinition = Some(SchemaFile.FromAsset(filePath)) }

    [<CustomOperation("xrayEnabled")>]
    member _.XrayEnabled(config: AppSyncApiConfig, enabled: bool) =
        { config with
            XrayEnabled = ValueSome enabled }

    [<CustomOperation("logLevel")>]
    member _.LogLevel(config: AppSyncApiConfig, level: FieldLogLevel) =
        { config with
            LogLevel = ValueSome level }

/// AppSync Data Source Configuration
type AppSyncDataSourceConfig =
    { Name: string
      ConstructId: string option
      Api: GraphqlApi option
      DynamoDBTable: ITable option
      LambdaFunction: IFunction option
      Description: string option }

type AppSyncDataSourceSpec =
    { Name: string
      ConstructId: string
      Config: AppSyncDataSourceConfig
      mutable DataSource: BaseDataSource option }

type AppSyncDataSourceBuilder(name: string) =
    member _.Yield(_: unit) : AppSyncDataSourceConfig =
        { Name = name
          ConstructId = None
          Api = None
          DynamoDBTable = None
          LambdaFunction = None
          Description = None }

    member _.Zero() : AppSyncDataSourceConfig =
        { Name = name
          ConstructId = None
          Api = None
          DynamoDBTable = None
          LambdaFunction = None
          Description = None }

    member _.Combine(state1: AppSyncDataSourceConfig, state2: AppSyncDataSourceConfig) : AppSyncDataSourceConfig =
        { Name = state2.Name
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Api = state2.Api |> Option.orElse state1.Api
          DynamoDBTable = state2.DynamoDBTable |> Option.orElse state1.DynamoDBTable
          LambdaFunction = state2.LambdaFunction |> Option.orElse state1.LambdaFunction
          Description = state2.Description |> Option.orElse state1.Description }

    member inline _.Delay([<InlineIfLambda>] f: unit -> AppSyncDataSourceConfig) : AppSyncDataSourceConfig = f ()

    member inline x.For
        (
            config: AppSyncDataSourceConfig,
            [<InlineIfLambda>] f: unit -> AppSyncDataSourceConfig
        ) : AppSyncDataSourceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: AppSyncDataSourceConfig) : AppSyncDataSourceSpec =
        let name = config.Name
        let constructId = config.ConstructId |> Option.defaultValue name

        match config.Api with
        | None -> failwith "GraphQL API is required for AppSync Data Source"
        | Some _ -> ()

        { Name = name
          ConstructId = constructId
          Config = config
          DataSource = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: AppSyncDataSourceConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("api")>]
    member _.Api(config: AppSyncDataSourceConfig, api: GraphqlApi) = { config with Api = Some api }

    [<CustomOperation("api")>]
    member _.Api(config: AppSyncDataSourceConfig, apiSpec: AppSyncApiSpec) =
        match apiSpec.GraphqlApi with
        | Some api -> { config with Api = Some api }
        | None ->
            failwith "AppSync API has not been created yet. Ensure it's yielded in the stack before the data source."

    [<CustomOperation("dynamoDBTable")>]
    member _.DynamoDBTable(config: AppSyncDataSourceConfig, table: ITable) =
        { config with
            DynamoDBTable = Some table }

    [<CustomOperation("dynamoDBTable")>]
    member _.DynamoDBTable(config: AppSyncDataSourceConfig, tableSpec: TableSpec) =
        match tableSpec.Table with
        | Some table ->
            { config with
                DynamoDBTable = Some table }
        | None -> failwith "Table has not been created yet. Ensure it's yielded in the stack before the data source."

    [<CustomOperation("lambdaFunction")>]
    member _.LambdaFunction(config: AppSyncDataSourceConfig, func: IFunction) =
        { config with
            LambdaFunction = Some func }

    [<CustomOperation("lambdaFunction")>]
    member _.LambdaFunction(config: AppSyncDataSourceConfig, funcSpec: FunctionSpec) =
        match funcSpec.Function with
        | Some func ->
            { config with
                LambdaFunction = Some func }
        | None -> failwith "Function has not been created yet. Ensure it's yielded in the stack before the data source."

    [<CustomOperation("description")>]
    member _.Description(config: AppSyncDataSourceConfig, description: string) =
        { config with
            Description = Some description }

/// Helper functions for AppSync operations
module AppSyncHelpers =

    /// Creates a schema from a file path
    let schemaFromFile (filePath: string) = SchemaFile.FromAsset(filePath)

    /// Creates a schema from inline SDL string
    let schemaFromString (sdl: string) = SchemaFile.FromAsset(sdl)

    /// Common log levels
    module LogLevels =
        let none = FieldLogLevel.NONE
        let error = FieldLogLevel.ERROR
        let all = FieldLogLevel.ALL

    /// Sample GraphQL schema templates
    module SchemaTemplates =
        let basicSchema =
            """
type Query {
  getItem(id: ID!): Item
  listItems: [Item]
}

type Mutation {
  createItem(input: CreateItemInput!): Item
  updateItem(id: ID!, input: UpdateItemInput!): Item
  deleteItem(id: ID!): Item
}

type Item {
  id: ID!
  name: String!
  description: String
  createdAt: AWSDateTime
  updatedAt: AWSDateTime
}

input CreateItemInput {
  name: String!
  description: String
}

input UpdateItemInput {
  name: String
  description: String
}

schema {
  query: Query
  mutation: Mutation
}
"""

[<AutoOpen>]
module AppSyncBuilders =
    /// <summary>
    /// Creates a new AppSync GraphQL API builder.
    /// Example: appSyncApi "MyAPI" { schemaFromFile "schema.graphql" }
    /// Note: Authorization modes should be configured via the CDK GraphqlApi construct directly for now.
    /// </summary>
    let appSyncApi name = AppSyncApiBuilder name

    /// <summary>
    /// Creates a new AppSync Data Source builder.
    /// Example: appSyncDataSource "DynamoDB" { api myApi; dynamoDBTable myTable }
    /// </summary>
    let appSyncDataSource name = AppSyncDataSourceBuilder name
