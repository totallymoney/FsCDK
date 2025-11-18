(**
---
title: IAM Best Practices for FsCDK
category: docs
index: 1
---

# IAM (Identity and Access Management) Best Practices for FsCDK

AWS Identity and Access Management (IAM) is the foundation of AWS security. As AWS Hero Ben Kehoe states: "IAM isn't complicated—it's just misunderstood." This portal enhances FsCDK's IAM docs with insights from heroes like Ben Kehoe, Scott Piper, and Yan Cui. Includes narratives, checklists, drills, and resources (4.5+ rated, highly viewed).

## Principle of Least Privilege

Always grant only the permissions required to perform a task—this is core to zero-trust security.

### Lambda Function Roles
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM

(**
#### ❌ BAD: Too permissive

Don't grant Lambda full access to everything:
*)

// This grants the Lambda full access to everything!
lambda "MyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

// role adminRole  // Don't do this!
}

(**
#### ✅ GOOD: Specific permissions

Grant only specific permissions needed:
*)

(*** hide ***)
let myVpc =
    vpc "MyVpc" {
        maxAzs 2
        natGateways 1
        cidr "10.0.0.0/16"
    }

lambda "MyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    // Grant only specific permissions needed
    policyStatement {
        effect Effect.ALLOW
        actions [ "dynamodb:GetItem"; "dynamodb:PutItem" ]
        resources [ "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable" ]
    }

    policyStatement {
        effect Effect.ALLOW
        actions [ "s3:GetObject" ]
        resources [ "arn:aws:s3:::my-bucket/*" ]
    }
}

(**
## Security Group Best Practices

FsCDK security groups follow least privilege by default.
*)

open Amazon.CDK.AWS.EC2

// ✅ GOOD: FsCDK defaults to denying all outbound

stack "MyStack" {
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    securityGroup "MySecurityGroup" {
        vpc myVpc
        description "Security group for Lambda"
        allowAllOutbound false // This is the default!
    }
}

(**
Only allow specific outbound traffic (Note: In real code, you'd add ingress/egress rules after creation).

❌ BAD: Allowing all outbound unnecessarily:
*)

stack "MyStack" {

    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    securityGroup "TooPermissive" {
        vpc myVpc
        allowAllOutbound true // Only use when absolutely necessary
    }
}

(**
## RDS Database Access

Restrict database access to specific security groups.
*)

open Amazon.CDK.AWS.RDS

// ✅ GOOD: Database in private subnet with restricted access


stack "MyStack" {
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    let! lambdaSecurityGroup =
        securityGroup "MySecurityGroup" {
            vpc myVpc
            description "Security group for Lambda"
            allowAllOutbound false // This is the default!
        }

    rdsInstance "MyDatabase" {
        vpc myVpc
        postgresEngine

        // Private subnet - not accessible from the internet
        vpcSubnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))

        // Not publicly accessible
        publiclyAccessible false

        // Only Lambda security group can access
        securityGroup lambdaSecurityGroup

        // Enable IAM authentication for better security
        iamAuthentication true
    }
}
(**
## Cognito Security

Implement strong authentication and authorization.
*)

open Amazon.CDK.AWS.Cognito

// ✅ GOOD: Secure user pool configuration
let myUserPool =
    userPool "SecureUserPool" {
        signInWithEmail

        // Disable self sign-up to prevent unauthorized accounts
        selfSignUpEnabled false // Approve users manually or via API

        // Require MFA for sensitive operations
        mfa Mfa.REQUIRED

        // Strong password policy
        passwordPolicy (
            PasswordPolicy(
                MinLength = 12,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigits = true,
                RequireSymbols = true,
                TempPasswordValidity = Duration.Days(7.0)
            )
        )

        // Account recovery via email only (more secure than SMS)
        accountRecovery AccountRecovery.EMAIL_ONLY
    }

// ✅ GOOD: Secure client configuration
userPoolClient "SecureClient" {
    userPool myUserPool

    // Don't generate secret for public clients (web/mobile)
    generateSecret false

    // Use SRP for secure authentication
    authFlows (
        AuthFlow(
            UserSrp = true,
            UserPassword = true,
            AdminUserPassword = false // Don't allow admin-initiated auth
        )
    )

    // Short-lived tokens
    tokenValidities (
        (Duration.Minutes 60.0), // refreshToken
        (Duration.Minutes 60.0), // accessToken
        (Duration.Days 30.0) // idToken
    )
}

(**
## S3 Bucket Policies

Secure your S3 buckets properly.
*)

open Amazon.CDK.AWS.S3

// ✅ GOOD: Secure S3 bucket
bucket "SecureAssets" {
    // Block all public access
    blockPublicAccess BlockPublicAccess.BLOCK_ALL

    // Encrypt data at rest
    encryption BucketEncryption.S3_MANAGED

    // Enforce SSL for all requests
    enforceSSL true

    // Enable versioning for data protection
    versioned true

    // Prevent accidental deletion
    removalPolicy RemovalPolicy.RETAIN
    autoDeleteObjects false
}

(**
## CloudFront CDN Security

Secure content delivery with CloudFront.
*)

open Amazon.CDK.AWS.CloudFront

(*** hide ***)
let myBehavior =
    CloudFrontBehaviors.httpBehaviorDefault "origin.example.com" (Some true)

(*** hide ***)
let myLogBucket =
    bucket "CloudFrontLogs" {
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
        encryption BucketEncryption.S3_MANAGED
        enforceSSL true
        versioned false
        removalPolicy RemovalPolicy.RETAIN
    }

// ✅ GOOD: Secure CloudFront distribution
cloudFrontDistribution "SecureCDN" {
    defaultBehavior myBehavior

    // Require HTTPS (Note: This is configured in the behavior)

    // Use modern TLS version
    minimumProtocolVersion SecurityPolicyProtocol.TLS_V1_2_2021

    // Optional: Add WAF for additional protection
    // webAclId myWafAclId

    // Enable logging for audit trail
    enableLogging myLogBucket "cloudfront-logs/"
}

(**
## Common IAM Patterns

### Read-Only Access to DynamoDB
*)

lambda "ReadOnlyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    policyStatement {
        effect Effect.ALLOW

        actions
            [ "dynamodb:GetItem"
              "dynamodb:Query"
              "dynamodb:Scan"
              "dynamodb:BatchGetItem" ]

        resources [ "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable" ]
    }
}

(**
### Write Access to S3 Bucket
*)

lambda "S3Writer" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    policyStatement {
        effect Effect.ALLOW
        actions [ "s3:PutObject"; "s3:PutObjectAcl" ]
        resources [ "arn:aws:s3:::my-bucket/*" ]
    }
}

(**
### Read from SQS Queue
*)

open Amazon.CDK.AWS.SQS

lambda "QueueProcessor" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    policyStatement {
        effect Effect.ALLOW
        actions [ "sqs:ReceiveMessage"; "sqs:DeleteMessage"; "sqs:GetQueueAttributes" ]
        resources [ "arn:aws:sqs:us-east-1:123456789012:my-queue" ]
    }
}

(**
### Publish to SNS Topic
*)

open Amazon.CDK.AWS.SNS

lambda "NotificationSender" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    policyStatement {
        effect Effect.ALLOW
        actions [ "sns:Publish" ]
        resources [ "arn:aws:sns:us-east-1:123456789012:my-topic" ]
    }
}

(**
## Virtual Private Cloud (VPC) Security

Network isolation and security.
*)

// ✅ GOOD: Properly segmented VPC
vpc "SecureVpc" {
    maxAzs 2

    // Subnet configuration (done via CDK)
    // - Public subnets: NAT Gateways, Load Balancers
    // - Private subnets: Lambda, ECS, RDS
    // - Isolated subnets: Highly sensitive resources

    // NAT Gateways for private subnet internet access
    natGateways 2 // One per AZ for HA

// Enable VPC Flow Logs for monitoring
// (Note: Would need to be configured separately)
}

// Place sensitive resources in private subnets

stack "DatabaseStack" {
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    let! lambdaSecurityGroup =
        securityGroup "MySecurityGroup" {
            vpc myVpc
            description "Security group for Lambda"
            allowAllOutbound false // This is the default!
        }

    rdsInstance "Database" {
        vpc myVpc
        postgresEngine
        vpcSubnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
        publiclyAccessible false
    }

    lambda "PrivateFunction" {
        runtime Runtime.DOTNET_8
        handler "MyApp::Handler"
        code "./publish"
        vpcSubnets { yield SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS) }
        securityGroups [ lambdaSecurityGroup ]
    }
}

(**
## Monitoring and Auditing

Enable CloudTrail and monitoring.
*)

// Enable detailed monitoring
lambda "MonitoredFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    // Enable AWS X-Ray tracing
    tracing Tracing.ACTIVE

    // Enable Lambda Insights
    insightsVersion LambdaInsightsVersion.VERSION_1_0_229_0
}

// Enable RDS Performance Insights


stack "DatabaseStack" {
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    rdsInstance "MonitoredDatabase" {
        vpc myVpc
        postgresEngine
        enablePerformanceInsights true
    }
}

(**
## Secrets Management

Never hardcode secrets!

❌ BAD: Hardcoded secrets
*)

lambda "BadFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"

    environment [ "DB_PASSWORD", "mypassword123" ] // Never do this!
}

(**
✅ GOOD: Use Secrets Manager or Parameter Store
*)
stack "DatabaseStack" {

    lambda "GoodFunction" {
        runtime Runtime.DOTNET_8
        handler "MyApp::Handler"
        code "./publish"

        environment [ "DB_SECRET_ARN", "arn:aws:secretsmanager:us-east-1:123456789012:secret:db-secret" ]

        // Grant permission to read secret
        policyStatement {
            effect Effect.ALLOW
            actions [ "secretsmanager:GetSecretValue" ]
            resources [ "arn:aws:secretsmanager:us-east-1:123456789012:secret:db-secret" ]
        }
    }

    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    // RDS can generate and manage secrets automatically
    rdsInstance "SecureDatabase" {
        vpc myVpc
        postgresEngine

        // Credentials automatically stored in Secrets Manager
        credentials (Credentials.FromGeneratedSecret "admin")
    }
}

(**
## Compliance Considerations

### GDPR/Data Privacy
*)

stack "DatabaseStack" {

    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    // Enable encryption for data at rest
    bucket "UserData" {
        encryption BucketEncryption.KMS_MANAGED // Customer managed keys
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
    }

    rdsInstance "UserDatabase" {
        vpc myVpc
        postgresEngine
        storageEncrypted true
        deletionProtection true
    }

    // Enable audit logging
    userPool "CompliantUserPool" {
        signInWithEmail
    // Cognito automatically logs authentication events
    }
}

(**
### HIPAA/PHI Data
*)

stack "DatabaseStack" {
    // Use KMS encryption for sensitive data
    bucket "HealthRecords" {
        encryption BucketEncryption.KMS_MANAGED
        versioned true
        removalPolicy RemovalPolicy.RETAIN
    }

    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    // Enable audit trails
    rdsInstance "HealthDatabase" {
        vpc myVpc
        postgresEngine
        storageEncrypted true
        backupRetentionDays 30.0 // Longer retention for compliance
        deletionProtection true
    }
}

(**
## Operational Checklist
Use this before prod deploys (inspired by Piper's audits):
1. Validate policies with IAM Access Analyzer.
2. Enable MFA and rotate keys.
3. Scan for secrets with git-secrets.
4. Run Prowler for compliance.
5. Document all roles/policies.

## Deliberate Practice Drills
### Drill 1: Policy Crafting
1. Write a policy allowing only S3:PutObject to a bucket.
2. Test with Policy Simulator.
3. Add conditions (e.g., IP restriction).

### Drill 2: Privilege Escalation
1. Use IAM Vulnerable to simulate attacks.
2. Identify and fix escalations in sample policies.

## 📚 Learning Resources for IAM & AWS Security

### AWS Official Documentation

**IAM Fundamentals:**

- [IAM User Guide](https://docs.aws.amazon.com/IAM/latest/UserGuide/introduction.html) - Complete IAM documentation
- [IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html) - Official AWS recommendations
- [IAM Policies and Permissions](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html) - Policy structure explained
- [IAM Policy Simulator](https://policysim.aws.amazon.com/) - Test policies before deployment

**Security Best Practices:**

- [AWS Security Best Practices](https://docs.aws.amazon.com/security/) - Comprehensive security guidance
- [AWS Well-Architected Framework - Security Pillar](https://docs.aws.amazon.com/wellarchitected/latest/security-pillar/welcome.html) - The 5 pillars of security
- [AWS Security Hub](https://aws.amazon.com/security-hub/) - Centralized security monitoring
- [AWS GuardDuty](https://aws.amazon.com/guardduty/) - Threat detection service

### AWS Heroes & Security Experts

**Ben Kehoe (@ben11kehoe) - AWS Serverless Hero:**

- [IAM for Humans](https://ben11kehoe.medium.com/iam-is-complicated-but-it-doesnt-have-to-be-b71e7b0b6c5c) - Simplifying IAM concepts
- [AWS IAM Policies in a Nutshell](https://ben11kehoe.medium.com/aws-iam-policies-in-a-nutshell-63d42d1caec5) - Understanding policy evaluation
- [Temporary Security Credentials](https://ben11kehoe.medium.com/you-should-never-use-aws-access-keys-or-iam-users-5d8e8e9f3d8e) - Why you should use roles
- [IAM Roles Everywhere](https://www.youtube.com/watch?v=aISWoPf_XNE) - AWS re:Invent talk on IAM roles

**Scott Piper (@0xdabbad00) - AWS Security Hero:**

- [Flaws.cloud](http://flaws.cloud/) - Learn AWS security through CTF challenges
- [CloudSploit](https://github.com/aquasecurity/cloudsploit) - AWS security scanning tool
- [AWS Security Mistakes](https://summitroute.com/blog/2020/05/21/aws_security_mistakes/) - Common pitfalls
- [IAM Vulnerable](https://github.com/BishopFox/iam-vulnerable) - Learn IAM privilege escalation

**Yan Cui (The Burning Monk) - Serverless Security:**

- [Serverless Security Best Practices](https://theburningmonk.com/2018/01/serverless-security-best-practices/) - Lambda-specific security
- [API Gateway Security](https://theburningmonk.com/2019/02/securing-api-gateway-with-lambda-authorizer/) - Custom authorizers
- [Secrets Management in Lambda](https://theburningmonk.com/2019/09/why-you-should-use-temporary-stack-credentials-for-your-aws-cloudformation-deployments/) - Handling sensitive data

### IAM Deep Dives

**Policy Evaluation Logic:**

- [Policy Evaluation Logic](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_evaluation-logic.html) - How AWS evaluates permissions
- [IAM JSON Policy Elements](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_elements.html) - Effect, Action, Resource, Condition
- [Policy Variables](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_variables.html) - Dynamic policy values
- [Service Control Policies (SCPs)](https://docs.aws.amazon.com/organizations/latest/userguide/orgs_manage_policies_scps.html) - Organization-wide guardrails

**Least Privilege:**

- [Grant Least Privilege](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html#grant-least-privilege) - Only grant what's needed
- [IAM Access Analyzer](https://docs.aws.amazon.com/IAM/latest/UserGuide/what-is-access-analyzer.html) - Find overly permissive policies
- [Access Advisor](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies_access-advisor.html) - See unused permissions
- [Policy Generator](https://awspolicygen.s3.amazonaws.com/policygen.html) - Create policies visually

**IAM Roles & Temporary Credentials:**

- [IAM Roles](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles.html) - Temporary credentials for services
- [Assume Role](https://docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRole.html) - Cross-account access
- [Session Policies](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html#policies_session) - Further restrict temporary credentials
- [OIDC Federation](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_oidc.html) - GitHub Actions, Google, etc.

### Serverless Security

**Lambda Security Best Practices:**

- [Lambda Security Overview](https://docs.aws.amazon.com/lambda/latest/dg/lambda-security.html) - Official security guide
- [Lambda Execution Roles](https://docs.aws.amazon.com/lambda/latest/dg/lambda-intro-execution-role.html) - Least privilege for functions
- [Lambda Resource Policies](https://docs.aws.amazon.com/lambda/latest/dg/access-control-resource-based.html) - Who can invoke your functions
- [VPC Security for Lambda](https://aws.amazon.com/blogs/compute/announcing-improved-vpc-networking-for-aws-lambda-functions/) - Private resource access

**API Gateway Security:**

- [API Gateway Authorization](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-control-access-to-api.html) - IAM, Cognito, Lambda authorizers
- [Lambda Authorizers](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-use-lambda-authorizer.html) - Custom authentication
- [API Keys](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-api-usage-plans.html) - Rate limiting and quotas
- [WAF Integration](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-control-access-aws-waf.html) - Protect against attacks

**Secrets Management:**

- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html) - Rotate and manage secrets
- [Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html) - Secure configuration storage
- [Lambda Environment Variables](https://docs.aws.amazon.com/lambda/latest/dg/configuration-envvars.html) - Encrypted with KMS
- [Secrets Manager Lambda Extension](https://docs.aws.amazon.com/secretsmanager/latest/userguide/retrieving-secrets_lambda.html) - Caching secrets in Lambda

### AWS Security Services

**Detection & Response:**

- [AWS GuardDuty](https://docs.aws.amazon.com/guardduty/latest/ug/what-is-guardduty.html) - Threat detection
- [AWS Security Hub](https://docs.aws.amazon.com/securityhub/latest/userguide/what-is-securityhub.html) - Centralized security findings
- [Amazon Detective](https://docs.aws.amazon.com/detective/latest/userguide/what-is-detective.html) - Security investigation
- [AWS Macie](https://docs.aws.amazon.com/macie/latest/user/what-is-macie.html) - Discover sensitive data in S3

**Compliance & Auditing:**

- [AWS CloudTrail](https://docs.aws.amazon.com/awscloudtrail/latest/userguide/cloudtrail-user-guide.html) - Audit all API calls
- [AWS Config](https://docs.aws.amazon.com/config/latest/developerguide/WhatIsConfig.html) - Resource configuration tracking
- [AWS Audit Manager](https://docs.aws.amazon.com/audit-manager/latest/userguide/what-is.html) - Continuous auditing
- [AWS Artifact](https://docs.aws.amazon.com/artifact/latest/ug/what-is-aws-artifact.html) - Compliance reports

**Encryption & Key Management:**

- [AWS KMS](https://docs.aws.amazon.com/kms/latest/developerguide/overview.html) - Managed encryption keys
- [KMS Key Policies](https://docs.aws.amazon.com/kms/latest/developerguide/key-policies.html) - Control key access
- [Envelope Encryption](https://docs.aws.amazon.com/kms/latest/developerguide/concepts.html#enveloping) - Data encryption pattern
- [AWS CloudHSM](https://docs.aws.amazon.com/cloudhsm/latest/userguide/introduction.html) - Hardware security modules

### Security Frameworks & Compliance

**Compliance Standards:**

- [AWS Compliance Programs](https://aws.amazon.com/compliance/programs/) - HIPAA, PCI-DSS, SOC 2, etc.
- [HIPAA on AWS](https://aws.amazon.com/compliance/hipaa-compliance/) - Healthcare compliance
- [PCI-DSS on AWS](https://aws.amazon.com/compliance/pci-dss-level-1-faqs/) - Payment card industry
- [GDPR on AWS](https://aws.amazon.com/compliance/gdpr-center/) - Data privacy

**Security Frameworks:**

- [AWS Well-Architected Security Pillar](https://docs.aws.amazon.com/wellarchitected/latest/security-pillar/welcome.html) - 7 design principles
- [CIS AWS Foundations Benchmark](https://www.cisecurity.org/benchmark/amazon_web_services) - Industry best practices
- [NIST Cybersecurity Framework](https://aws.amazon.com/compliance/nist/) - Federal security standards
- [OWASP Top 10](https://owasp.org/www-project-top-ten/) - Web application security risks

### Video Tutorials

**IAM Fundamentals:**
- [IAM Primer](https://www.youtube.com/watch?v=SXSqhTn2DuE) - AWS official introduction
- [Become an IAM Policy Master](https://www.youtube.com/watch?v=YQsK4MtsELU) - Comprehensive tutorial
- [IAM Roles Explained](https://www.youtube.com/watch?v=qpvbHwP3U_M) - Understanding roles

**Advanced Security:**

- [AWS re:Inforce Security Conference](https://www.youtube.com/results?search_query=aws+reinforce) - Annual security-focused event
- [IAM Policy Deep Dive](https://www.youtube.com/watch?v=ExjW3HCFVSI) - Advanced policy patterns
- [Cross-Account Access](https://www.youtube.com/watch?v=HPfGG8xLqCk) - Secure multi-account strategies

**Serverless Security:**

- [Serverless Security Best Practices](https://www.youtube.com/watch?v=kmSdyN9qiXY) - AWS re:Invent
- [Lambda Security Deep Dive](https://www.youtube.com/watch?v=ODg9LG5JxXM) - Production patterns
- [API Gateway Authorization](https://www.youtube.com/watch?v=VZqG7HjT2AQ) - Custom authorizers

### Security Tools & Utilities

**Policy Analysis:**

- [Parliament](https://github.com/duo-labs/parliament) - IAM policy linter
- [IAM Policy Validator](https://github.com/aws-cloudformation/cloudformation-cli/tree/master/src/cfn_policy_validator) - Validate CloudFormation IAM
- [Cloudsplaining](https://github.com/salesforce/cloudsplaining) - Identify overprivileged policies
- [IAMbic](https://github.com/noqdev/iambic) - IAM as code

**Security Scanning:**

- [Prowler](https://github.com/prowler-cloud/prowler) - AWS security best practices scanner
- [ScoutSuite](https://github.com/nccgroup/ScoutSuite) - Multi-cloud security auditing
- [CloudMapper](https://github.com/duo-labs/cloudmapper) - Visualize AWS environments
- [PMapper](https://github.com/nccgroup/PMapper) - IAM privilege escalation analysis

**Secrets Detection:**

- [git-secrets](https://github.com/awslabs/git-secrets) - Prevent committing secrets
- [TruffleHog](https://github.com/trufflesecurity/truffleHog) - Find secrets in git history
- [detect-secrets](https://github.com/Yelp/detect-secrets) - Prevent secrets in code

### Hands-On Learning

**AWS Security Workshops:**

- [AWS Security Workshops](https://workshops.aws/categories/Security) - Official hands-on labs
- [IAM Workshop](https://catalog.workshops.aws/iam/en-US) - Deep dive into IAM
- [Serverless Security Workshop](https://catalog.workshops.aws/serverless-security/en-US) - Secure serverless apps
- [Well-Architected Security Labs](https://www.wellarchitectedlabs.com/security/) - Security best practices

**Security Challenges:**

- [flaws.cloud](http://flaws.cloud/) - CTF-style AWS security challenges
- [flaws2.cloud](http://flaws2.cloud/) - Advanced AWS security challenges
- [CloudGoat](https://github.com/RhinoSecurityLabs/cloudgoat) - Vulnerable AWS environments

### Recommended Learning Path

**Week 1 - IAM Fundamentals:**

1. Read [IAM User Guide](https://docs.aws.amazon.com/IAM/latest/UserGuide/introduction.html) - Chapters 1-5
2. Watch [IAM Primer Video](https://www.youtube.com/watch?v=SXSqhTn2DuE)
3. Practice with [IAM Policy Simulator](https://policysim.aws.amazon.com/)
4. Review [FsCDK IAM examples](iam-best-practices.html) (this document)

**Week 2 - Policy Mastery:**

1. Study [Policy Evaluation Logic](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_evaluation-logic.html)
2. Read [Ben Kehoe's IAM Posts](https://ben11kehoe.medium.com/)
3. Use [IAM Access Analyzer](https://docs.aws.amazon.com/IAM/latest/UserGuide/what-is-access-analyzer.html)
4. Take [IAM Workshop](https://catalog.workshops.aws/iam/en-US)

**Week 3 - Security Services:**

1. Enable [AWS GuardDuty](https://docs.aws.amazon.com/guardduty/latest/ug/what-is-guardduty.html)
2. Configure [Security Hub](https://docs.aws.amazon.com/securityhub/latest/userguide/what-is-securityhub.html)
3. Set up [CloudTrail](https://docs.aws.amazon.com/awscloudtrail/latest/userguide/cloudtrail-user-guide.html)
4. Run [Prowler](https://github.com/prowler-cloud/prowler) security scan

**Week 4 - Serverless Security:**

1. Read [Lambda Security Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/lambda-security.html)
2. Implement [Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html) in Lambda
3. Add [Lambda authorizers](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-use-lambda-authorizer.html) to API Gateway
4. Take [Serverless Security Workshop](https://catalog.workshops.aws/serverless-security/en-US)

**Ongoing - Security Mastery:**

- Complete [flaws.cloud](http://flaws.cloud/) and [flaws2.cloud](http://flaws2.cloud/) challenges
- Watch [AWS re:Inforce sessions](https://www.youtube.com/results?search_query=aws+reinforce)
- Follow [AWS Security Blog](https://aws.amazon.com/blogs/security/)
- Run weekly [Prowler scans](https://github.com/prowler-cloud/prowler)

### AWS Security Experts to Follow

**AWS Heroes:**

- **[Ben Kehoe (@ben11kehoe)](https://twitter.com/ben11kehoe)** - IAM and serverless security
- **[Scott Piper (@0xdabbad00)](https://twitter.com/0xdabbad00)** - Cloud security, IAM privilege escalation
- **[Chris Farris (@jcfarris)](https://twitter.com/jcfarris)** - AWS security and compliance
- **[Yan Cui (@theburningmonk)](https://twitter.com/theburningmonk)** - Serverless security

**Security Researchers:**

- **[Mark Nunnikhoven (@marknca)](https://twitter.com/marknca)** - Cloud security best practices
- **[Corey Quinn (@QuinnyPig)](https://twitter.com/QuinnyPig)** - AWS cost and security
- **[Ian Mckay (@iann0036)](https://twitter.com/iann0036)** - AWS security tools

**AWS Security Team:**

- Follow [AWS Security Blog](https://aws.amazon.com/blogs/security/)
- Subscribe to [AWS Security Bulletins](https://aws.amazon.com/security/security-bulletins/)

### Common Security Pitfalls

**❌ DON'T:**

1. **Use root account for daily tasks** → Create IAM users with least privilege
2. **Hardcode AWS credentials** → Use IAM roles for services, temporary credentials for users
3. **Use wildcard (*) in policies** → Be specific with actions and resources
4. **Ignore CloudTrail logs** → Enable and monitor for suspicious activity
5. **Share IAM credentials** → Each person gets their own IAM user/role
6. **Leave unused access keys active** → Rotate regularly, delete unused keys
7. **Grant broad S3 permissions** → Use specific bucket and prefix restrictions
8. **Disable MFA** → Enable MFA for all human users, especially admins

**✅ DO:**

1. **Use IAM roles for services** → Lambda, EC2, ECS should use roles
2. **Enable MFA for all users** → Especially for privileged accounts
3. **Rotate credentials regularly** → 90-day rotation policy
4. **Use CloudTrail and Config** → Audit all API calls and configuration changes
5. **Apply least privilege** → Grant only necessary permissions
6. **Enable GuardDuty** → Detect threats automatically
7. **Use Secrets Manager** → Store and rotate secrets securely
8. **Tag resources** → Enforce ABAC (Attribute-Based Access Control)

### FsCDK Security Features

**Security by Default:**

- Lambda functions use least-privilege execution roles
- S3 buckets block public access and enforce SSL
- Security groups deny all outbound by default
- Environment variables encrypted with KMS
- CloudWatch Logs retention configured

**IAM Helpers:**

- `grant` builder for simple IAM permissions
- `policyStatement` for custom policies
- Automatic role creation with minimal permissions
- Support for managed policies

For implementation details, see [src/IAM.fs](../src/IAM.fs) and [src/Grants.fs](../src/Grants.fs) in the FsCDK repository.

### Additional Security Resources

- [AWS Well-Architected Framework - Security Pillar](https://docs.aws.amazon.com/wellarchitected/latest/security-pillar/welcome.html)
- [AWS Security Best Practices](https://aws.amazon.com/architecture/security-identity-compliance/)
- [Yan Cui's Serverless Security Best Practices](https://theburningmonk.com/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CIS AWS Foundations Benchmark](https://www.cisecurity.org/benchmark/amazon_web_services)
*)
