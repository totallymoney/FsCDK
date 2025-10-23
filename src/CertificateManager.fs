namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.Route53

// ============================================================================
// Certificate Manager Configuration DSL
// ============================================================================

/// <summary>
/// High-level Certificate Manager builder following AWS security best practices.
///
/// **Default Security Settings:**
/// - Validation method = DNS (more secure than email validation)
/// - Key algorithm = RSA_2048 (industry standard)
/// - Transparency logging = enabled (default AWS behavior)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework security pillar:
/// - DNS validation is automated and doesn't rely on email
/// - RSA_2048 provides strong encryption with broad compatibility
/// - Certificate transparency helps detect mis-issuance
///
/// **Use Cases:**
/// - HTTPS for CloudFront distributions
/// - HTTPS for Application Load Balancers
/// - Custom domain names for API Gateway
///
/// **Escape Hatch:**
/// Access the underlying CDK Certificate via the `Certificate` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type CertificateConfig =
    { CertificateName: string
      ConstructId: string option
      DomainName: string option
      SubjectAlternativeNames: string list
      Validation: CertificateValidation option
      CertificateName_: string option
      KeyAlgorithm: KeyAlgorithm option }

type CertificateSpec =
    { CertificateName: string
      ConstructId: string
      Props: CertificateProps
      mutable Certificate: ICertificate option }

    /// Gets the underlying ICertificate resource. Must be called after the stack is built.
    member this.Resource =
        match this.Certificate with
        | Some cert -> cert
        | None ->
            failwith
                $"Certificate '{this.CertificateName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

    /// Gets the certificate ARN
    member this.Arn =
        match this.Certificate with
        | Some cert -> cert.CertificateArn
        | None ->
            failwith
                $"Certificate '{this.CertificateName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type CertificateRef =
    | CertificateInterface of ICertificate
    | CertificateSpecRef of CertificateSpec

module CertificateHelpers =
    let resolveCertificateRef (ref: CertificateRef) =
        match ref with
        | CertificateInterface cert -> cert
        | CertificateSpecRef spec ->
            match spec.Certificate with
            | Some cert -> cert
            | None ->
                failwith
                    $"Certificate '{spec.CertificateName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type CertificateBuilder(name: string) =
    member _.Yield _ : CertificateConfig =
        { CertificateName = name
          ConstructId = None
          DomainName = None
          SubjectAlternativeNames = []
          Validation = None
          CertificateName_ = None
          KeyAlgorithm = Some KeyAlgorithm.RSA_2048 }

    member _.Zero() : CertificateConfig =
        { CertificateName = name
          ConstructId = None
          DomainName = None
          SubjectAlternativeNames = []
          Validation = None
          CertificateName_ = None
          KeyAlgorithm = Some KeyAlgorithm.RSA_2048 }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CertificateConfig) : CertificateConfig = f ()

    member inline x.For
        (
            config: CertificateConfig,
            [<InlineIfLambda>] f: unit -> CertificateConfig
        ) : CertificateConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: CertificateConfig, b: CertificateConfig) : CertificateConfig =
        { CertificateName = a.CertificateName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          DomainName =
            match a.DomainName with
            | Some _ -> a.DomainName
            | None -> b.DomainName
          SubjectAlternativeNames = a.SubjectAlternativeNames @ b.SubjectAlternativeNames
          Validation =
            match a.Validation with
            | Some _ -> a.Validation
            | None -> b.Validation
          CertificateName_ =
            match a.CertificateName_ with
            | Some _ -> a.CertificateName_
            | None -> b.CertificateName_
          KeyAlgorithm =
            match a.KeyAlgorithm with
            | Some _ -> a.KeyAlgorithm
            | None -> b.KeyAlgorithm }

    member _.Run(config: CertificateConfig) : CertificateSpec =
        let props = CertificateProps()
        let constructId = config.ConstructId |> Option.defaultValue config.CertificateName

        // Domain name is required
        props.DomainName <-
            match config.DomainName with
            | Some domain -> domain
            | None -> invalidArg "domainName" "Domain name is required for Certificate"

        // AWS Best Practice: Use DNS validation by default (more secure and automated)
        props.Validation <- config.Validation |> Option.defaultValue (CertificateValidation.FromDns())

        // AWS Best Practice: RSA_2048 provides good security with broad compatibility
        props.KeyAlgorithm <- config.KeyAlgorithm |> Option.defaultValue KeyAlgorithm.RSA_2048

        if not (List.isEmpty config.SubjectAlternativeNames) then
            props.SubjectAlternativeNames <- config.SubjectAlternativeNames |> List.toArray

        config.CertificateName_ |> Option.iter (fun n -> props.CertificateName <- n)

        { CertificateName = config.CertificateName
          ConstructId = constructId
          Props = props
          Certificate = None }

    /// <summary>Sets the construct ID for the certificate.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CertificateConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the primary domain name for the certificate.</summary>
    /// <param name="domain">The domain name (e.g., "example.com").</param>
    [<CustomOperation("domainName")>]
    member _.DomainName(config: CertificateConfig, domain: string) =
        { config with DomainName = Some domain }

    /// <summary>Adds a subject alternative name (SAN).</summary>
    /// <param name="san">Additional domain name (e.g., "*.example.com", "www.example.com").</param>
    [<CustomOperation("subjectAlternativeName")>]
    member _.SubjectAlternativeName(config: CertificateConfig, san: string) =
        { config with
            SubjectAlternativeNames = san :: config.SubjectAlternativeNames }

    /// <summary>Sets the validation method for the certificate.</summary>
    [<CustomOperation("validation")>]
    member _.Validation(config: CertificateConfig, validation: CertificateValidation) =
        { config with
            Validation = Some validation }

    /// <summary>Uses DNS validation with a specific hosted zone.</summary>
    [<CustomOperation("dnsValidation")>]
    member _.DnsValidation(config: CertificateConfig, hostedZone: IHostedZone) =
        { config with
            Validation = Some(CertificateValidation.FromDns hostedZone) }

    /// <summary>Uses DNS validation with a specific hosted zone.</summary>
    [<CustomOperation("dnsValidation")>]
    member _.DnsValidation(config: CertificateConfig, hostedZone: Route53HostedZoneSpec) =
        match hostedZone.HostedZone with
        | Some knownZone ->
            { config with
                Validation = Some(CertificateValidation.FromDns knownZone) }
        | None ->
            // Todo: This should carry the process forward and resolve it on Run instead of here.
            failwith "DNS-validation from new zone not implemented yet"

    /// <summary>Uses email validation.</summary>
    [<CustomOperation("emailValidation")>]
    member _.EmailValidation(config: CertificateConfig) =
        { config with
            Validation = Some(CertificateValidation.FromEmail()) }

    /// <summary>Sets the certificate name in ACM.</summary>
    [<CustomOperation("certificateName")>]
    member _.CertificateName(config: CertificateConfig, name: string) =
        { config with
            CertificateName_ = Some name }

    /// <summary>Sets the key algorithm.</summary>
    [<CustomOperation("keyAlgorithm")>]
    member _.KeyAlgorithm(config: CertificateConfig, algorithm: KeyAlgorithm) =
        { config with
            KeyAlgorithm = Some algorithm }

// ============================================================================
// DNS Validated Certificate Configuration DSL (for Route53)
// ============================================================================

type DnsValidatedCertificateConfig =
    { CertificateName: string
      ConstructId: string option
      DomainName: string option
      SubjectAlternativeNames: string list
      HostedZone: Route53HostedZoneRef option
      Region: string option
      CertificateName_: string option
      KeyAlgorithm: KeyAlgorithm option }

type DnsValidatedCertificateSpec =
    { CertificateName: string
      ConstructId: string
      Props: DnsValidatedCertificateProps }

type DnsValidatedCertificateBuilder(name: string) =
    member _.Yield _ : DnsValidatedCertificateConfig =
        { CertificateName = name
          ConstructId = None
          DomainName = None
          SubjectAlternativeNames = []
          HostedZone = None
          Region = None
          CertificateName_ = None
          KeyAlgorithm = Some KeyAlgorithm.RSA_2048 }

    member _.Zero() : DnsValidatedCertificateConfig =
        { CertificateName = name
          ConstructId = None
          DomainName = None
          SubjectAlternativeNames = []
          HostedZone = None
          Region = None
          CertificateName_ = None
          KeyAlgorithm = Some KeyAlgorithm.RSA_2048 }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DnsValidatedCertificateConfig) : DnsValidatedCertificateConfig =
        f ()

    member inline x.For
        (
            config: DnsValidatedCertificateConfig,
            [<InlineIfLambda>] f: unit -> DnsValidatedCertificateConfig
        ) : DnsValidatedCertificateConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine
        (
            a: DnsValidatedCertificateConfig,
            b: DnsValidatedCertificateConfig
        ) : DnsValidatedCertificateConfig =
        { CertificateName = a.CertificateName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          DomainName =
            match a.DomainName with
            | Some _ -> a.DomainName
            | None -> b.DomainName
          SubjectAlternativeNames = a.SubjectAlternativeNames @ b.SubjectAlternativeNames
          HostedZone =
            match a.HostedZone with
            | Some _ -> a.HostedZone
            | None -> b.HostedZone
          Region =
            match a.Region with
            | Some _ -> a.Region
            | None -> b.Region
          CertificateName_ =
            match a.CertificateName_ with
            | Some _ -> a.CertificateName_
            | None -> b.CertificateName_
          KeyAlgorithm =
            match a.KeyAlgorithm with
            | Some _ -> a.KeyAlgorithm
            | None -> b.KeyAlgorithm }

    member _.Run(config: DnsValidatedCertificateConfig) : DnsValidatedCertificateSpec =
        let props = DnsValidatedCertificateProps()
        let constructId = config.ConstructId |> Option.defaultValue config.CertificateName

        // Domain name is required
        props.DomainName <-
            match config.DomainName with
            | Some domain -> domain
            | None -> invalidArg "domainName" "Domain name is required for DNS Validated Certificate"

        // Hosted zone is required for DNS validation
        props.HostedZone <-
            match config.HostedZone with
            | Some hz ->
                match hz with
                | Route53HostedZoneRef.Route53HostedZoneInterface ihz -> ihz
                | Route53HostedZoneRef.Route53HostedZoneSpecRef sp ->
                    match sp.HostedZone with
                    | Some hz -> hz
                    | None -> invalidArg "hostedZone" $"Hosted zone {sp.ZoneName} has to be resolved first."
            | None -> invalidArg "hostedZone" "Hosted zone is required for DNS Validated Certificate"

        if not (List.isEmpty config.SubjectAlternativeNames) then
            props.SubjectAlternativeNames <- config.SubjectAlternativeNames |> List.toArray

        config.Region |> Option.iter (fun r -> props.Region <- r)

        config.CertificateName_ |> Option.iter (fun n -> props.CertificateName <- n)

        { CertificateName = config.CertificateName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DnsValidatedCertificateConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the domain name.</summary>
    [<CustomOperation("domainName")>]
    member _.DomainName(config: DnsValidatedCertificateConfig, domain: string) =
        { config with DomainName = Some domain }

    /// <summary>Adds a subject alternative name.</summary>
    [<CustomOperation("subjectAlternativeName")>]
    member _.SubjectAlternativeName(config: DnsValidatedCertificateConfig, san: string) =
        { config with
            SubjectAlternativeNames = san :: config.SubjectAlternativeNames }

    /// <summary>Sets the Route53 hosted zone for DNS validation.</summary>
    [<CustomOperation("hostedZone")>]
    member _.HostedZone(config: DnsValidatedCertificateConfig, hostedZone: IHostedZone) =
        { config with
            HostedZone = Some(Route53HostedZoneRef.Route53HostedZoneInterface hostedZone) }

    /// <summary>Sets the Route53 hosted zone for DNS validation.</summary>
    [<CustomOperation("hostedZone")>]
    member _.HostedZone(config: DnsValidatedCertificateConfig, hostedZone: Route53HostedZoneSpec) =
        { config with
            HostedZone = Some(Route53HostedZoneRef.Route53HostedZoneSpecRef hostedZone) }

    /// <summary>Sets the region for the certificate (useful for CloudFront which requires us-east-1).</summary>
    [<CustomOperation("region")>]
    member _.Region(config: DnsValidatedCertificateConfig, region: string) = { config with Region = Some region }

    /// <summary>Sets the certificate name.</summary>
    [<CustomOperation("certificateName")>]
    member _.CertificateName(config: DnsValidatedCertificateConfig, name: string) =
        { config with
            CertificateName_ = Some name }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CertificateManagerBuilders =
    /// <summary>Creates an ACM certificate with AWS best practices.</summary>
    /// <param name="name">The certificate name.</param>
    /// <code lang="fsharp">
    /// certificate "MySiteCert" {
    ///     domainName "example.com"
    ///     subjectAlternativeName "*.example.com"
    ///     subjectAlternativeName "www.example.com"
    ///     dnsValidation myHostedZone
    /// }
    /// </code>
    let certificate (name: string) = CertificateBuilder name

    /// <summary>Creates a DNS-validated certificate for cross-region use (e.g., CloudFront).</summary>
    /// <param name="name">The certificate name.</param>
    /// <code lang="fsharp">
    /// dnsValidatedCertificate "CloudFrontCert" {
    ///     domainName "example.com"
    ///     hostedZone myHostedZone
    ///     region "us-east-1"
    /// }
    /// </code>
    let dnsValidatedCertificate (name: string) = DnsValidatedCertificateBuilder name
