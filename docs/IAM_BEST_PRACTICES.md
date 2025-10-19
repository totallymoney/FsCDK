# IAM Best Practices for FsCDK

This guide covers IAM (Identity and Access Management) best practices when using FsCDK to build AWS infrastructure.

## Principle of Least Privilege

Always grant only the permissions required to perform a task.

### Lambda Function Roles

```fsharp
// ❌ BAD: Too permissive
lambda "MyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    // This grants the Lambda full access to everything!
    role adminRole
}

// ✅ GOOD: Specific permissions
lambda "MyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    // Grant only specific permissions needed
    policyStatement {
        effect Effect.ALLOW
        actions [ "dynamodb:GetItem"; "dynamodb:PutItem" ]
        resources [ myTableArn ]
    }
    
    policyStatement {
        effect Effect.ALLOW
        actions [ "s3:GetObject" ]
        resources [ $"{myBucketArn}/*" ]
    }
}
```

## Security Group Best Practices

FsCDK security groups follow least privilege by default.

```fsharp
// ✅ GOOD: FsCDK defaults to denying all outbound
securityGroup "MySecurityGroup" {
    vpc myVpc
    description "Security group for Lambda"
    allowAllOutbound false  // This is the default!
}

// Only allow specific outbound traffic
// (Note: In real code, you'd add ingress/egress rules after creation)

// ❌ BAD: Allowing all outbound unnecessarily
securityGroup "TooPermissive" {
    vpc myVpc
    allowAllOutbound true  // Only use when absolutely necessary
}
```

## RDS Database Access

Restrict database access to specific security groups.

```fsharp
// ✅ GOOD: Database in private subnet with restricted access
rdsInstance "MyDatabase" {
    vpc myVpc
    postgresEngine
    
    // Private subnet - not accessible from internet
    vpcSubnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
    
    // Not publicly accessible
    publiclyAccessible false
    
    // Only Lambda security group can access
    securityGroup lambdaSecurityGroup
    
    // Enable IAM authentication for better security
    iamAuthentication true
}
```

## Cognito Security

Implement strong authentication and authorization.

```fsharp
// ✅ GOOD: Secure user pool configuration
userPool "SecureUserPool" {
    signInWithEmail
    
    // Disable self sign-up to prevent unauthorized accounts
    selfSignUpEnabled false  // Approve users manually or via API
    
    // Require MFA for sensitive operations
    mfa Mfa.REQUIRED
    
    // Strong password policy
    passwordPolicy (PasswordPolicy(
        MinLength = 12,
        RequireLowercase = true,
        RequireUppercase = true,
        RequireDigits = true,
        RequireSymbols = true,
        TempPasswordValidity = Duration.Days(7.0)
    ))
    
    // Account recovery via email only (more secure than SMS)
    accountRecovery AccountRecovery.EMAIL_ONLY
}

// ✅ GOOD: Secure client configuration
userPoolClient "SecureClient" {
    userPool myUserPool
    
    // Don't generate secret for public clients (web/mobile)
    generateSecret false
    
    // Use SRP for secure authentication
    authFlows (AuthFlow(
        UserSrp = true,
        UserPassword = true,
        AdminUserPassword = false  // Don't allow admin-initiated auth
    ))
    
    // Short-lived tokens
    tokenValidities(
        accessToken = Duration.Minutes(60.0),
        idToken = Duration.Minutes(60.0),
        refreshToken = Duration.Days(30.0)
    )
}
```

## S3 Bucket Policies

Secure your S3 buckets properly.

```fsharp
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
```

## CloudFront Security

Secure content delivery with CloudFront.

```fsharp
// ✅ GOOD: Secure CloudFront distribution
cloudFrontDistribution "SecureCDN" {
    defaultBehavior myBehavior
    
    // Require HTTPS
    // (Note: This is configured in the behavior)
    
    // Use modern TLS version
    minimumProtocolVersion SecurityPolicyProtocol.TLS_V1_2_2021
    
    // Optional: Add WAF for additional protection
    webAclId myWafAclId
    
    // Enable logging for audit trail
    enableLogging myLogBucket "cloudfront-logs/"
}
```

## Common IAM Patterns

### Read-Only Access to DynamoDB

```fsharp
lambda "ReadOnlyFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    policyStatement {
        effect Effect.ALLOW
        actions [ 
            "dynamodb:GetItem"
            "dynamodb:Query"
            "dynamodb:Scan"
            "dynamodb:BatchGetItem"
        ]
        resources [ myTableArn ]
    }
}
```

### Write Access to S3 Bucket

```fsharp
lambda "S3Writer" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    policyStatement {
        effect Effect.ALLOW
        actions [ "s3:PutObject"; "s3:PutObjectAcl" ]
        resources [ $"{myBucketArn}/*" ]
    }
}
```

### Read from SQS Queue

```fsharp
lambda "QueueProcessor" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    policyStatement {
        effect Effect.ALLOW
        actions [ 
            "sqs:ReceiveMessage"
            "sqs:DeleteMessage"
            "sqs:GetQueueAttributes"
        ]
        resources [ myQueueArn ]
    }
}
```

### Publish to SNS Topic

```fsharp
lambda "NotificationSender" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    policyStatement {
        effect Effect.ALLOW
        actions [ "sns:Publish" ]
        resources [ myTopicArn ]
    }
}
```

## VPC Security

Network isolation and security.

```fsharp
// ✅ GOOD: Properly segmented VPC
vpc "SecureVpc" {
    maxAzs 2
    
    // Subnet configuration (done via CDK)
    // - Public subnets: NAT Gateways, Load Balancers
    // - Private subnets: Lambda, ECS, RDS
    // - Isolated subnets: Highly sensitive resources
    
    // NAT Gateways for private subnet internet access
    natGateways 2  // One per AZ for HA
    
    // Enable VPC Flow Logs for monitoring
    // (Note: Would need to be configured separately)
}

// Place sensitive resources in private subnets
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
    vpcSubnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
    securityGroups [ restrictedSecurityGroup ]
}
```

## Monitoring and Auditing

Enable CloudTrail and monitoring.

```fsharp
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
rdsInstance "MonitoredDatabase" {
    vpc myVpc
    postgresEngine
    enablePerformanceInsights true
}
```

## Secrets Management

Never hardcode secrets!

```fsharp
// ❌ BAD: Hardcoded secrets
lambda "BadFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    environment [
        "DB_PASSWORD", "mypassword123"  // Never do this!
    ]
}

// ✅ GOOD: Use Secrets Manager or Parameter Store
lambda "GoodFunction" {
    runtime Runtime.DOTNET_8
    handler "MyApp::Handler"
    code "./publish"
    
    environment [
        "DB_SECRET_ARN", dbSecretArn  // Reference to secret
    ]
    
    // Grant permission to read secret
    policyStatement {
        effect Effect.ALLOW
        actions [ "secretsmanager:GetSecretValue" ]
        resources [ dbSecretArn ]
    }
}

// RDS can generate and manage secrets automatically
rdsInstance "SecureDatabase" {
    vpc myVpc
    postgresEngine
    
    // Credentials automatically stored in Secrets Manager
    credentials Credentials.FromGeneratedSecret("admin")
}
```

## Compliance Considerations

### GDPR/Data Privacy

```fsharp
// Enable encryption for data at rest
bucket "UserData" {
    encryption BucketEncryption.KMS_MANAGED  // Customer managed keys
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
```

### HIPAA/PHI Data

```fsharp
// Use KMS encryption for sensitive data
bucket "HealthRecords" {
    encryption BucketEncryption.KMS_MANAGED
    versioned true
    removalPolicy RemovalPolicy.RETAIN
}

// Enable audit trails
rdsInstance "HealthDatabase" {
    vpc myVpc
    postgresEngine
    storageEncrypted true
    backupRetentionDays 30.0  // Longer retention for compliance
    deletionProtection true
}
```

## Security Checklist

Before deploying to production:

- [ ] All security groups follow least privilege
- [ ] All S3 buckets block public access (unless specifically required)
- [ ] All databases use encryption at rest
- [ ] All data transfer uses encryption in transit (TLS 1.2+)
- [ ] No secrets hardcoded in code or environment variables
- [ ] MFA enabled for sensitive operations
- [ ] CloudTrail enabled for audit logging
- [ ] VPC Flow Logs enabled
- [ ] Automated backups configured
- [ ] Deletion protection enabled for production resources
- [ ] IAM roles follow least privilege
- [ ] Regular security reviews scheduled

## Additional Resources

- [AWS Well-Architected Framework - Security Pillar](https://docs.aws.amazon.com/wellarchitected/latest/security-pillar/welcome.html)
- [AWS Security Best Practices](https://aws.amazon.com/architecture/security-identity-compliance/)
- [Yan Cui's Serverless Security Best Practices](https://theburningmonk.com/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
