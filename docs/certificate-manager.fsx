(**
---
title: Certificate Manager
category: docs
index: 13
---

# AWS Certificate Manager

AWS Certificate Manager (ACM) provides free SSL/TLS certificates for AWS services.
Automatically renew certificates and secure your applications with HTTPS.

## Quick Start

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

(*
stack "Route53Cert" {
    // Assuming hosted zone exists
    let hostedZone = HostedZone.FromLookup(this, "Zone", HostedZoneProviderProps(DomainName = "example.com"))

    certificate "Route53Cert" {
        domainName "example.com"
        subjectAlternativeName "*.example.com"
        dnsValidation hostedZone
    }
}
*)

(**
## CloudFront Certificate (us-east-1)

CloudFront requires certificates in us-east-1. Use DnsValidatedCertificate for cross-region deployment.
*)

(*
stack "CloudFrontCert" {
    let hostedZone = HostedZone.FromLookup(this, "Zone", HostedZoneProviderProps(DomainName = "example.com"))

    dnsValidatedCertificate "CFCert" {
        domainName "cdn.example.com"
        hostedZone hostedZone
        region "us-east-1" // Required for CloudFront
    }
}
*)

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
## Email Validation

Use email validation when DNS validation is not possible.
*)

stack "EmailValidatedCert" {
    certificate "EmailCert" {
        domainName "example.com"
        emailValidation
    }
}

(**
## Complete HTTPS Setup with CloudFront

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
## Best Practices

### Security

- ✅ Use DNS validation (more secure than email)
- ✅ Use RSA_2048 or higher key algorithms
- ✅ Enable certificate transparency logging (default)
- ✅ Rotate certificates before expiration (ACM auto-renews)

### Operational Excellence

- ✅ Use wildcard certificates to simplify management
- ✅ Tag certificates with project and environment
- ✅ Monitor certificate expiration in CloudWatch
- ✅ Document domain ownership requirements

### Cost Optimization

- ✅ ACM certificates are free for AWS services
- ✅ Consolidate domains into fewer certificates
- ✅ Delete unused certificates

### High Availability

- ✅ Use multi-region certificates for global services
- ✅ Maintain backup certificates
- ✅ Plan for certificate rotation

*)
