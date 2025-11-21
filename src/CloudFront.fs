namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.CloudFront.Origins
open Amazon.CDK.AWS.S3

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

type S3OriginType =
    | StaticWebsiteOrigin of bucket: IBucket
    | GenericOrigin of origin: IOrigin

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

    member inline x.For
        (
            config: DistributionConfig,
            [<InlineIfLambda>] f: unit -> DistributionConfig
        ) : DistributionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: DistributionConfig, b: DistributionConfig) : DistributionConfig =
        { DistributionName = a.DistributionName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          DefaultBehavior =
            match a.DefaultBehavior with
            | Some _ -> a.DefaultBehavior
            | None -> b.DefaultBehavior
          DefaultRootObject =
            match a.DefaultRootObject with
            | Some _ -> a.DefaultRootObject
            | None -> b.DefaultRootObject
          Comment =
            match a.Comment with
            | Some _ -> a.Comment
            | None -> b.Comment
          Enabled =
            match a.Enabled with
            | Some _ -> a.Enabled
            | None -> b.Enabled
          PriceClass =
            match a.PriceClass with
            | Some _ -> a.PriceClass
            | None -> b.PriceClass
          HttpVersion =
            match a.HttpVersion with
            | Some _ -> a.HttpVersion
            | None -> b.HttpVersion
          MinimumProtocolVersion =
            match a.MinimumProtocolVersion with
            | Some _ -> a.MinimumProtocolVersion
            | None -> b.MinimumProtocolVersion
          Certificate =
            match a.Certificate with
            | Some _ -> a.Certificate
            | None -> b.Certificate
          // preserve definition order; reverse on Run for user order
          DomainNames = a.DomainNames @ b.DomainNames
          EnableIpv6 =
            match a.EnableIpv6 with
            | Some _ -> a.EnableIpv6
            | None -> b.EnableIpv6
          EnableLogging =
            match a.EnableLogging with
            | Some _ -> a.EnableLogging
            | None -> b.EnableLogging
          LogBucket =
            match a.LogBucket with
            | Some _ -> a.LogBucket
            | None -> b.LogBucket
          LogFilePrefix =
            match a.LogFilePrefix with
            | Some _ -> a.LogFilePrefix
            | None -> b.LogFilePrefix
          LogIncludesCookies =
            match a.LogIncludesCookies with
            | Some _ -> a.LogIncludesCookies
            | None -> b.LogIncludesCookies
          GeoRestriction =
            match a.GeoRestriction with
            | Some _ -> a.GeoRestriction
            | None -> b.GeoRestriction
          WebAclId =
            match a.WebAclId with
            | Some _ -> a.WebAclId
            | None -> b.WebAclId
          AdditionalBehaviors =
            Map.fold (fun acc key value -> Map.add key value acc) a.AdditionalBehaviors b.AdditionalBehaviors }

    member _.Run(config: DistributionConfig) : DistributionSpec =
        let props = DistributionProps()
        let constructId = config.ConstructId |> Option.defaultValue config.DistributionName

        // Default behavior is required
        props.DefaultBehavior <-
            match config.DefaultBehavior with
            | Some behavior -> behavior
            | None -> invalidArg "defaultBehavior" "Default behavior is required for CloudFront Distribution"

        // AWS Best Practice: Enable the distribution by default
        props.Enabled <- config.Enabled |> Option.defaultValue true
        props.HttpVersion <- config.HttpVersion |> Option.defaultValue HttpVersion.HTTP2_AND_3

        props.MinimumProtocolVersion <-
            config.MinimumProtocolVersion
            |> Option.defaultValue SecurityPolicyProtocol.TLS_V1_2_2021

        // AWS Best Practice: Use PriceClass100 for cost optimization (US, Canada, Europe)
        // Users should explicitly choose higher price classes if global distribution is needed
        props.PriceClass <- config.PriceClass |> Option.defaultValue PriceClass.PRICE_CLASS_100

        // AWS Best Practice: Enable IPv6 by default
        props.EnableIpv6 <- config.EnableIpv6 |> Option.defaultValue true

        // Optionals
        config.DefaultRootObject |> Option.iter (fun v -> props.DefaultRootObject <- v)
        config.Comment |> Option.iter (fun v -> props.Comment <- v)
        config.Certificate |> Option.iter (fun v -> props.Certificate <- v)

        if not (List.isEmpty config.DomainNames) then
            // Reverse to preserve call order (domainName "a"; domainName "b" => ["a"; "b"])
            props.DomainNames <- config.DomainNames |> List.rev |> List.toArray

        config.EnableLogging |> Option.iter (fun v -> props.EnableLogging <- v)

        config.LogBucket |> Option.iter (fun v -> props.LogBucket <- v)

        config.LogFilePrefix |> Option.iter (fun v -> props.LogFilePrefix <- v)

        config.LogIncludesCookies
        |> Option.iter (fun v -> props.LogIncludesCookies <- v)

        config.GeoRestriction |> Option.iter (fun v -> props.GeoRestriction <- v)
        config.WebAclId |> Option.iter (fun v -> props.WebAclId <- v)

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
    member _.ConstructId(config: DistributionConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the default behavior from a pre-built IBehaviorOptions.</summary>
    [<CustomOperation("defaultBehavior")>]
    member _.DefaultBehavior(config: DistributionConfig, behavior: IBehaviorOptions) =
        { config with
            DefaultBehavior = Some behavior }

    /// <summary>
    /// Convenience: default S3 origin behavior with common best-practice defaults.
    /// Defaults:
    /// - ViewerProtocolPolicy = REDIRECT_TO_HTTPS
    /// - CachePolicy = CachePolicy.CACHING_OPTIMIZED
    /// - OriginRequestPolicy = OriginRequestPolicy.CORS_S3_ORIGIN
    /// - Compress = true
    /// You can override any default via optional parameters.
    /// </summary>
    [<CustomOperation("s3DefaultBehavior")>]
    member _.S3DefaultBehavior
        (
            config: DistributionConfig,
            originType: S3OriginType,
            ?viewerProtocolPolicy: ViewerProtocolPolicy,
            ?cachePolicy: ICachePolicy,
            ?originRequestPolicy: IOriginRequestPolicy,
            ?responseHeadersPolicy: ResponseHeadersPolicy,
            ?allowedMethods: AllowedMethods,
            ?cachedMethods: CachedMethods,
            ?compress: bool
        ) =
        let behavior = BehaviorOptions()

        let origin =
            match originType with
            | S3OriginType.StaticWebsiteOrigin bucket -> S3StaticWebsiteOrigin bucket :> IOrigin
            | S3OriginType.GenericOrigin origin -> origin

        behavior.Origin <- origin
        behavior.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        behavior.CachePolicy <- defaultArg cachePolicy (CachePolicy.CACHING_OPTIMIZED)
        behavior.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.CORS_S3_ORIGIN

        responseHeadersPolicy
        |> Option.iter (fun p -> behavior.ResponseHeadersPolicy <- p)

        allowedMethods |> Option.iter (fun m -> behavior.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> behavior.CachedMethods <- m)
        behavior.Compress <- defaultArg compress true

        { config with
            DefaultBehavior = Some behavior }

    /// <summary>
    /// Convenience: default HTTP(S) origin behavior (e.g., ALB/API) with common defaults.
    /// Defaults:
    /// - ViewerProtocolPolicy = REDIRECT_TO_HTTPS
    /// - CachePolicy = CachePolicy.CACHING_OPTIMIZED (override for dynamic APIs)
    /// - OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER
    /// - Compress = true
    /// </summary>
    [<CustomOperation("httpDefaultBehavior")>]
    member _.HttpDefaultBehavior
        (
            config: DistributionConfig,
            domainName: string,
            ?originPath: string,
            ?viewerProtocolPolicy: ViewerProtocolPolicy,
            ?cachePolicy: ICachePolicy,
            ?originRequestPolicy: IOriginRequestPolicy,
            ?responseHeadersPolicy: ResponseHeadersPolicy,
            ?allowedMethods: AllowedMethods,
            ?cachedMethods: CachedMethods,
            ?compress: bool
        ) =
        let origin =
            match originPath with
            | Some p ->
                let props = HttpOriginProps()
                props.OriginPath <- p
                HttpOrigin(domainName, props) :> IOrigin
            | None -> HttpOrigin(domainName) :> IOrigin

        let behavior = BehaviorOptions()
        behavior.Origin <- origin
        behavior.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        behavior.CachePolicy <- defaultArg cachePolicy CachePolicy.CACHING_OPTIMIZED
        behavior.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.ALL_VIEWER

        responseHeadersPolicy
        |> Option.iter (fun p -> behavior.ResponseHeadersPolicy <- p)

        allowedMethods |> Option.iter (fun m -> behavior.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> behavior.CachedMethods <- m)
        behavior.Compress <- defaultArg compress true

        { config with
            DefaultBehavior = Some behavior }

    /// <summary>Add an additional behavior for a path pattern.</summary>
    [<CustomOperation("additionalBehavior")>]
    member _.AdditionalBehavior(config: DistributionConfig, pathPattern: string, behavior: IBehaviorOptions) =
        { config with
            AdditionalBehaviors = config.AdditionalBehaviors |> Map.add pathPattern behavior }

    /// <summary>
    /// Convenience: adds S3 behavior at a path pattern.
    /// Same defaults as s3DefaultBehavior unless overridden.
    /// </summary>
    [<CustomOperation("additionalS3Behavior")>]
    member _.AdditionalS3Behavior
        (
            config: DistributionConfig,
            pathPattern: string,
            originType: S3OriginType,
            ?viewerProtocolPolicy: ViewerProtocolPolicy,
            ?cachePolicy: ICachePolicy,
            ?originRequestPolicy: IOriginRequestPolicy,
            ?responseHeadersPolicy: ResponseHeadersPolicy,
            ?allowedMethods: AllowedMethods,
            ?cachedMethods: CachedMethods,
            ?compress: bool
        ) =
        let behavior = BehaviorOptions()

        let origin =
            match originType with
            | S3OriginType.StaticWebsiteOrigin bucket -> S3StaticWebsiteOrigin bucket :> IOrigin
            | S3OriginType.GenericOrigin origin -> origin

        behavior.Origin <- origin
        behavior.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        behavior.CachePolicy <- defaultArg cachePolicy CachePolicy.CACHING_OPTIMIZED
        behavior.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.CORS_S3_ORIGIN

        responseHeadersPolicy
        |> Option.iter (fun p -> behavior.ResponseHeadersPolicy <- p)

        allowedMethods |> Option.iter (fun m -> behavior.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> behavior.CachedMethods <- m)
        behavior.Compress <- defaultArg compress true

        { config with
            AdditionalBehaviors = config.AdditionalBehaviors |> Map.add pathPattern behavior }

    /// <summary>
    /// Convenience: adds an additional HTTP(S) behavior at a path pattern.
    /// Same defaults as httpDefaultBehavior unless overridden.
    /// </summary>
    [<CustomOperation("additionalHttpBehavior")>]
    member _.AdditionalHttpBehavior
        (
            config: DistributionConfig,
            pathPattern: string,
            domainName: string,
            ?originPath: string,
            ?viewerProtocolPolicy: ViewerProtocolPolicy,
            ?cachePolicy: ICachePolicy,
            ?originRequestPolicy: IOriginRequestPolicy,
            ?responseHeadersPolicy: ResponseHeadersPolicy,
            ?allowedMethods: AllowedMethods,
            ?cachedMethods: CachedMethods,
            ?compress: bool
        ) =
        let origin =
            match originPath with
            | Some p ->
                let props = HttpOriginProps()
                props.OriginPath <- p
                HttpOrigin(domainName, props) :> IOrigin
            | None -> HttpOrigin(domainName) :> IOrigin

        let behavior = BehaviorOptions()
        behavior.Origin <- origin
        behavior.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        behavior.CachePolicy <- defaultArg cachePolicy CachePolicy.CACHING_OPTIMIZED
        behavior.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.ALL_VIEWER

        responseHeadersPolicy
        |> Option.iter (fun p -> behavior.ResponseHeadersPolicy <- p)

        allowedMethods |> Option.iter (fun m -> behavior.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> behavior.CachedMethods <- m)
        behavior.Compress <- defaultArg compress true

        { config with
            AdditionalBehaviors = config.AdditionalBehaviors |> Map.add pathPattern behavior }

    /// <summary>Adds a domain name (call multiple times to add several).</summary>
    [<CustomOperation("domainName")>]
    member _.DomainName(config: DistributionConfig, domain: string) =
        { config with
            DomainNames = domain :: config.DomainNames }

    /// <summary>Sets an ACM certificate for the distribution.</summary>
    [<CustomOperation("certificate")>]
    member _.Certificate(config: DistributionConfig, certificate: Amazon.CDK.AWS.CertificateManager.ICertificate) =
        { config with
            Certificate = Some certificate }

    /// <summary>Sets the default root object (e.g., "index.html").</summary>
    [<CustomOperation("defaultRootObject")>]
    member _.DefaultRootObject(config: DistributionConfig, obj: string) =
        { config with
            DefaultRootObject = Some obj }

    /// <summary>Sets the comment for the distribution.</summary>
    [<CustomOperation("comment")>]
    member _.Comment(config: DistributionConfig, comment: string) = { config with Comment = Some comment }

    /// <summary>Enables or disables the distribution.</summary>
    [<CustomOperation("enabled")>]
    member _.Enabled(config: DistributionConfig, enabled: bool) = { config with Enabled = Some enabled }

    /// <summary>Sets the price class.</summary>
    [<CustomOperation("priceClass")>]
    member _.PriceClass(config: DistributionConfig, priceClass: PriceClass) =
        { config with
            PriceClass = Some priceClass }

    /// <summary>Sets the HTTP version preference.</summary>
    [<CustomOperation("httpVersion")>]
    member _.HttpVersion(config: DistributionConfig, version: HttpVersion) =
        { config with
            HttpVersion = Some version }

    /// <summary>Sets the minimum TLS protocol version.</summary>
    [<CustomOperation("minimumProtocolVersion")>]
    member _.MinimumProtocolVersion(config: DistributionConfig, version: SecurityPolicyProtocol) =
        { config with
            MinimumProtocolVersion = Some version }

    /// <summary>Enables or disables IPv6.</summary>
    [<CustomOperation("enableIpv6")>]
    member _.EnableIpv6(config: DistributionConfig, enabled: bool) =
        { config with
            EnableIpv6 = Some enabled }

    /// <summary>Enables logging to an S3 bucket (optionally with a prefix and cookies flag).</summary>
    [<CustomOperation("enableLogging")>]
    member _.EnableLogging(config: DistributionConfig, bucket: IBucket, ?prefix: string, ?includeCookies: bool) =
        { config with
            EnableLogging = Some true
            LogBucket = Some(bucket)
            LogFilePrefix = prefix
            LogIncludesCookies = includeCookies }

    /// <summary>Sets the associated WAF web ACL ID.</summary>
    [<CustomOperation("webAclId")>]
    member _.WebAclId(config: DistributionConfig, aclId: string) = { config with WebAclId = Some aclId }

// ============================================================================
// CloudFront Origin Access Identity Configuration DSL
// ============================================================================

/// <summary>
/// High-level CloudFront Origin Access Identity (OAI) builder.
///
/// **Use Case:**
/// OAI allows CloudFront to access private S3 buckets without making them public.
/// This is a security best practice for serving static content.
///
/// **Note:**
/// AWS recommends using Origin Access Control (OAC) instead of OAI for new applications.
/// OAI is maintained for backward compatibility.
///
/// **Escape Hatch:**
/// Access the underlying CDK OriginAccessIdentity via the `Identity` property.
/// </summary>
type OriginAccessIdentityConfig =
    { IdentityName: string
      ConstructId: string option
      Comment: string option }

type OriginAccessIdentitySpec =
    { IdentityName: string
      ConstructId: string
      Props: OriginAccessIdentityProps
      mutable Identity: IOriginAccessIdentity option }

type OriginAccessIdentityBuilder(name: string) =
    member _.Yield _ : OriginAccessIdentityConfig =
        { IdentityName = name
          ConstructId = None
          Comment = None }

    member _.Zero() : OriginAccessIdentityConfig =
        { IdentityName = name
          ConstructId = None
          Comment = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> OriginAccessIdentityConfig) : OriginAccessIdentityConfig = f ()

    member _.Combine(a: OriginAccessIdentityConfig, b: OriginAccessIdentityConfig) : OriginAccessIdentityConfig =
        { IdentityName = a.IdentityName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Comment =
            match a.Comment with
            | Some _ -> a.Comment
            | None -> b.Comment }

    member _.Run(config: OriginAccessIdentityConfig) : OriginAccessIdentitySpec =
        let props = OriginAccessIdentityProps()
        let constructId = config.ConstructId |> Option.defaultValue config.IdentityName

        config.Comment |> Option.iter (fun c -> props.Comment <- c)

        { IdentityName = config.IdentityName
          ConstructId = constructId
          Props = props
          Identity = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: OriginAccessIdentityConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets a comment for the OAI.</summary>
    [<CustomOperation("comment")>]
    member _.Comment(config: OriginAccessIdentityConfig, comment: string) = { config with Comment = Some comment }

// ============================================================================
// Helper modules (kept inside CloudFront.fs as requested)
// ============================================================================

[<AutoOpen>]
module CloudFrontBuilders =
    /// <summary>Creates a CloudFront distribution with AWS best practices.</summary>
    /// <param name="name">The distribution name.</param>
    /// <remarks>
    /// Example:
    /// cloudFrontDistribution "MyCDN" {
    ///     s3DefaultBehavior myBucket
    ///     defaultRootObject "index.html"
    ///     domainName "static.example.com"
    ///     priceClass PriceClass.PRICE_CLASS_100
    /// }
    /// </remarks>
    let cloudFrontDistribution (name: string) = DistributionBuilder name

    /// <summary>Creates a CloudFront Origin Access Identity.</summary>
    /// <param name="name">The OAI name.</param>
    /// <code lang="fsharp">
    /// originAccessIdentity "MyOAI" {
    ///     comment "Access to private S3 bucket"
    /// }
    /// </code>
    let originAccessIdentity (name: string) = OriginAccessIdentityBuilder name

/// <summary>
/// Factory helpers to build common IBehaviorOptions for S3 and HTTP origins.
/// These helpers are useful if you prefer to construct behaviors and pass them via defaultBehavior/additionalBehavior.
/// </summary>
module CloudFrontBehaviors =

    let s3Behavior
        (originType: S3OriginType)
        (viewerProtocolPolicy: ViewerProtocolPolicy option)
        (cachePolicy: ICachePolicy option)
        (originRequestPolicy: IOriginRequestPolicy option)
        (responseHeadersPolicy: ResponseHeadersPolicy option)
        (allowedMethods: AllowedMethods option)
        (cachedMethods: CachedMethods option)
        (compress: bool option)
        : IBehaviorOptions =

        let b = BehaviorOptions()

        b.Origin <-
            match originType with
            | S3OriginType.StaticWebsiteOrigin bucket -> S3StaticWebsiteOrigin bucket :> IOrigin
            | S3OriginType.GenericOrigin origin -> origin

        b.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        b.CachePolicy <- defaultArg cachePolicy CachePolicy.CACHING_OPTIMIZED
        b.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.CORS_S3_ORIGIN
        responseHeadersPolicy |> Option.iter (fun p -> b.ResponseHeadersPolicy <- p)
        allowedMethods |> Option.iter (fun m -> b.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> b.CachedMethods <- m)
        b.Compress <- defaultArg compress true
        b :> IBehaviorOptions

    let s3BehaviorDefault (originType: S3OriginType) =
        s3Behavior originType None None None None None None None

    let httpBehavior
        (domainName: string)
        (originPath: string option)
        (viewerProtocolPolicy: ViewerProtocolPolicy option)
        (cachePolicy: ICachePolicy option)
        (originRequestPolicy: IOriginRequestPolicy option)
        (responseHeadersPolicy: ResponseHeadersPolicy option)
        (allowedMethods: AllowedMethods option)
        (cachedMethods: CachedMethods option)
        (compress: bool option)
        : IBehaviorOptions =

        let origin =
            match originPath with
            | Some p ->
                let props = HttpOriginProps()
                props.OriginPath <- p
                HttpOrigin(domainName, props) :> IOrigin
            | None -> HttpOrigin(domainName) :> IOrigin

        let b = BehaviorOptions()
        b.Origin <- origin
        b.ViewerProtocolPolicy <- defaultArg viewerProtocolPolicy ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        b.CachePolicy <- defaultArg cachePolicy CachePolicy.CACHING_OPTIMIZED
        b.OriginRequestPolicy <- defaultArg originRequestPolicy OriginRequestPolicy.ALL_VIEWER
        responseHeadersPolicy |> Option.iter (fun p -> b.ResponseHeadersPolicy <- p)
        allowedMethods |> Option.iter (fun m -> b.AllowedMethods <- m)
        cachedMethods |> Option.iter (fun m -> b.CachedMethods <- m)
        b.Compress <- defaultArg compress true
        b :> IBehaviorOptions

    let httpBehaviorDefault (domainName: string) =
        httpBehavior domainName None None None None None None None
