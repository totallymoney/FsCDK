(**
---
title: CloudTrail Audit Logging
category: Resources
categoryindex: 5
---

# ![CloudTrail](img/icons/Arch_AWS-CloudTrail_48.png) AWS CloudTrail - Audit Logging

AWS CloudTrail provides governance, compliance, and audit capabilities for your AWS account. CloudTrail logs,
continuously monitors, and retains account activity related to actions across your AWS infrastructure.

**Security Best Practice:** Per O'Reilly "Security as Code" - "Log all API calls with CloudTrail. This is non-negotiable for security monitoring."

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.CloudTrail
open Amazon.CDK.AWS.Logs

(**
## Basic CloudTrail (Secure Defaults)

Create a CloudTrail with all security best practices enabled by default.
*)

stack "BasicCloudTrail" {
    cloudTrail "SecurityAudit" {
        // All secure defaults are automatically applied:
        // - Multi-region trail (captures all regions)
        // - Global service events included (IAM, STS, CloudFront)
        // - Log file validation enabled (integrity checking)
        // - CloudWatch Logs integration enabled
        // - S3 bucket auto-created with encryption
        ()
    }
}

(**
## Production CloudTrail

Production-ready configuration with custom retention and monitoring.
*)

stack "ProductionCloudTrail" {
    cloudTrail "ProductionAudit" {
        // Multi-region and global events (defaults)
        isMultiRegionTrail true
        includeGlobalServiceEvents true

        // Log integrity validation (detect tampering)
        enableFileValidation true

        // CloudWatch Logs for real-time monitoring
        sendToCloudWatchLogs true
        cloudWatchLogsRetention RetentionDays.THREE_MONTHS

        // Log all management events (default)
        managementEvents ReadWriteType.ALL
    }
}

(**
## Organization Trail

For AWS Organizations, create an organization-wide trail that logs events for all accounts.
*)

stack "OrganizationCloudTrail" {
    cloudTrail "OrgSecurityAudit" {
        isOrganizationTrail true
        isMultiRegionTrail true
        includeGlobalServiceEvents true
        enableFileValidation true
        cloudWatchLogsRetention RetentionDays.SIX_MONTHS
    }
}

(**
## Custom S3 Bucket

Use a custom S3 bucket for CloudTrail logs (with lifecycle rules for cost optimization).
*)

stack "CustomBucketCloudTrail" {
    // Create custom S3 bucket with lifecycle rules
    let trailBucket =
        s3Bucket "CloudTrailLogs" {
            versioned true // Enable versioning for audit trail integrity

            lifecycleRule {
                transitions
                    [ transition {
                          storageClass Amazon.CDK.AWS.S3.StorageClass.GLACIER
                          transitionAfter (Duration.Days(90.0))
                      }
                      transition {
                          storageClass Amazon.CDK.AWS.S3.StorageClass.DEEP_ARCHIVE
                          transitionAfter (Duration.Days(365.0))
                      } ]
            }
        }

    cloudTrail "CustomBucketAudit" {
        s3Bucket trailBucket
        isMultiRegionTrail true
        includeGlobalServiceEvents true
    }
}

(**
## Compliance Trail (Long Retention)

For compliance requirements (HIPAA, PCI-DSS, SOC2), use longer retention periods.
*)

stack "ComplianceCloudTrail" {
    cloudTrail "ComplianceAudit" {
        isMultiRegionTrail true
        includeGlobalServiceEvents true
        enableFileValidation true

        // Extended retention for compliance
        cloudWatchLogsRetention RetentionDays.ONE_YEAR

        // All management events
        managementEvents ReadWriteType.ALL
    }
}

(**
## Read-Only Trail (Cost Optimization)

For read-only monitoring, log only read operations (reduces volume and cost).
*)

stack "ReadOnlyCloudTrail" {
    cloudTrail "ReadOnlyAudit" {
        isMultiRegionTrail true
        includeGlobalServiceEvents true

        // Only log read operations
        managementEvents ReadWriteType.READ_ONLY

        // Shorter retention for cost savings
        cloudWatchLogsRetention RetentionDays.ONE_WEEK
    }
}

(**
## Write-Only Trail (Security Focus)

For security monitoring, focus on write operations (changes to infrastructure).
*)

stack "SecurityFocusCloudTrail" {
    cloudTrail "SecurityMonitoring" {
        isMultiRegionTrail true
        includeGlobalServiceEvents true
        enableFileValidation true

        // Only log write/delete operations
        managementEvents ReadWriteType.WRITE_ONLY

        cloudWatchLogsRetention RetentionDays.ONE_MONTH
    }
}

(**
## Single Region Trail (Cost Optimization)

For single-region applications, save costs with a single-region trail.
*)

stack "SingleRegionCloudTrail" {
    cloudTrail "RegionalAudit" {
        // Single region only (cost optimization)
        isMultiRegionTrail false

        // Still include global service events
        includeGlobalServiceEvents true
        enableFileValidation true

        cloudWatchLogsRetention RetentionDays.TWO_WEEKS
    }
}

(**
## Disabling CloudWatch Logs (S3 Only)

For cost-conscious deployments, disable CloudWatch Logs and use S3 only.
*)

stack "S3OnlyCloudTrail" {
    cloudTrail "S3OnlyAudit" {
        isMultiRegionTrail true
        includeGlobalServiceEvents true
        enableFileValidation true

        // Disable CloudWatch Logs (S3 only)
        sendToCloudWatchLogs false
    }
}

(**
## Best Practices

### Security

- ✅ **Always enable multi-region trails** - Capture events from all regions
- ✅ **Include global service events** - Monitor IAM, STS, CloudFront changes
- ✅ **Enable log file validation** - Detect if logs have been tampered with
- ✅ **Use CloudWatch Logs** - Enable real-time monitoring and alerting
- ✅ **Enable for all accounts** - Use organization trails for centralized logging
- ✅ **Monitor CloudTrail health** - Set up CloudWatch alarms for delivery failures
- ✅ **Restrict S3 bucket access** - Only CloudTrail service should write logs

### Compliance

CloudTrail is **required** for most compliance frameworks:

- **HIPAA** - Audit trail of all access to ePHI
- **PCI-DSS** - Requirement 10.2 - Audit trail for all system components
- **SOC2** - CC7.2 - Monitoring of system components
- **GDPR** - Article 32 - Security of processing (audit logs)
- **FedRAMP** - AU-2 - Audit Events
- **ISO 27001** - A.12.4.1 - Event logging

### Cost Optimization

- **First trail is FREE** - Management events in primary region
- **Additional trails** - $2.00 per 100,000 events
- **CloudWatch Logs** - $0.50 per GB ingested (optional)
- **S3 storage** - Standard S3 rates
- **Data events** - $0.10 per 100,000 events (S3, Lambda)

**Cost Reduction Strategies:**
1. Use read-only or write-only trails if appropriate
2. Disable CloudWatch Logs for low-priority environments
3. Use S3 lifecycle policies to transition to Glacier
4. Use single-region trails for region-specific applications
5. Filter out high-volume, low-value events

### Monitoring & Alerting

Set up CloudWatch alarms for:

- **Failed log delivery** - Alert if CloudTrail can't write logs
- **Log file validation failures** - Alert if log integrity is compromised
- **Unauthorized API calls** - Alert on suspicious activity
- **IAM policy changes** - Alert on privilege escalation attempts
- **Security group changes** - Alert on network exposure changes
- **Root account usage** - Alert on root account API calls

### Incident Investigation

CloudTrail is essential for security incident response:

1. **Who** - Identity of the caller (IAM user, role, federated user)
2. **What** - The action that was attempted
3. **When** - Date and time of the request
4. **Where** - Source IP address and region
5. **Why** - Event outcome (success or failure)

### Data Events (Advanced)

For S3 and Lambda, you can enable data event logging (additional cost):

```fsharp
// Note: Data events require additional configuration
// and are not included in the free tier
```

## Default Settings

FsCDK CloudTrail applies these security best practices by default:

- **Multi-Region Trail:** ✅ true (captures all regions)
- **Global Service Events:** ✅ true (IAM, STS, CloudFront, etc.)
- **Log File Validation:** ✅ true (integrity checking)
- **Management Events:** ✅ ReadWriteType.ALL (all API calls)
- **CloudWatch Logs:** ✅ true (real-time monitoring)
- **CloudWatch Retention:** 📅 ONE_MONTH (balance cost/security)
- **S3 Bucket:** 🪣 Auto-created with encryption
- **Organization Trail:** ❌ false (opt-in for AWS Organizations)

## What is Logged?

### Management Events (Always Logged)

- IAM user/role creation, deletion, policy changes
- EC2 instance start/stop/terminate
- VPC creation, security group changes
- S3 bucket creation, policy changes
- Lambda function creation, updates
- RDS instance creation, configuration changes
- CloudFormation stack operations
- And thousands of other AWS API calls

### Global Service Events

- **IAM** - User, role, policy changes
- **AWS STS** - Temporary credential requests
- **CloudFront** - Distribution changes
- **Route53** - DNS changes (global operations)
- **AWS Organizations** - Account management

### NOT Logged by Default

- **S3 object-level operations** (GET, PUT, DELETE) - Requires data events
- **Lambda invocations** - Requires data events
- **DynamoDB operations** - Requires data events
- **CloudWatch Logs data** - Not logged
- **Service-specific logs** (RDS query logs, VPC Flow Logs, etc.)

## Log Format Example

```json
{
  "eventVersion": "1.08",
  "userIdentity": {
    "type": "IAMUser",
    "principalId": "AIDAI...",
    "arn": "arn:aws:iam::123456789012:user/alice",
    "accountId": "123456789012",
    "userName": "alice"
  },
  "eventTime": "2025-11-08T10:30:00Z",
  "eventSource": "ec2.amazonaws.com",
  "eventName": "RunInstances",
  "awsRegion": "us-east-1",
  "sourceIPAddress": "203.0.113.1",
  "requestParameters": {
    "instanceType": "t3.micro",
    "imageId": "ami-12345678"
  },
  "responseElements": {
    "instancesSet": [...]
  }
}
```

## Integration with Other Services

### CloudWatch Logs Insights

Query CloudTrail logs using CloudWatch Logs Insights:

```sql
fields @timestamp, userIdentity.userName, eventName, sourceIPAddress
| filter eventName = "RunInstances"
| sort @timestamp desc
| limit 20
```

### AWS Config

CloudTrail + AWS Config = Complete compliance solution:
- **CloudTrail** - Who did what, when
- **AWS Config** - Current and historical configuration state

### Amazon Athena

Query CloudTrail logs in S3 using SQL:

```sql
SELECT useridentity.username, eventname, count(*) as count
FROM cloudtrail_logs
WHERE eventtime > '2025-11-01'
GROUP BY useridentity.username, eventname
ORDER BY count DESC
```

### AWS Security Hub

CloudTrail findings are automatically sent to Security Hub for centralized security management.

## Resources

- [AWS CloudTrail Documentation](https://docs.aws.amazon.com/cloudtrail/)
- [CloudTrail Best Practices](https://docs.aws.amazon.com/awscloudtrail/latest/userguide/best-practices-security.html)
- [CloudTrail Log File Validation](https://docs.aws.amazon.com/awscloudtrail/latest/userguide/cloudtrail-log-file-validation-intro.html)
- [CloudTrail Pricing](https://aws.amazon.com/cloudtrail/pricing/)
- [Analyzing CloudTrail Logs with Athena](https://docs.aws.amazon.com/athena/latest/ug/cloudtrail-logs.html)

## Summary

CloudTrail is **non-negotiable** for:
- ✅ Security incident investigation
- ✅ Compliance requirements
- ✅ Operational troubleshooting
- ✅ Detecting unauthorized access
- ✅ Audit trails for regulated industries

FsCDK makes it easy with secure-by-default settings. Simply add `cloudTrail "name" { }` to your stack!
*)
