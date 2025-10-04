namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.S3

// ============================================================================
// S3 Bucket Configuration DSL
// ============================================================================

// Configuration for S3 Bucket builder
// Keeps it minimal and aligned with the existing DSL style

type BucketConfig =
    { BucketName: string
      ConstructId: string option
      BlockPublicAccess: BlockPublicAccess option
      Encryption: BucketEncryption option
      EnforceSSL: bool option
      Versioned: bool option
      RemovalPolicy: RemovalPolicy option
      ServerAccessLogsBucket: IBucket option
      ServerAccessLogsPrefix: string option
      AutoDeleteObjects: bool option
      WebsiteIndexDocument: string option
      WebsiteErrorDocument: string option
      LifecycleRules: ILifecycleRule list
      Cors: ICorsRule list
      Metrics: IBucketMetrics list }

type BucketSpec =
    { BucketName: string
      ConstructId: string
      Props: BucketProps }

type BucketBuilder(name: string) =
    member _.Yield _ : BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = None
          Encryption = None
          EnforceSSL = None
          Versioned = None
          RemovalPolicy = None
          ServerAccessLogsBucket = None
          ServerAccessLogsPrefix = None
          AutoDeleteObjects = None
          WebsiteIndexDocument = None
          WebsiteErrorDocument = None
          LifecycleRules = []
          Cors = []
          Metrics = [] }

    member _.Yield(corsRule: ICorsRule) : BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = None
          Encryption = None
          EnforceSSL = None
          Versioned = None
          RemovalPolicy = None
          ServerAccessLogsBucket = None
          ServerAccessLogsPrefix = None
          AutoDeleteObjects = None
          WebsiteIndexDocument = None
          WebsiteErrorDocument = None
          LifecycleRules = []
          Cors = [ corsRule ]
          Metrics = [] }

    member _.Yield(lifecycleRule: ILifecycleRule) : BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = None
          Encryption = None
          EnforceSSL = None
          Versioned = None
          RemovalPolicy = None
          ServerAccessLogsBucket = None
          ServerAccessLogsPrefix = None
          AutoDeleteObjects = None
          WebsiteIndexDocument = None
          WebsiteErrorDocument = None
          LifecycleRules = [ lifecycleRule ]
          Cors = []
          Metrics = [] }

    member _.Yield(metrics: IBucketMetrics) : BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = None
          Encryption = None
          EnforceSSL = None
          Versioned = None
          RemovalPolicy = None
          ServerAccessLogsBucket = None
          ServerAccessLogsPrefix = None
          AutoDeleteObjects = None
          WebsiteIndexDocument = None
          WebsiteErrorDocument = None
          LifecycleRules = []
          Cors = []
          Metrics = [ metrics ] }

    member _.Zero() : BucketConfig =
        { BucketName = name
          ConstructId = None
          BlockPublicAccess = None
          Encryption = None
          EnforceSSL = None
          Versioned = None
          RemovalPolicy = None
          ServerAccessLogsBucket = None
          ServerAccessLogsPrefix = None
          AutoDeleteObjects = None
          WebsiteIndexDocument = None
          WebsiteErrorDocument = None
          LifecycleRules = []
          Cors = []
          Metrics = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> BucketConfig) : BucketConfig = f ()

    member _.Combine(state1: BucketConfig, state2: BucketConfig) : BucketConfig =
        { BucketName = state1.BucketName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          BlockPublicAccess = state2.BlockPublicAccess |> Option.orElse state1.BlockPublicAccess
          Encryption = state2.Encryption |> Option.orElse state1.Encryption
          EnforceSSL = state2.EnforceSSL |> Option.orElse state1.EnforceSSL
          Versioned = state2.Versioned |> Option.orElse state1.Versioned
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          ServerAccessLogsBucket = state2.ServerAccessLogsBucket |> Option.orElse state1.ServerAccessLogsBucket
          ServerAccessLogsPrefix = state2.ServerAccessLogsPrefix |> Option.orElse state1.ServerAccessLogsPrefix
          AutoDeleteObjects = state2.AutoDeleteObjects |> Option.orElse state1.AutoDeleteObjects
          WebsiteIndexDocument = state2.WebsiteIndexDocument |> Option.orElse state1.WebsiteIndexDocument
          WebsiteErrorDocument = state2.WebsiteErrorDocument |> Option.orElse state1.WebsiteErrorDocument
          LifecycleRules = state1.LifecycleRules @ state2.LifecycleRules
          Cors = state1.Cors @ state2.Cors
          Metrics = state1.Metrics @ state2.Metrics }

    member inline x.For(config: BucketConfig, [<InlineIfLambda>] f: unit -> BucketConfig) : BucketConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: BucketConfig) : BucketSpec =
        let bucketName = config.BucketName
        let constructId = config.ConstructId |> Option.defaultValue bucketName

        let props = BucketProps()
        props.BucketName <- bucketName

        config.BlockPublicAccess |> Option.iter (fun v -> props.BlockPublicAccess <- v)
        config.Encryption |> Option.iter (fun v -> props.Encryption <- v)
        config.EnforceSSL |> Option.iter (fun v -> props.EnforceSSL <- v)
        config.Versioned |> Option.iter (fun v -> props.Versioned <- v)

        config.RemovalPolicy
        |> Option.iter (fun v -> props.RemovalPolicy <- System.Nullable<RemovalPolicy>(v))

        config.ServerAccessLogsBucket
        |> Option.iter (fun v -> props.ServerAccessLogsBucket <- v)

        config.ServerAccessLogsPrefix
        |> Option.iter (fun v -> props.ServerAccessLogsPrefix <- v)

        config.AutoDeleteObjects |> Option.iter (fun v -> props.AutoDeleteObjects <- v)

        config.WebsiteIndexDocument
        |> Option.iter (fun v -> props.WebsiteIndexDocument <- v)

        config.WebsiteErrorDocument
        |> Option.iter (fun v -> props.WebsiteErrorDocument <- v)

        if not (List.isEmpty config.LifecycleRules) then
            props.LifecycleRules <- (config.LifecycleRules |> List.toArray)

        if not (List.isEmpty config.Cors) then
            props.Cors <- (config.Cors |> List.toArray)

        if not (List.isEmpty config.Metrics) then
            props.Metrics <- (config.Metrics |> List.toArray)

        { BucketName = bucketName
          ConstructId = constructId
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: BucketConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("blockPublicAccess")>]
    member _.BlockPublicAccess(config: BucketConfig, value: BlockPublicAccess) =
        { config with
            BlockPublicAccess = Some value }

    [<CustomOperation("encryption")>]
    member _.Encryption(config: BucketConfig, value: BucketEncryption) = { config with Encryption = Some value }

    [<CustomOperation("enforceSSL")>]
    member _.EnforceSSL(config: BucketConfig, value: bool) = { config with EnforceSSL = Some value }

    [<CustomOperation("versioned")>]
    member _.Versioned(config: BucketConfig, value: bool) = { config with Versioned = Some value }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: BucketConfig, value: RemovalPolicy) =
        { config with
            RemovalPolicy = Some value }

    [<CustomOperation("serverAccessLogsBucket")>]
    member _.ServerAccessLogsBucket(config: BucketConfig, bucket: IBucket) =
        { config with
            ServerAccessLogsBucket = Some bucket }

    [<CustomOperation("serverAccessLogsPrefix")>]
    member _.ServerAccessLogsPrefix(config: BucketConfig, prefix: string) =
        { config with
            ServerAccessLogsPrefix = Some prefix }

    [<CustomOperation("autoDeleteObjects")>]
    member _.AutoDeleteObjects(config: BucketConfig, enabled: bool) =
        { config with
            AutoDeleteObjects = Some enabled }

    [<CustomOperation("websiteIndexDocument")>]
    member _.WebsiteIndexDocument(config: BucketConfig, doc: string) =
        { config with
            WebsiteIndexDocument = Some doc }

    [<CustomOperation("websiteErrorDocument")>]
    member _.WebsiteErrorDocument(config: BucketConfig, doc: string) =
        { config with
            WebsiteErrorDocument = Some doc }

// ============================================================================
// S3 CorsRule Builder DSL
// ============================================================================

type CorsRuleConfig =
    { AllowedMethods: HttpMethods list option
      AllowedOrigins: string list option
      AllowedHeaders: string list option
      ExposedHeaders: string list option
      Id: string option
      MaxAge: int option }

type CorsRuleBuilder() =
    member _.Yield _ : CorsRuleConfig =
        { AllowedMethods = None
          AllowedOrigins = None
          AllowedHeaders = None
          ExposedHeaders = None
          Id = None
          MaxAge = None }

    member _.Zero() : CorsRuleConfig =
        { AllowedMethods = None
          AllowedOrigins = None
          AllowedHeaders = None
          ExposedHeaders = None
          Id = None
          MaxAge = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CorsRuleConfig) : CorsRuleConfig = f ()

    member _.Combine(state1: CorsRuleConfig, state2: CorsRuleConfig) : CorsRuleConfig =
        { AllowedMethods = state2.AllowedMethods |> Option.orElse state1.AllowedMethods
          AllowedOrigins = state2.AllowedOrigins |> Option.orElse state1.AllowedOrigins
          AllowedHeaders = state2.AllowedHeaders |> Option.orElse state1.AllowedHeaders
          ExposedHeaders = state2.ExposedHeaders |> Option.orElse state1.ExposedHeaders
          Id = state2.Id |> Option.orElse state1.Id
          MaxAge = state2.MaxAge |> Option.orElse state1.MaxAge }

    member inline x.For(config: CorsRuleConfig, [<InlineIfLambda>] f: unit -> CorsRuleConfig) : CorsRuleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CorsRuleConfig) : ICorsRule =
        let rule = CorsRule()

        let methods =
            match config.AllowedMethods with
            | Some m -> m
            | None -> failwith "corsRule.allowedMethods is required"

        let origins =
            match config.AllowedOrigins with
            | Some o -> o
            | None -> failwith "corsRule.allowedOrigins is required"

        rule.AllowedMethods <- methods |> List.toArray
        rule.AllowedOrigins <- origins |> List.toArray

        config.AllowedHeaders
        |> Option.iter (fun h -> rule.AllowedHeaders <- (h |> List.toArray))

        config.ExposedHeaders
        |> Option.iter (fun h -> rule.ExposedHeaders <- (h |> List.toArray))

        config.Id |> Option.iter (fun i -> rule.Id <- i)
        config.MaxAge |> Option.iter (fun s -> rule.MaxAge <- float s)
        rule :> ICorsRule

    [<CustomOperation("allowedMethods")>]
    member _.AllowedMethods(config: CorsRuleConfig, methods: HttpMethods list) =
        { config with
            AllowedMethods = Some methods }

    [<CustomOperation("allowedOrigins")>]
    member _.AllowedOrigins(config: CorsRuleConfig, origins: string list) =
        { config with
            AllowedOrigins = Some origins }

    [<CustomOperation("allowedHeaders")>]
    member _.AllowedHeaders(config: CorsRuleConfig, headers: string list) =
        { config with
            AllowedHeaders = Some headers }

    [<CustomOperation("exposedHeaders")>]
    member _.ExposedHeaders(config: CorsRuleConfig, headers: string list) =
        { config with
            ExposedHeaders = Some headers }

    [<CustomOperation("id")>]
    member _.Id(config: CorsRuleConfig, id: string) = { config with Id = Some id }

    [<CustomOperation("maxAgeSeconds")>]
    member _.MaxAgeSeconds(config: CorsRuleConfig, seconds: int) = { config with MaxAge = Some seconds }
