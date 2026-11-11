namespace FsCDK

open System.Collections.Generic
open System.Text.RegularExpressions
open Amazon.CDK
open Amazon.CDK.AWS.WAFv2
open Amazon.CDK.AWS.Logs

// ============================================================================
// WAFv2 Web ACL Configuration DSL
// ============================================================================

/// <summary>
/// Where the web ACL is used. CloudFront web ACLs must be created in us-east-1
/// (put them in a us-east-1 stack and pass the ARN to the distribution's `webAclId`).
/// Regional web ACLs protect ALBs, REST API stages, AppSync APIs, Cognito user pools and App Runner services.
/// API Gateway HTTP APIs cannot be associated with WAF; front them with CloudFront instead, keeping in mind
/// that the API's execute-api endpoint still accepts requests that bypass CloudFront.
/// </summary>
[<RequireQualifiedAccess>]
type WebAclScope =
    | Regional
    | CloudFront

    member this.Value =
        match this with
        | Regional -> "REGIONAL"
        | CloudFront -> "CLOUDFRONT"

[<RequireQualifiedAccess>]
type WebAclDefaultAction =
    | Allow
    | Block

/// <summary>A web ACL rule. Priorities follow the order in which rules are added.</summary>
type WafRule =
    /// Managed rule group. Rules in `countRules` are overridden to Count; `countOnly` counts the whole group.
    | ManagedRuleGroup of vendor: string * name: string * countRules: string list * countOnly: bool
    /// Blocks a client IP that sends more than `limit` requests within `windowSeconds` (60, 120, 300 or 600).
    | RateBasedRule of name: string * limit: int * windowSeconds: int
    /// Any rule. Its Priority is replaced with its position in the web ACL.
    | CustomRule of CfnWebACL.RuleProperty

/// <summary>Where WAF writes its logs.</summary>
type WafLogDestination =
    /// CloudWatch log group; its name must start with "aws-waf-logs-".
    | LogGroupDestination of ILogGroup
    /// A logGroup spec yielded earlier in the same stack.
    | LogGroupSpecDestination of CloudWatchLogGroupSpec
    /// S3 bucket or Kinesis Data Firehose ARN; the bucket or stream name must start with "aws-waf-logs-".
    | ArnDestination of string

[<RequireQualifiedAccess>]
type WafRedactedField =
    | Header of name: string
    | QueryString
    | UriPath
    | Method

type WebAclConfig =
    { AclName: string
      ConstructId: string option
      Description: string option
      Scope: WebAclScope option
      DefaultAction: WebAclDefaultAction option
      Rules: WafRule list
      MetricName: string option
      SampledRequests: bool option
      Associations: string list
      LogDestination: WafLogDestination option
      RedactedFields: WafRedactedField list }

type WebAclSpec =
    { AclName: string
      ConstructId: string
      Scope: WebAclScope
      Props: CfnWebACLProps
      Associations: string list
      LogDestination: WafLogDestination option
      RedactedFields: WafRedactedField list
      mutable WebAcl: CfnWebACL option }

    /// Gets the web ACL ARN (for CloudFront `webAclId`). Must be called after the stack is built.
    member this.Arn =
        match this.WebAcl with
        | Some acl -> acl.AttrArn
        | None ->
            failwith
                $"WebAcl '{this.AclName}' has not been created yet. Ensure it's yielded in a stack before referencing it."

module WafRules =
    /// AWS core rule set (OWASP Top 10 style protections), 700 WCU.
    let commonRuleSet =
        ManagedRuleGroup("AWS", "AWSManagedRulesCommonRuleSet", [], false)

    /// Known bad inputs (Log4j, invalid patterns), 200 WCU.
    let knownBadInputs =
        ManagedRuleGroup("AWS", "AWSManagedRulesKnownBadInputsRuleSet", [], false)

    /// Amazon IP reputation list, 25 WCU.
    let amazonIpReputationList =
        ManagedRuleGroup("AWS", "AWSManagedRulesAmazonIpReputationList", [], false)

    /// SQL injection rule set, 200 WCU.
    let sqlInjection = ManagedRuleGroup("AWS", "AWSManagedRulesSQLiRuleSet", [], false)

    /// Common, known-bad-inputs and IP reputation groups (925 WCU of the default 1500).
    let recommended = [ commonRuleSet; knownBadInputs; amazonIpReputationList ]

    /// An AWS managed rule group by name.
    let managed (name: string) =
        ManagedRuleGroup("AWS", name, [], false)

    /// Per-IP rate limit over a 5 minute window.
    let rateLimit (limit: int) =
        RateBasedRule("RateLimitPerIp", limit, 300)

module WebAclOperations =

    let private emptyObject () = Dictionary<string, obj>() :> obj

    /// CloudWatch metric names allow letters, digits, hyphens and underscores.
    let metricName (name: string) =
        Regex.Replace(name, "[^A-Za-z0-9_-]", "_")

    let visibility (metric: string) (sampled: bool) =
        CfnWebACL.VisibilityConfigProperty(
            CloudWatchMetricsEnabled = true,
            MetricName = metricName metric,
            SampledRequestsEnabled = sampled
        )

    let toRuleProperty (sampled: bool) (priority: int) (rule: WafRule) : CfnWebACL.RuleProperty =
        match rule with
        | ManagedRuleGroup(vendor, name, countRules, countOnly) ->
            let statement =
                CfnWebACL.ManagedRuleGroupStatementProperty(VendorName = vendor, Name = name)

            if not countRules.IsEmpty then
                statement.RuleActionOverrides <-
                    countRules
                    |> List.map (fun r ->
                        CfnWebACL.RuleActionOverrideProperty(
                            Name = r,
                            ActionToUse = CfnWebACL.RuleActionProperty(Count = CfnWebACL.CountActionProperty())
                        ))
                    |> Array.ofList

            let overrideAction = CfnWebACL.OverrideActionProperty()

            if countOnly then
                overrideAction.Count <- emptyObject ()
            else
                overrideAction.None <- emptyObject ()

            CfnWebACL.RuleProperty(
                Name = name,
                Priority = float priority,
                Statement = CfnWebACL.StatementProperty(ManagedRuleGroupStatement = statement),
                OverrideAction = overrideAction,
                VisibilityConfig = visibility name sampled
            )

        | RateBasedRule(name, limit, window) ->
            CfnWebACL.RuleProperty(
                Name = name,
                Priority = float priority,
                Statement =
                    CfnWebACL.StatementProperty(
                        RateBasedStatement =
                            CfnWebACL.RateBasedStatementProperty(
                                Limit = float limit,
                                AggregateKeyType = "IP",
                                EvaluationWindowSec = float window
                            )
                    ),
                Action = CfnWebACL.RuleActionProperty(Block = CfnWebACL.BlockActionProperty()),
                VisibilityConfig = visibility name sampled
            )

        | CustomRule rule ->
            rule.Priority <- float priority
            rule

    let private redactedField (field: WafRedactedField) =
        match field with
        | WafRedactedField.Header name ->
            // SingleHeader is raw JSON in the L1 type, so a SingleHeaderProperty would render as {"name": ...}
            let header = Dictionary<string, obj>()
            header.["Name"] <- name
            CfnLoggingConfiguration.FieldToMatchProperty(SingleHeader = header)
        | WafRedactedField.QueryString -> CfnLoggingConfiguration.FieldToMatchProperty(QueryString = emptyObject ())
        | WafRedactedField.UriPath -> CfnLoggingConfiguration.FieldToMatchProperty(UriPath = emptyObject ())
        | WafRedactedField.Method -> CfnLoggingConfiguration.FieldToMatchProperty(Method = emptyObject ())

    /// Creates the web ACL, its regional associations and its logging configuration.
    let create (stack: Stack) (spec: WebAclSpec) : unit =
        if
            spec.Scope = WebAclScope.CloudFront
            && not (Token.IsUnresolved(stack.Region))
            && stack.Region <> "us-east-1"
        then
            failwith
                $"WebAcl '{spec.AclName}': CloudFront web ACLs must be created in us-east-1 (stack '{stack.StackName}' is in {stack.Region}). Use a us-east-1 stack with crossRegionReferences."

        let acl = CfnWebACL(stack, spec.ConstructId, spec.Props)

        spec.Associations
        |> List.iteri (fun i arn ->
            CfnWebACLAssociation(
                stack,
                $"{spec.ConstructId}Association{i}",
                CfnWebACLAssociationProps(ResourceArn = arn, WebAclArn = acl.AttrArn)
            )
            |> ignore)

        let logGroupArn (lg: ILogGroup) =
            // LogGroupName is a Ref token for log groups in this app; the physical name is on the CfnLogGroup
            let physicalName =
                match lg.Node.DefaultChild with
                | :? CfnLogGroup as cfn -> cfn.LogGroupName
                | _ -> lg.LogGroupName

            if
                isNull physicalName
                || (not (Token.IsUnresolved(physicalName))
                    && not (physicalName.StartsWith "aws-waf-logs-"))
            then
                failwith
                    $"WebAcl '{spec.AclName}': WAF log group names must start with 'aws-waf-logs-' (got '{physicalName}')."

            // WAF expects the log group ARN without the ':*' suffix of ILogGroup.LogGroupArn
            Stack
                .Of(lg)
                .FormatArn(
                    ArnComponents(
                        Service = "logs",
                        Resource = "log-group",
                        ResourceName = lg.LogGroupName,
                        ArnFormat = ArnFormat.COLON_RESOURCE_NAME
                    )
                )

        spec.LogDestination
        |> Option.iter (fun destination ->
            let destinationArn =
                match destination with
                | ArnDestination arn -> arn
                | LogGroupDestination lg -> logGroupArn lg
                | LogGroupSpecDestination lgSpec ->
                    match lgSpec.LogGroup with
                    | Some lg -> logGroupArn lg
                    | None ->
                        failwith
                            $"WebAcl '{spec.AclName}': log group '{lgSpec.LogGroupName}' must be yielded in the stack before the web ACL."

            let props =
                CfnLoggingConfigurationProps(ResourceArn = acl.AttrArn, LogDestinationConfigs = [| destinationArn |])

            if not spec.RedactedFields.IsEmpty then
                props.RedactedFields <- spec.RedactedFields |> List.map redactedField |> Array.ofList

            CfnLoggingConfiguration(stack, $"{spec.ConstructId}Logging", props) |> ignore)

        spec.WebAcl <- Some acl

type WebAclBuilder(name: string) =
    member _.Yield(_: unit) : WebAclConfig =
        { AclName = name
          ConstructId = None
          Description = None
          Scope = None
          DefaultAction = None
          Rules = []
          MetricName = None
          SampledRequests = None
          Associations = []
          LogDestination = None
          RedactedFields = [] }

    member x.Yield(rule: WafRule) : WebAclConfig = { x.Yield(()) with Rules = [ rule ] }

    member x.Zero() : WebAclConfig = x.Yield(())

    member inline _.Delay([<InlineIfLambda>] f: unit -> WebAclConfig) : WebAclConfig = f ()

    member _.Combine(state1: WebAclConfig, state2: WebAclConfig) : WebAclConfig =
        { AclName = state1.AclName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          Scope = state2.Scope |> Option.orElse state1.Scope
          DefaultAction = state2.DefaultAction |> Option.orElse state1.DefaultAction
          Rules = state1.Rules @ state2.Rules
          MetricName = state2.MetricName |> Option.orElse state1.MetricName
          SampledRequests = state2.SampledRequests |> Option.orElse state1.SampledRequests
          Associations = state1.Associations @ state2.Associations
          LogDestination = state2.LogDestination |> Option.orElse state1.LogDestination
          RedactedFields = state1.RedactedFields @ state2.RedactedFields }

    member inline x.For(config: WebAclConfig, [<InlineIfLambda>] f: unit -> WebAclConfig) : WebAclConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: WebAclConfig) : WebAclSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.AclName
        let scope = config.Scope |> Option.defaultValue WebAclScope.Regional
        let sampled = config.SampledRequests |> Option.defaultValue true

        if scope = WebAclScope.CloudFront && not config.Associations.IsEmpty then
            invalidArg
                "associate"
                $"WebAcl '{config.AclName}': CloudFront web ACLs are attached with the distribution's webAclId, not associate."

        // HTTP API (API Gateway v2) stage ARNs look like arn:aws:apigateway:region::/apis/ID/stages/NAME
        match config.Associations |> List.tryFind (fun arn -> arn.Contains "::/apis/") with
        | Some arn ->
            invalidArg
                "associate"
                $"WebAcl '{config.AclName}': WAF cannot be associated with HTTP APIs ({arn}). Put CloudFront with a CloudFront web ACL in front of the API."
        | None -> ()

        let names =
            config.Rules
            |> List.map (function
                | ManagedRuleGroup(_, n, _, _) -> n
                | RateBasedRule(n, _, _) -> n
                | CustomRule r -> r.Name)

        match names |> List.countBy id |> List.tryFind (fun (_, c) -> c > 1) with
        | Some(n, _) -> invalidArg "rules" $"WebAcl '{config.AclName}': rule name '{n}' is used more than once."
        | None -> ()

        match
            names
            |> List.tryFind (fun n -> isNull n || not (Regex.IsMatch(n, "^[A-Za-z0-9_-]{1,128}$")))
        with
        | Some n ->
            invalidArg
                "rules"
                $"WebAcl '{config.AclName}': rule name '{n}' must be 1-128 letters, digits, hyphens or underscores."
        | None -> ()

        for rule in config.Rules do
            match rule with
            | RateBasedRule(n, limit, window) ->
                if limit < 10 || limit > 2000000000 then
                    invalidArg "rateLimit" $"WebAcl '{config.AclName}': rule '{n}' limit must be 10..2000000000."

                if not (List.contains window [ 60; 120; 300; 600 ]) then
                    invalidArg "rateLimit" $"WebAcl '{config.AclName}': rule '{n}' window must be 60, 120, 300 or 600."
            | _ -> ()

        let defaultAction =
            match config.DefaultAction |> Option.defaultValue WebAclDefaultAction.Allow with
            | WebAclDefaultAction.Allow -> CfnWebACL.DefaultActionProperty(Allow = CfnWebACL.AllowActionProperty())
            | WebAclDefaultAction.Block -> CfnWebACL.DefaultActionProperty(Block = CfnWebACL.BlockActionProperty())

        let props =
            CfnWebACLProps(
                Scope = scope.Value,
                DefaultAction = defaultAction,
                VisibilityConfig =
                    WebAclOperations.visibility (config.MetricName |> Option.defaultValue config.AclName) sampled,
                Rules =
                    (config.Rules
                     |> List.mapi (WebAclOperations.toRuleProperty sampled)
                     |> Array.ofList)
            )

        config.Description |> Option.iter (fun d -> props.Description <- d)

        // Tokens and session cookies stay out of WAF logs unless redactedFields says otherwise.
        let redacted =
            if config.RedactedFields.IsEmpty then
                [ WafRedactedField.Header "authorization"; WafRedactedField.Header "cookie" ]
            else
                config.RedactedFields |> List.distinct

        { AclName = config.AclName
          ConstructId = constructId
          Scope = scope
          Props = props
          Associations = config.Associations
          LogDestination = config.LogDestination
          RedactedFields = redacted
          WebAcl = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: WebAclConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: WebAclConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets the scope (default: Regional). CloudFront web ACLs must be deployed in us-east-1.</summary>
    /// <param name="scope">WebAclScope.Regional or WebAclScope.CloudFront.</param>
    /// <code lang="fsharp">
    /// webAcl "EdgeAcl" {
    ///     scope WebAclScope.CloudFront
    /// }
    /// </code>
    [<CustomOperation("scope")>]
    member _.Scope(config: WebAclConfig, scope: WebAclScope) = { config with Scope = Some scope }

    /// <summary>Sets the action for requests no rule matched (default: Allow).</summary>
    [<CustomOperation("defaultAction")>]
    member _.DefaultAction(config: WebAclConfig, action: WebAclDefaultAction) =
        { config with
            DefaultAction = Some action }

    /// <summary>Adds an AWS managed rule group.</summary>
    /// <param name="groupName">E.g. "AWSManagedRulesCommonRuleSet".</param>
    /// <code lang="fsharp">
    /// webAcl "Acl" {
    ///     managedRuleGroup "AWSManagedRulesCommonRuleSet"
    /// }
    /// </code>
    [<CustomOperation("managedRuleGroup")>]
    member _.ManagedRuleGroup(config: WebAclConfig, groupName: string) =
        { config with
            Rules = config.Rules @ [ ManagedRuleGroup("AWS", groupName, [], false) ] }

    /// <summary>Adds an AWS managed rule group with some of its rules overridden to Count.</summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="countRules">Rules to count instead of block, e.g. [ "SizeRestrictions_BODY" ].</param>
    [<CustomOperation("managedRuleGroup")>]
    member _.ManagedRuleGroup(config: WebAclConfig, groupName: string, countRules: string list) =
        { config with
            Rules = config.Rules @ [ ManagedRuleGroup("AWS", groupName, countRules, false) ] }

    /// <summary>Adds the recommended AWS managed groups: common rule set, known bad inputs and IP reputation.</summary>
    [<CustomOperation("recommendedRules")>]
    member _.RecommendedRules(config: WebAclConfig) =
        { config with
            Rules = config.Rules @ WafRules.recommended }

    /// <summary>Blocks client IPs that exceed `limit` requests per 5 minutes.</summary>
    /// <param name="limit">Requests per 5 minutes per IP (10..2000000000).</param>
    /// <code lang="fsharp">
    /// webAcl "Acl" {
    ///     rateLimit 1000
    /// }
    /// </code>
    [<CustomOperation("rateLimit")>]
    member _.RateLimit(config: WebAclConfig, limit: int) =
        { config with
            Rules = config.Rules @ [ WafRules.rateLimit limit ] }

    /// <summary>Adds a named per-IP rate limit with an evaluation window.</summary>
    /// <param name="ruleName">Rule name.</param>
    /// <param name="limit">Requests per window per IP.</param>
    /// <param name="windowSeconds">60, 120, 300 or 600.</param>
    [<CustomOperation("rateLimit")>]
    member _.RateLimit(config: WebAclConfig, ruleName: string, limit: int, windowSeconds: int) =
        { config with
            Rules = config.Rules @ [ RateBasedRule(ruleName, limit, windowSeconds) ] }

    /// <summary>Adds a rule.</summary>
    /// <param name="rule">A WafRule.</param>
    [<CustomOperation("rule")>]
    member _.Rule(config: WebAclConfig, rule: WafRule) =
        { config with
            Rules = config.Rules @ [ rule ] }

    /// <summary>Adds a raw CloudFormation rule; its priority becomes its position.</summary>
    /// <param name="rule">The rule.</param>
    [<CustomOperation("rule")>]
    member _.Rule(config: WebAclConfig, rule: CfnWebACL.RuleProperty) =
        { config with
            Rules = config.Rules @ [ CustomRule rule ] }

    /// <summary>Sets the web ACL CloudWatch metric name (default: the ACL name).</summary>
    [<CustomOperation("metricName")>]
    member _.MetricName(config: WebAclConfig, metricName: string) =
        { config with
            MetricName = Some metricName }

    /// <summary>Enables or disables sampled requests (default: enabled). Samples include request headers.</summary>
    [<CustomOperation("sampledRequests")>]
    member _.SampledRequests(config: WebAclConfig, enabled: bool) =
        { config with
            SampledRequests = Some enabled }

    /// <summary>Associates the regional web ACL with a resource (ALB, REST API stage, AppSync, Cognito user pool, App Runner).</summary>
    /// <param name="resourceArn">The resource ARN.</param>
    /// <code lang="fsharp">
    /// webAcl "Acl" {
    ///     associate restApiStageArn
    /// }
    /// </code>
    [<CustomOperation("associate")>]
    member _.Associate(config: WebAclConfig, resourceArn: string) =
        { config with
            Associations = config.Associations @ [ resourceArn ] }

    /// <summary>Sends WAF logs to a CloudWatch log group whose name starts with "aws-waf-logs-".</summary>
    /// <param name="logGroup">The log group (same region as the web ACL).</param>
    /// <code lang="fsharp">
    /// let! wafLogs = logGroup "aws-waf-logs-app" { retention RetentionDays.ONE_YEAR }
    /// webAcl "Acl" {
    ///     logTo wafLogs
    /// }
    /// </code>
    [<CustomOperation("logTo")>]
    member _.LogTo(config: WebAclConfig, logGroup: ILogGroup) =
        { config with
            LogDestination = Some(LogGroupDestination logGroup) }

    /// <summary>Sends WAF logs to a logGroup spec; yield the spec in the same stack before the web ACL.</summary>
    /// <param name="logGroup">The log group spec (name must start with "aws-waf-logs-").</param>
    /// <code lang="fsharp">
    /// let wafLogs = logGroup "aws-waf-logs-edge" { retention RetentionDays.ONE_YEAR }
    /// let edgeAcl = webAcl "EdgeAcl" { scope WebAclScope.CloudFront; logTo wafLogs }
    /// stack "Edge" {
    ///     wafLogs
    ///     edgeAcl
    /// }
    /// </code>
    [<CustomOperation("logTo")>]
    member _.LogTo(config: WebAclConfig, logGroup: CloudWatchLogGroupSpec) =
        { config with
            LogDestination = Some(LogGroupSpecDestination logGroup) }

    /// <summary>Sends WAF logs to an S3 bucket or Firehose stream ARN (name must start with "aws-waf-logs-").</summary>
    /// <param name="destinationArn">The destination ARN.</param>
    [<CustomOperation("logTo")>]
    member _.LogTo(config: WebAclConfig, destinationArn: string) =
        { config with
            LogDestination = Some(ArnDestination destinationArn) }

    /// <summary>
    /// Redacts a field from WAF logs. Without any redactedField the authorization and cookie headers are redacted;
    /// once used, only the listed fields are redacted.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <code lang="fsharp">
    /// webAcl "Acl" {
    ///     logTo wafLogs
    ///     redactedField (WafRedactedField.Header "authorization")
    ///     redactedField WafRedactedField.QueryString
    /// }
    /// </code>
    [<CustomOperation("redactedField")>]
    member _.RedactedField(config: WebAclConfig, field: WafRedactedField) =
        { config with
            RedactedFields = config.RedactedFields @ [ field ] }

[<AutoOpen>]
module WebAclBuilders =
    /// <summary>Creates a WAFv2 web ACL.</summary>
    /// <param name="name">The web ACL name.</param>
    /// <code lang="fsharp">
    /// webAcl "ApiAcl" {
    ///     recommendedRules
    ///     rateLimit 2000
    ///     associate albArn
    /// }
    /// </code>
    let webAcl (name: string) = WebAclBuilder name
