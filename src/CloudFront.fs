namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.CloudFront.Origins

// ============================================================================
// CloudFront Distribution Configuration DSL
// ============================================================================

type DistributionConfig =
    { DistributionName: string
      ConstructId: string option
      DefaultBehavior: IBehaviorOptions option
      DefaultRootObject: string option
      Comment: string option
      Enabled: bool option
      PriceClass: PriceClass option
      HttpVersion: HttpVersion option
      MinimumProtocolVersion: SecurityPolicyProtocol option
      Certificate: Amazon.CDK.AWS.CertificateManager.ICertificate option
      DomainNames: string list
      EnableIpv6: bool option
      EnableLogging: bool option
      LogBucket: IBucket option
      LogFilePrefix: string option
      LogIncludesCookies: bool option
      GeoRestriction: GeoRestriction option
      WebAclId: string option
      AdditionalBehaviors: Map<string, IBehaviorOptions> }

type DistributionSpec =
    { DistributionName: string
      ConstructId: string
      Props: DistributionProps }

type DistributionBuilder(name: string) =
    member _.Yield _ : DistributionConfig =
        { DistributionName = name
          ConstructId = None
          DefaultBehavior = None
          DefaultRootObject = None
          Comment = None
          Enabled = None
          PriceClass = None
          HttpVersion = None
          MinimumProtocolVersion = None
          Certificate = None
          DomainNames = []
          EnableIpv6 = None
          EnableLogging = None
          LogBucket = None
          LogFilePrefix = None
          LogIncludesCookies = None
          GeoRestriction = None
          WebAclId = None
          AdditionalBehaviors = Map.empty }

    member _.Zero() : DistributionConfig =
        { DistributionName = name
          ConstructId = None
          DefaultBehavior = None
          DefaultRootObject = None
          Comment = None
          Enabled = None
          PriceClass = None
          HttpVersion = None
          MinimumProtocolVersion = None
          Certificate = None
          DomainNames = []
          EnableIpv6 = None
          EnableLogging = None
          LogBucket = None
          LogFilePrefix = None
          LogIncludesCookies = None
          GeoRestriction = None
          WebAclId = None
          AdditionalBehaviors = Map.empty }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DistributionConfig) : DistributionConfig = f ()

    member inline x.For(config: DistributionConfig, [<InlineIfLambda>] f: unit -> DistributionConfig) : DistributionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: DistributionConfig, state2: DistributionConfig) : DistributionConfig =
        { DistributionName = state1.DistributionName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          DefaultBehavior =
            if state1.DefaultBehavior.IsSome then
                state1.DefaultBehavior
            else
                state2.DefaultBehavior
          DefaultRootObject =
            if state1.DefaultRootObject.IsSome then
                state1.DefaultRootObject
            else
                state2.DefaultRootObject
          Comment =
            if state1.Comment.IsSome then
                state1.Comment
            else
                state2.Comment
          Enabled =
            if state1.Enabled.IsSome then
                state1.Enabled
            else
                state2.Enabled
          PriceClass =
            if state1.PriceClass.IsSome then
                state1.PriceClass
            else
                state2.PriceClass
          HttpVersion =
            if state1.HttpVersion.IsSome then
                state1.HttpVersion
            else
                state2.HttpVersion
          MinimumProtocolVersion =
            if state1.MinimumProtocolVersion.IsSome then
                state1.MinimumProtocolVersion
            else
                state2.MinimumProtocolVersion
          Certificate =
            if state1.Certificate.IsSome then
                state1.Certificate
            else
                state2.Certificate
          DomainNames = state1.DomainNames @ state2.DomainNames
          EnableIpv6 =
            if state1.EnableIpv6.IsSome then
                state1.EnableIpv6
            else
                state2.EnableIpv6
          EnableLogging =
            if state1.EnableLogging.IsSome then
                state1.EnableLogging
            else
                state2.EnableLogging
          LogBucket =
            if state1.LogBucket.IsSome then
                state1.LogBucket
            else
                state2.LogBucket
          LogFilePrefix =
            if state1.LogFilePrefix.IsSome then
                state1.LogFilePrefix
            else
                state2.LogFilePrefix
          LogIncludesCookies =
            if state1.LogIncludesCookies.IsSome then
                state1.LogIncludesCookies
            else
                state2.LogIncludesCookies
          GeoRestriction =
            if state1.GeoRestriction.IsSome then
                state1.GeoRestriction
            else
                state2.GeoRestriction
          WebAclId =
            if state1.WebAclId.IsSome then
                state1.WebAclId
            else
                state2.WebAclId
          AdditionalBehaviors =
            Map.fold (fun acc key value -> Map.add key value acc) state1.AdditionalBehaviors state2.AdditionalBehaviors }

    member _.Run(config: DistributionConfig) : DistributionSpec =
        let props = DistributionProps()
        let constructId = config.ConstructId |> Option.defaultValue config.DistributionName

        // Default behavior is required
        props.DefaultBehavior <-
            match config.DefaultBehavior with
            | Some behavior -> behavior
            | None -> failwith "Default behavior is required for CloudFront Distribution"

        // AWS Best Practice: Enable the distribution by default
        props.Enabled <- config.Enabled |> Option.defaultValue true

        // AWS Best Practice: Use HTTP/2 for better performance
        props.HttpVersion <- config.HttpVersion |> Option.defaultValue HttpVersion.HTTP2

        // AWS Best Practice: Use TLS 1.2 as minimum
        props.MinimumProtocolVersion <-
            config.MinimumProtocolVersion
            |> Option.defaultValue SecurityPolicyProtocol.TLS_V1_2_2021

        // AWS Best Practice: Use PriceClass100 for cost optimization (US, Canada, Europe)
        // Users should explicitly choose higher price classes if global distribution is needed
        props.PriceClass <- config.PriceClass |> Option.defaultValue PriceClass.PRICE_CLASS_100

        // AWS Best Practice: Enable IPv6 by default
        props.EnableIpv6 <- config.EnableIpv6 |> Option.defaultValue true

        config.DefaultRootObject
        |> Option.iter (fun obj -> props.DefaultRootObject <- obj)

        config.Comment |> Option.iter (fun c -> props.Comment <- c)

        config.Certificate
        |> Option.iter (fun cert -> props.Certificate <- cert)

        if not (List.isEmpty config.DomainNames) then
            props.DomainNames <- config.DomainNames |> List.toArray

        config.EnableLogging
        |> Option.iter (fun e -> props.EnableLogging <- e)

        config.LogBucket |> Option.iter (fun b -> props.LogBucket <- b)

        config.LogFilePrefix
        |> Option.iter (fun p -> props.LogFilePrefix <- p)

        config.LogIncludesCookies
        |> Option.iter (fun c -> props.LogIncludesCookies <- c)

        config.GeoRestriction
        |> Option.iter (fun g -> props.GeoRestriction <- g)

        config.WebAclId |> Option.iter (fun w -> props.WebAclId <- w)

        if not (Map.isEmpty config.AdditionalBehaviors) then
            let behaviorDict = System.Collections.Generic.Dictionary<string, IBehaviorOptions>()

            for KeyValue(path, behavior) in config.AdditionalBehaviors do
                behaviorDict.Add(path, behavior)

            props.AdditionalBehaviors <- behaviorDict

        { DistributionName = config.DistributionName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the distribution.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DistributionConfig, id: string) =
        { config with ConstructId = Some id }

    /// <summary>Sets the default behavior.</summary>
    [<CustomOperation("defaultBehavior")>]
    member _.DefaultBehavior(config: DistributionConfig, behavior: IBehaviorOptions) =
        { config with
            DefaultBehavior = Some behavior }

    /// <summary>Sets the default root object (e.g., "index.html").</summary>
    [<CustomOperation("defaultRootObject")>]
    member _.DefaultRootObject(config: DistributionConfig, obj: string) =
        { config with
            DefaultRootObject = Some obj }

    /// <summary>Sets the comment for the distribution.</summary>
    [<CustomOperation("comment")>]
    member _.Comment(config: DistributionConfig, comment: string) =
        { config with Comment = Some comment }

    /// <summary>Enables or disables the distribution.</summary>
    [<CustomOperation("enabled")>]
    member _.Enabled(config: DistributionConfig, enabled: bool) =
        { config with Enabled = Some enabled }

    /// <summary>Sets the price class.</summary>
    [<CustomOperation("priceClass")>]
    member _.PriceClass(config: DistributionConfig, priceClass: PriceClass) =
        { config with
            PriceClass = Some priceClass }

    /// <summary>Sets the HTTP version.</summary>
    [<CustomOperation("httpVersion")>]
    member _.HttpVersion(config: DistributionConfig, version: HttpVersion) =
        { config with
            HttpVersion = Some version }

    /// <summary>Sets the minimum protocol version.</summary>
    [<CustomOperation("minimumProtocolVersion")>]
    member _.MinimumProtocolVersion(config: DistributionConfig, version: SecurityPolicyProtocol) =
        { config with
            MinimumProtocolVersion = Some version }

    /// <summary>Adds a domain name.</summary>
    [<CustomOperation("domainName")>]
    member _.DomainName(config: DistributionConfig, domain: string) =
        { config with
            DomainNames = domain :: config.DomainNames }

    /// <summary>Enables or disables IPv6.</summary>
    [<CustomOperation("enableIpv6")>]
    member _.EnableIpv6(config: DistributionConfig, enabled: bool) =
        { config with EnableIpv6 = Some enabled }

    /// <summary>Enables logging.</summary>
    [<CustomOperation("enableLogging")>]
    member _.EnableLogging(config: DistributionConfig, bucket: IBucket, ?prefix: string) =
        { config with
            EnableLogging = Some true
            LogBucket = Some bucket
            LogFilePrefix = prefix }

    /// <summary>Sets the WAF web ACL ID.</summary>
    [<CustomOperation("webAclId")>]
    member _.WebAclId(config: DistributionConfig, aclId: string) =
        { config with WebAclId = Some aclId }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CloudFrontBuilders =
    /// <summary>Creates a CloudFront distribution with AWS best practices.</summary>
    /// <param name="name">The distribution name.</param>
    /// <code lang="fsharp">
    /// cloudFrontDistribution "MyCDN" {
    ///     s3Origin myBucket
    ///     defaultRootObject "index.html"
    ///     priceClass PriceClass.PRICE_CLASS_100
    /// }
    /// </code>
    let cloudFrontDistribution (name: string) = DistributionBuilder(name)
