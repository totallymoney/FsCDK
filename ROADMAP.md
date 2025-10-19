# FsCDK Roadmap

This document outlines the planned features and priorities for FsCDK, inspired by the success of [Farmer](https://compositionalit.github.io/farmer/) for Azure and extending similar principles to AWS CDK.

## Vision

FsCDK aims to be the premier F# library for AWS infrastructure as code, combining:
- **Type Safety**: Leverage F#'s strong type system to catch configuration errors at compile time
- **Security by Default**: Follow AWS Well-Architected Framework principles with sensible secure defaults
- **Composability**: Build reusable infrastructure components using F#'s functional programming features
- **Escape Hatches**: Provide access to underlying AWS CDK constructs for advanced scenarios
- **Developer Experience**: Offer intuitive builders and excellent IDE support

## Milestones

### Phase 1: Core Builders ✅ (Current)

**Priority: High** | **Status: In Progress**

Core infrastructure builders with security best practices:

- [x] **Storage**
  - [x] S3 buckets with KMS encryption by default
  - [x] Lifecycle rules helpers
  - [x] Block public access by default
  
- [x] **Compute**
  - [x] Lambda functions with encrypted environment variables
  - [x] Configurable log retention (90 days default)
  - [x] Memory/timeout defaults (512MB/30s)
  
- [x] **Security**
  - [x] IAM role/policy helpers following least-privilege
  - [x] Lambda execution role generator
  - [x] S3/DynamoDB access role helpers
  
- [x] **Observability**
  - [x] CloudTrail with encrypted logs
  - [x] CloudWatch alarm templates (Lambda errors, RDS CPU, ALB 5xx)
  - [x] GuardDuty/Config documentation
  
- [x] **Meta**
  - [x] Global tagging helpers
  - [x] Standard tag conventions (project, environment, owner, created-by)

### Phase 2: Extended AWS Services

**Priority: High** | **Status: Planned**

Expand coverage to additional AWS services:

- [ ] **Networking** (Enhanced)
  - [ ] VPC peering helpers
  - [ ] PrivateLink/VPC endpoints
  - [ ] Transit Gateway support
  - [ ] Network ACLs with templates
  
- [ ] **Container Services**
  - [ ] ECS/Fargate task definitions
  - [ ] EKS cluster with node groups
  - [ ] ECR repositories with lifecycle policies
  - [ ] App Runner services
  
- [ ] **API Gateway**
  - [ ] REST API builder
  - [ ] HTTP API builder
  - [ ] WebSocket API builder
  - [ ] API Gateway v2 with Lambda integration
  
- [ ] **Event-Driven**
  - [ ] EventBridge rules and targets
  - [ ] Step Functions state machines
  - [ ] SNS topics with encryption
  - [ ] SQS queues with DLQ
  
- [ ] **Data Services**
  - [ ] DynamoDB streams integration
  - [ ] Aurora Serverless v2
  - [ ] ElastiCache (Redis/Memcached)
  - [ ] OpenSearch Service

### Phase 3: Advanced Features

**Priority: Medium** | **Status: Planned**

Advanced infrastructure patterns and tooling:

- [ ] **Stack Composition**
  - [ ] Multi-stack applications
  - [ ] Cross-stack references
  - [ ] Stack set management
  - [ ] Nested stacks support
  
- [ ] **Testing**
  - [ ] Snapshot testing utilities
  - [ ] Fine-grained assertions
  - [ ] Integration test helpers
  - [ ] LocalStack support
  
- [ ] **CI/CD Integration**
  - [ ] CodePipeline builders
  - [ ] CodeBuild project templates
  - [ ] GitHub Actions workflows
  - [ ] GitLab CI templates
  
- [ ] **Monitoring & Alerting**
  - [ ] X-Ray tracing integration
  - [ ] CloudWatch dashboards
  - [ ] Composite alarms
  - [ ] SNS alert subscriptions

### Phase 4: Developer Experience

**Priority: Medium** | **Status: Planned**

Tooling and documentation improvements:

- [ ] **Documentation**
  - [ ] Comprehensive API reference
  - [ ] Best practices guide
  - [ ] Architecture decision records
  - [ ] Video tutorials
  
- [ ] **CLI Tools**
  - [ ] Project scaffolding
  - [ ] Diff visualization
  - [ ] Deployment helpers
  - [ ] Resource explorer
  
- [ ] **IDE Integration**
  - [ ] Code snippets
  - [ ] Live templates
  - [ ] Quick fixes
  - [ ] IntelliSense enhancements

### Phase 5: Ecosystem Integration

**Priority: Low** | **Status: Future**

Integration with broader F# and AWS ecosystem:

- [ ] **Farmer Migration**
  - [ ] Azure → AWS mapping guide
  - [ ] Migration toolkit
  - [ ] Side-by-side comparison
  - [ ] Automated translation tools
  
- [ ] **Multi-Cloud**
  - [ ] Pulumi F# integration
  - [ ] Terraform CDK F# support
  - [ ] Cross-cloud abstractions
  
- [ ] **Community**
  - [ ] Plugin system
  - [ ] Community builders registry
  - [ ] Shared stack marketplace
  - [ ] Contribution templates

## Priority Definitions

- **High**: Core functionality needed for production use
- **Medium**: Valuable features that improve developer experience
- **Low**: Nice-to-have features for specific use cases

## Contributing

We welcome contributions! Areas where you can help:

1. **New Builders**: Implement builders for additional AWS services
2. **Documentation**: Write guides, tutorials, and examples
3. **Testing**: Add tests for existing builders
4. **Bug Fixes**: Identify and fix issues
5. **Feedback**: Share your experience and suggest improvements

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Versioning

FsCDK follows [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to existing APIs
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

## Release Schedule

- **Monthly**: Patch releases with bug fixes
- **Quarterly**: Minor releases with new features
- **Yearly**: Major releases with breaking changes (as needed)

## Feedback & Discussion

- GitHub Issues: Bug reports and feature requests
- GitHub Discussions: General questions and ideas
- Discord: Real-time chat with maintainers and community

---

Last Updated: 2025-01-19
