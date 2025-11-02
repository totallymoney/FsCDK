(**
---
title: DynamoDB Tables
category: docs
index: 23
---

# Amazon DynamoDB

Amazon DynamoDB is a fully managed NoSQL database service that provides fast and predictable performance
with seamless scalability. DynamoDB lets you offload the administrative burdens of operating and scaling
a distributed database.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB

(**
## Basic Table

Create a simple DynamoDB table with a partition key.
*)

stack "BasicDynamoDB" {
    table "Users" {
        partitionKey "userId" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
    }
}

(**
## Table with Sort Key

Create a table with both partition and sort keys for complex queries.
*)

stack "TableWithSortKey" {
    table "Orders" {
        partitionKey "customerId" AttributeType.STRING
        sortKey "orderDate" AttributeType.NUMBER
        billingMode BillingMode.PAY_PER_REQUEST
    }
}

(**
## Table with Provisioned Capacity

Use provisioned capacity for predictable workloads.
*)

(**
Note: Provisioned capacity configuration must be done using the CDK Table construct directly.
*)

(**
## Table with Point-in-Time Recovery

Enable point-in-time recovery for production tables.
*)

stack "TableWithPITR" {
    table "ProductionData" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        pointInTimeRecovery true
        removalPolicy RemovalPolicy.RETAIN
    }
}

(**
## Table with DynamoDB Streams

Enable DynamoDB Streams for change data capture.
*)

stack "TableWithStreams" {
    table "Events" {
        partitionKey "eventId" AttributeType.STRING
        sortKey "timestamp" AttributeType.NUMBER
        billingMode BillingMode.PAY_PER_REQUEST
        stream StreamViewType.NEW_AND_OLD_IMAGES
    }
}

(**
## Development Table

Optimized settings for development and testing.
*)

stack "DevTable" {
    table "DevData" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        removalPolicy RemovalPolicy.DESTROY
    }
}

(**
## Best Practices

### Performance

- Use partition keys with high cardinality to distribute load
- Design sort keys to support query patterns
- Use sparse indexes for infrequent attributes
- Enable DAX (DynamoDB Accelerator) for read-heavy workloads
- Use batch operations for bulk reads/writes
- Implement exponential backoff for retries

### Security

- Enable encryption at rest (enabled by default)
- Use IAM roles for fine-grained access control
- Enable point-in-time recovery for production
- Use VPC endpoints for private access
- Audit access with CloudTrail
- Implement least-privilege IAM policies

### Cost Optimization

- Use PAY_PER_REQUEST for unpredictable workloads
- Use PROVISIONED capacity for steady, predictable traffic
- Enable auto-scaling for provisioned capacity
- Use TTL to automatically delete expired items
- Monitor unused indexes and remove them
- Use DynamoDB Standard-IA for infrequently accessed data

### Reliability

- Enable point-in-time recovery for production tables
- Use global tables for multi-region applications
- Set appropriate removal policies (RETAIN for production)
- Monitor capacity metrics and throttling
- Implement circuit breakers for downstream failures

### Operational Excellence

- Use descriptive table names with purpose
- Tag tables with project and environment
- Monitor CloudWatch metrics (throttles, capacity)
- Set up alarms for capacity and errors
- Document partition and sort key design
- Version your table schemas
*)

(**
## Billing Modes

### PAY_PER_REQUEST (On-Demand)
- No capacity planning required
- Pay per request
- Great for unpredictable workloads
- No minimum fees
- Automatically scales

### PROVISIONED
- Pre-provision read/write capacity
- Lower cost for consistent workloads
- Requires capacity planning
- Can use auto-scaling
- Reserved capacity available for further savings

## Stream View Types

- **KEYS_ONLY**: Only key attributes
- **NEW_IMAGE**: Entire item after modification
- **OLD_IMAGE**: Entire item before modification
- **NEW_AND_OLD_IMAGES**: Both before and after (recommended)

## Resources

- [DynamoDB Documentation](https://docs.aws.amazon.com/dynamodb/)
- [DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)
- [DynamoDB Streams](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html)
- [Data Modeling](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/data-modeling.html)
*)
