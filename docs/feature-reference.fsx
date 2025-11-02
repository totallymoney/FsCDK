(**
---
title: Complete Feature Reference
category: docs
index: 30
---

# FsCDK Complete Feature Reference

Comprehensive reference of all AWS services and features available in FsCDK.

## Core Framework

### Stack and Application
- **Stack Builder** - Define CloudFormation stacks with type-safe DSL
- **App** - CDK application configuration
- **Environment** - AWS account/region configuration
- **Tags** - Resource tagging across stacks
- **Custom Resources** - Custom CloudFormation resources

Documentation: [Getting Started](getting-started-extended.html)

---

## Compute Services

### AWS Lambda
- Lambda Functions with production-safe defaults
- Docker Image Functions
- Lambda Powertools integration
- Function Versions
- Function URLs
- Lambda Filesystem (EFS)
- Event Source Mapping
- Event Invoke Config
- Function Permissions

Documentation: 
- [Lambda Quickstart](lambda-quickstart.html)
- [Lambda Production Defaults](lambda-production-defaults.html)
- [Lambda Powertools](lambda-powertools.html)

### Containers
- **ECS** - Container orchestration with Fargate support
- **EKS** - Kubernetes cluster management
- **App Runner** - Fully managed container service
- **ECR** - Container image registry

Documentation: 
- [ECS/EC2](ec2-ecs.html)
- [EKS/Kubernetes](eks-kubernetes.html)
- [ECR Repository](ecr-repository.html)

### Virtual Machines
- **EC2** - Virtual machine configuration
- **Bastion Host** - Secure SSH access
- **Elastic Beanstalk** - PaaS for applications

Documentation: [Bastion Host](bastion-host.html)

---

## Networking

### VPC and Networking
- **VPC** - Virtual Private Cloud with Multi-AZ support
- **Subnets** - Subnet selection and configuration
- **Security Groups** - Least-privilege security
- **Route Tables** - Custom routing
- **VPC Gateway Attachment** - Internet/NAT gateways
- **Elastic IP** - Static IP addresses

Features:
- Multi-AZ support
- DNS enabled by default
- NAT gateways
- Security groups with no outbound by default

### Load Balancing
- **Application Load Balancer (ALB)** - HTTP/HTTPS load balancer
  - HTTP/2 support
  - Drop invalid headers
  - Target Groups
  - Listeners (HTTP/HTTPS)
- **Network Load Balancer** - TCP/UDP load balancer

Documentation:
- [ALB, Secrets, Route53](alb-secrets-route53.html)
- [Network Load Balancer](network-load-balancer.html)

### DNS
- **Route53** - DNS service
- **Route53 Record Set** - DNS record management

---

## API Services

### API Gateway
- **API Gateway V2 (HTTP API)** - Low-latency HTTP API
  - CORS support
  - JWT/Lambda authorizers
  - Auto-deploy
  - Cost-effective (70% cheaper than REST API)

Documentation: [API Gateway V2](api-gateway-v2.html)

### GraphQL
- **AppSync** - Managed GraphQL API
  - GraphQL subscriptions
  - X-Ray tracing
  - DynamoDB/Lambda/HTTP data sources
  - Real-time updates

### Content Delivery
- **CloudFront** - Global CDN
  - HTTP/2 support
  - TLS 1.2 minimum
  - IPv6 enabled
  - Edge caching

---

## Storage Services

### Object Storage
- **S3 Buckets** - Secure object storage
  - Versioning
  - Encryption (KMS)
  - Block public access
  - Lifecycle rules
  - Bucket policies
  - Bucket metrics

Documentation:
- [S3 Quickstart](s3-quickstart.html)
- [Bucket Policy](bucket-policy.html)

---

## Database Services

### NoSQL
- **DynamoDB** - Serverless NoSQL database
  - Pay-per-request or provisioned billing
  - Point-in-time recovery
  - DynamoDB Streams
  - Kinesis integration
  - Import from S3

Documentation: [DynamoDB](dynamodb.html)

### Relational
- **RDS** - Managed relational databases
  - PostgreSQL, MySQL, MariaDB
  - Oracle, SQL Server
  - Multi-AZ support
  - Automated backups
  - Encryption
  - IAM authentication
  - Performance Insights

Documentation: [RDS Database](rds-database.html)

### Document Database
- **DocumentDB** - MongoDB-compatible database

### Caching
- **ElastiCache** - Redis/Memcached caching

---

## Messaging and Events

### Messaging
- **SNS** - Pub/sub messaging
  - Standard and FIFO topics
  - Multiple subscription types
  - Message filtering
- **SQS** - Message queuing
  - Standard and FIFO queues
  - Dead-letter queues
  - Visibility timeout

Documentation: [SNS and SQS Messaging](sns-sqs-messaging.html)

### Event Processing
- **EventBridge** - Event-driven architecture
  - Scheduled events (cron)
  - Event patterns
  - Multiple targets
- **Kinesis Streams** - Real-time data streaming
  - Encryption enabled
  - Shard-level processing

Documentation:
- [EventBridge](eventbridge.html)
- [Kinesis Streams](kinesis-streams.html)

---

## Orchestration

### Workflows
- **Step Functions** - State machine orchestration
  - Standard and Express workflows
  - X-Ray tracing
  - Full logging
  - Error handling
  - Parallel execution

Documentation: [Step Functions](step-functions.html)

---

## Security and Identity

### Authentication and Authorization
- **Cognito** - User authentication
  - User Pools
  - Strong password policies
  - MFA support
  - Social identity providers

### Encryption and Secrets
- **KMS** - Key management
- **Secrets Manager** - Secure credential storage
- **Certificate Manager** - SSL/TLS certificates

Documentation:
- [KMS Encryption](kms-encryption.html)
- [Certificate Manager](certificate-manager.html)

### IAM
- **IAM Roles** - Identity and access management
- **Policy Statements** - Fine-grained permissions
- **Managed Policies** - Reusable policies
- **Grants** - Resource access grants
- **OIDC Provider** - Federated identity

Documentation:
- [IAM Best Practices](iam-best-practices.html)
- [Managed Policy](managed-policy.html)

---

## Monitoring and Observability

### Logging
- **CloudWatch Logs** - Log management
  - Retention policies
  - Log groups
  - Structured logging

### Metrics and Dashboards
- **CloudWatch Dashboard** - Metrics visualization

Documentation: [CloudWatch Dashboard](cloudwatch-dashboard.html)

### Tracing
- **X-Ray** - Distributed tracing
  - Enabled by default on Lambda
  - Step Functions integration
  - AppSync integration

### Monitoring
- **CloudWatch Synthetics** - Canary monitoring

---

## Production-Safe Defaults

FsCDK emphasizes **security and reliability by default**:

### Lambda
- X-Ray tracing enabled
- Structured JSON logging
- Auto-DLQ creation
- Reserved concurrency (10)
- Environment encryption (KMS)
- 90-day log retention

### VPC
- Multi-AZ by default
- DNS enabled
- Cost-optimized NAT gateways

### Security Groups
- No outbound traffic by default
- Least privilege principle

### RDS
- Automated backups (7 days)
- Encryption at rest
- Not publicly accessible
- Auto minor version upgrades

### S3
- Block public access
- KMS encryption
- Versioning opt-in

### ALB
- Internal by default
- HTTP/2 enabled
- Drop invalid headers

### ECS/EKS
- Container Insights enabled
- Comprehensive logging
- Latest versions

### Step Functions
- X-Ray tracing enabled
- Full logging (ALL level)
- Timeout protection

### Kinesis
- Encryption enabled
- 24-hour retention

### ECR
- Image scan on push
- Lifecycle policies
- Encryption

---

## Builder Pattern and DSL

All FsCDK resources follow a consistent **Computation Expression (CE) builder pattern**:

```fsharp
resourceType "name" {
    property value
    customOperation value
    nestedBuilder {
        // ...
    }
}
```

**Key Features:**
- Type-safe configuration
- Immutable infrastructure definitions
- Production-safe defaults
- Escape hatches to underlying CDK
- Composable and reusable

---

## Comparison to Alternatives

### vs Farmer
- FsCDK uses AWS CDK (industry standard)
- Access to all AWS services
- Better CloudFormation compatibility
- More mature ecosystem

Documentation: [Comparison to Farmer](comparison-to-farmer.html)

### vs CDK (TypeScript/Python)
- Type-safe F# syntax
- Functional programming patterns
- Better defaults (security-first)
- Immutable configuration
- More concise code

---

## Examples and Guides

### Quickstart Guides
- [Getting Started Extended](getting-started-extended.html)
- [Lambda Quickstart](lambda-quickstart.html)
- [S3 Quickstart](s3-quickstart.html)

### Patterns and Best Practices
- [Multi-Tier Example](multi-tier-example.html)
- [IAM Best Practices](iam-best-practices.html)
- [Lambda Production Defaults](lambda-production-defaults.html)

### Service-Specific Guides
- [ALB, Secrets, Route53](alb-secrets-route53.html)
- [ECS/EC2](ec2-ecs.html)
- [EKS/Kubernetes](eks-kubernetes.html)
- [EventBridge](eventbridge.html)
- [Kinesis Streams](kinesis-streams.html)
- [KMS Encryption](kms-encryption.html)
- [CloudWatch Dashboard](cloudwatch-dashboard.html)
- [Certificate Manager](certificate-manager.html)
- [Bastion Host](bastion-host.html)
- [Bucket Policy](bucket-policy.html)
- [Managed Policy](managed-policy.html)
- [Network Load Balancer](network-load-balancer.html)

### New Features (Recently Added)
- [API Gateway V2 (HTTP API)](api-gateway-v2.html)
- [ECR Repository](ecr-repository.html)
- [Step Functions](step-functions.html)
- [DynamoDB](dynamodb.html)
- [RDS Database](rds-database.html)
- [SNS and SQS Messaging](sns-sqs-messaging.html)

---

## Getting Help

- **GitHub Issues**: [Report bugs or request features](https://github.com/Thorium/FsCDK/issues)
- **Documentation**: Browse the docs/ directory
- **Examples**: Check the examples/ directory
- **AWS CDK Docs**: [Official AWS CDK documentation](https://docs.aws.amazon.com/cdk/)

---

## Summary

FsCDK provides:

- **60+ service modules** covering major AWS services
- **Type-safe F# DSL** for infrastructure as code
- **Production-safe defaults** following AWS best practices
- **Comprehensive examples** and documentation
- **Full AWS CDK integration** with escape hatches
- **Built for .NET 8** and modern F#

Perfect for teams that want the power of AWS CDK with the elegance and safety of F#.
*)
