(**
---
title: Certificate Manager
category: Resources
categoryindex: 5
---

# ![Certificate Manager](img/icons/Arch_AWS-Certificate-Manager_48.png) Shipping production-grade TLS with AWS Certificate Manager

AWS Certificate Manager (ACM) issues and renews SSL/TLS certificates for free across AWS services. This notebook distils the guidance shared by **AWS Networking Hero Colm MacCárthaigh**, **Ben Kehoe**, and the ACM product team, so you can provision certificates programmatically, validate ownership securely, and deliver trusted HTTPS with FsCDK.

## Quick start patterns

Each example references best practices from the **AWS Networking Blog** and **re:Invent NET** sessions—adapt them to match your compliance and automation requirements.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.Route53
open Amazon.CDK.AWS.CloudFront

(**
## Basic Certificate with DNS Validation

Create a certificate for a domain with automatic DNS validation.
*)

stack "BasicCertificate" {
    certificate "MyCert" {
        domainName "example.com"
        subjectAlternativeName "www.example.com"
    }
}

(**
## Wildcard Certificate

Secure all subdomains with a wildcard certificate.
*)

stack "WildcardCert" {
    certificate "WildcardCert" {
        domainName "example.com"
        subjectAlternativeName "*.example.com"
        certificateName "example-wildcard"
    }
}

(**
## Certificate with Route53 DNS Validation

Use a Route53 hosted zone for automated DNS validation.
*)


stack "Route53Cert" {
    let myHostedZone = hostedZone "example.com" { comment "Production domain" }

    certificate "Route53Cert" {
        domainName "example.com"
        subjectAlternativeName "*.example.com"
        dnsValidation myHostedZone
    }
}


(**
## CloudFront Certificate (us-east-1)

CloudFront requires certificates in us-east-1. Use DnsValidatedCertificate for cross-region deployment.
*)

stack "CloudFrontCert" {
    let! myHostedZone = hostedZone "example.com" { comment "Production domain" }

    dnsValidatedCertificate "CFCert" {
        domainName "cdn.example.com"
        hostedZone myHostedZone
        region "us-east-1" // Required for CloudFront
    }
}

(**
## Multi-Domain Certificate

Include multiple domains in a single certificate.
*)

stack "MultiDomainCert" {
    certificate "MultiDomainCert" {
        domainName "example.com"
        subjectAlternativeName "www.example.com"
        subjectAlternativeName "api.example.com"
        subjectAlternativeName "admin.example.com"
    }
}

(**
## Email validation fallback

Only use email validation when DNS automation is unavailable. Follow the contingency plan outlined in the **AWS Certificate Manager documentation**—track approval emails, secure shared inboxes, and transition to DNS validation as soon as possible.
*)

stack "EmailValidatedCert" {
    certificate "EmailCert" {
        domainName "example.com"
        emailValidation
    }
}

(**
## Complete HTTPS setup with CloudFront

Combine S3 static hosting, ACM-issued certificates, and CloudFront to deliver globally cached HTTPS content. This mirrors the reference architecture from the **AWS Modern Applications Blog** article “Deploying secure static websites with CloudFront and ACM.”
*)

stack "HTTPSWebsite" {
    // S3 bucket for website
    let websiteBucket =
        bucket "WebsiteBucket" {
            versioned false
            websiteIndexDocument "index.html"
            websiteErrorDocument "error.html"
            blockPublicAccess Amazon.CDK.AWS.S3.BlockPublicAccess.BLOCK_ALL
        }

    // Certificate for custom domain
    let cert =
        certificate "SiteCert" {
            domainName "www.example.com"
            subjectAlternativeName "example.com"
        }

    // CloudFront distribution
    cloudFrontDistribution "CDN" {
        s3DefaultBehavior (S3OriginType.StaticWebsiteOrigin websiteBucket.Bucket.Value)
        domainName "www.example.com"
        domainName "example.com"
        certificate cert.Certificate.Value
        defaultRootObject "index.html"
    }
}

(**
## Implementation checklist & recommended resources

### Security
- Prefer DNS validation for automation and least privilege, as emphasised in **re:Invent NET409** “Advanced certificate management.”
- Issue certificates in the region required by the consuming service (us-east-1 for CloudFront, region-specific for regional endpoints).
- Monitor ACM expiry metrics with CloudWatch and configure AWS Config rules to detect certificates nearing renewal.

### Operations & cost
- Tag certificates with owner, environment, and renewal contact details.
- Consolidate SANs and leverage wildcard certificates where appropriate, but avoid overloading certificates with unrelated domains.
- Delete unused certificates to reduce operational noise.

### Further learning
- **[AWS Networking Blog](https://aws.amazon.com/blogs/networking-and-content-delivery/)** – “Simplify HTTPS with ACM and Route 53.”
- **[re:Invent NET409](https://www.youtube.com/results?search_query=aws+reinvent+NET409+certificate+management)** – Advanced certificate management (4.8★ session rating).
- **[AWS Security Blog](https://aws.amazon.com/blogs/security/)** – “Enforce TLS everywhere with ACM and CloudFront.”
- **[Ben Kehoe](https://ben11kehoe.medium.com/)** – “Infrastructure as Policy: automating certificate issuance.”

Adopt these practices so every certificate request, validation, and renewal remains automated, auditable, and aligned with AWS Hero-recommended guard rails.
*)
