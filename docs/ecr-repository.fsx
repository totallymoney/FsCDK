(**
---
title: ECR Repository (Container Registry)
category: docs
index: 21
---

# Amazon ECR (Elastic Container Registry)

Amazon Elastic Container Registry (ECR) is a fully managed Docker container registry that makes it easy
to store, manage, and deploy Docker container images. ECR eliminates the need to operate your own container repositories.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.ECR

(**
## Basic Repository

Create a repository with secure defaults.
*)

stack "BasicECR" {
    let repo = ecrRepository "my-app" { () }
    // Uses secure defaults:
    // - Image scan on push: enabled
    // - Tag mutability: MUTABLE
    // - Encryption: AES256 (AWS managed)
    // - Removal policy: RETAIN
    ()
}

(**
## Repository with Lifecycle Rules

Automatically clean up old images to reduce storage costs.
*)

stack "ECRWithLifecycle" {
    // Development repository
    let devRepo =
        ecrRepository "my-app-dev" {
            // Delete untagged images after 7 days
            lifecycleRule (ECRHelpers.deleteUntaggedAfterDays 7)

            // Keep only last 10 images
            lifecycleRule (ECRHelpers.keepLastNImages 10)
            ()
        }

    // Production repository
    let prodRepo =
        ecrRepository "my-app-prod" {
            // Delete untagged images after 14 days
            lifecycleRule (ECRHelpers.deleteUntaggedAfterDays 14)

            // Keep only last 30 images
            lifecycleRule (ECRHelpers.keepLastNImages 30)

            // Immutable tags for production
            imageTagMutability TagMutability.IMMUTABLE
        }

    ()
}

(**
## Repository with Standard Lifecycle Policies

Use predefined lifecycle policies for common scenarios.
*)

stack "ECRStandardPolicies" {
    // Development repository with standard rules
    let devRepo = ecrRepository "app-dev" { () }
    // Add standard dev lifecycle rules using CDK directly:
    // - Delete untagged images after 7 days
    // - Keep last 10 images

    // Production repository with standard rules
    let prodRepo =
        ecrRepository "app-prod" { imageTagMutability TagMutability.IMMUTABLE }
    // Add standard prod lifecycle rules using CDK directly:
    // - Delete untagged images after 14 days
    // - Keep last 30 images`
    ()
}

(**
## Repository with Custom Lifecycle Rules

Define custom lifecycle rules for specific tag patterns.
*)

stack "ECRCustomLifecycle" {
    let repo =
        ecrRepository "my-service" {
            // Delete staging images after 30 days
            lifecycleRule (ECRHelpers.deleteTaggedAfterDays "staging-" 30)

            // Delete feature branch images after 7 days
            lifecycleRule (ECRHelpers.deleteTaggedAfterDays "feature-" 7)

            // Delete PR images after 3 days
            lifecycleRule (ECRHelpers.deleteTaggedAfterDays "pr-" 3)

            // Keep untagged images for only 1 day
            lifecycleRule (ECRHelpers.deleteUntaggedAfterDays 1)
        }

    ()
}

(**
## Immutable Tags

Prevent tag overwrites in production repositories for better traceability.
*)

stack "ImmutableTags" {
    let prodRepo =
        ecrRepository "prod-app" {
            imageTagMutability TagMutability.IMMUTABLE
            imageScanOnPush true
        }

    ()
}

(**
## Repository for Development

Optimized settings for development workflows.
*)

stack "DevRepository" {
    let devRepo =
        ecrRepository "dev-app" {
            // Mutable tags for easy iteration
            imageTagMutability TagMutability.MUTABLE

            // Clean up frequently
            lifecycleRule (ECRHelpers.deleteUntaggedAfterDays 3)
            lifecycleRule (ECRHelpers.keepLastNImages 5)

            // Destroy on stack deletion (dev only!)
            removalPolicy RemovalPolicy.DESTROY
            emptyOnDelete true
        }

    ()
}

(**
## Image Scanning

Enable vulnerability scanning for container images.
*)

stack "SecureRepository" {
    let repo =
        ecrRepository "secure-app" {
            // Scan all pushed images automatically
            imageScanOnPush true

            // Immutable tags for audit trail
            imageTagMutability TagMutability.IMMUTABLE
        }

    ()
}

(**
## Cross-Account Access

Share repositories across AWS accounts.

Note: Cross-account permissions must be configured using repository policies with the CDK API directly.
*)

(**
## Repository Policy

Add resource-based policies for fine-grained access control.

Note: Repository policies must be configured using the CDK Repository directly.
*)

(**
## Lifecycle Rule Helpers

FsCDK provides helper functions for common lifecycle patterns:
*)

// Delete untagged images after N days
let cleanupUntagged = ECRHelpers.deleteUntaggedAfterDays 7

// Keep only the last N images
let keepRecent = ECRHelpers.keepLastNImages 10

// Delete images with tag prefix after N days
let cleanupFeatureBranches = ECRHelpers.deleteTaggedAfterDays "feature-" 7

// Standard development lifecycle
let devRules = ECRHelpers.standardDevLifecycleRules () // 7 days, 10 images

// Standard production lifecycle
let prodRules = ECRHelpers.standardProdLifecycleRules () // 14 days, 30 images

(**
## Best Practices

### Security

- Enable image scanning on push to detect vulnerabilities
- Use immutable tags in production (prevents tag overwrites)
- Scan images before deployment to production
- Use AWS managed encryption (AES256) for at-rest encryption
- Rotate images regularly (rebuild from base images)
- Use multi-stage Docker builds to minimize attack surface
- Implement least-privilege IAM policies for ECR access
- Enable AWS PrivateLink for private registry access

### Cost Optimization

- Use lifecycle rules to delete untagged images (orphaned layers)
- Keep only necessary image versions (e.g., last 30)
- Delete feature branch images after merge
- Use standard lifecycle policies for consistent cleanup
- Monitor storage metrics in CloudWatch
- Clean up test/dev repositories more aggressively
- Consider cross-region replication only when necessary

### Reliability

- Use RETAIN removal policy for production repositories
- Replicate critical images across regions
- Set up CloudWatch alarms for repository metrics
- Monitor image push/pull metrics
- Test disaster recovery procedures
- Maintain golden images for quick recovery

### Performance

- Use ECR in the same region as your compute (ECS/EKS/Lambda)
- Enable image layer caching in CI/CD pipelines
- Use multi-stage builds to optimize image size
- Compress image layers
- Pull images from ECR VPC endpoints for faster pulls

### Operational Excellence

- Use descriptive repository names (e.g., org/team/service)
- Tag repositories with project, environment, team
- Document Dockerfiles and image build process
- Version images using semantic versioning
- Automate image builds with CI/CD
- Monitor security scan findings
- Set up notifications for critical vulnerabilities
- Maintain an image inventory and lifecycle policy

## Default Settings

The ECR repository builder applies these secure defaults:

- **Image Scan on Push**: Enabled (detects vulnerabilities)
- **Tag Mutability**: MUTABLE (allows tag reuse for dev)
- **Encryption**: AES256 (AWS managed encryption)
- **Removal Policy**: RETAIN (prevents accidental deletion)
- **Empty on Delete**: Disabled (requires manual cleanup)

## Tag Mutability

### MUTABLE (Default)
- Allows pushing images with existing tags
- Useful for development (e.g., `latest`, `dev`)
- Flexible but less traceable

### IMMUTABLE
- Prevents overwriting existing tags
- Required for production compliance
- Better audit trail and traceability
- Recommended for production repositories

## Image Scanning

ECR integrates with Amazon Inspector for vulnerability scanning:

- **Basic Scanning**: Scans for CVEs in OS packages (free)
- **Enhanced Scanning**: Deeper scanning including language packages (paid)
- **Scan on Push**: Automatic scanning when images are pushed
- **On-Demand Scanning**: Manual scans via API/Console

Scan findings are available in:
- ECR Console
- Amazon Inspector
- EventBridge events
- Security Hub (if enabled)

## Lifecycle Policies

Lifecycle policies automatically delete images based on:

- **Image Age**: Delete images older than N days
- **Image Count**: Keep only the last N images
- **Tag Status**: Target TAGGED, UNTAGGED, or ANY
- **Tag Prefix**: Filter by tag prefix (e.g., "feature-")

Policies are evaluated in priority order. Use multiple rules to implement complex strategies.

## Escape Hatch

For advanced scenarios, access the underlying CDK Repository:

`fsharp
let repoResource = ecrRepository "my-app" { imageScanOnPush true }

// Access the CDK Repository for advanced configuration
let cdkRepo = repoResource.Repository

// Add repository policy
let policyDocument = ... // Create IAM policy document
cdkRepo.AddToResourcePolicy(PolicyStatement(...))

// Grant pull access to a role
cdkRepo.GrantPull(myRole)

// Grant push access to a role
cdkRepo.GrantPush(myRole)

// Grant pull/push access
cdkRepo.GrantPullPush(myRole)
`

## Integration with ECS/EKS

ECR works seamlessly with Amazon ECS and EKS:

`fsharp
stack "ContainerStack" {
    // Create repository
    let repo =
        ecrRepository "my-service" {
            imageScanOnPush true
            imageTagMutability TagMutability.IMMUTABLE
            ()
        }

    // Use with ECS task definition
    // let taskDef = FargateTaskDefinition(...)
    // taskDef.AddContainer("app",
    //     ContainerDefinitionOptions(
    //         Image = ContainerImage.FromEcrRepository(repo.Repository, "v1.0.0")
    //     ))`
}
`

## CI/CD Integration

Typical CI/CD workflow with ECR:

1. Build Docker image
2. Tag image (e.g., commit SHA, version)
3. Authenticate to ECR (`aws ecr get-login-password`)
4. Push image to repository
5. ECR automatically scans image
6. Deploy to ECS/EKS/Lambda if scan passes

## Resources

- [Amazon ECR Documentation](https://docs.aws.amazon.com/AmazonECR/latest/userguide/what-is-ecr.html)
- [ECR Lifecycle Policies](https://docs.aws.amazon.com/AmazonECR/latest/userguide/lifecycle-policies.html)
- [ECR Image Scanning](https://docs.aws.amazon.com/AmazonECR/latest/userguide/image-scanning.html)
- [ECR Security Best Practices](https://docs.aws.amazon.com/AmazonECR/latest/userguide/security-best-practices.html)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
*)
