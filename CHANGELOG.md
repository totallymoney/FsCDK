# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- VPC builder with AWS best practices (Multi-AZ support, DNS enabled, cost-optimized NAT gateways)
- Security Group builder with least-privilege defaults (no outbound traffic by default)
- RDS PostgreSQL database builder with:
  - Automated backups (7 days retention by default)
  - Storage encryption enabled by default
  - Multi-AZ deployment support
  - Auto minor version upgrades
  - Configurable deletion protection
- CloudFront CDN distribution builder with:
  - HTTP/2 enabled by default
  - TLS 1.2 minimum protocol version
  - IPv6 enabled by default
  - Cost-optimized price class (PriceClass100)
- Cognito User Pool builder with security best practices:
  - Email/username sign-in by default
  - Auto-verify email
  - Strong password policy (8+ characters, upper, lower, digits, symbols)
  - Self sign-up disabled by default (explicit opt-in for security)
  - Account recovery via email
  - MFA support
- Cognito User Pool Client builder with:
  - SRP auth flow by default
  - Prevents user existence errors
  - Configurable token validities
  - Support for OAuth flows

### Changed
- Enhanced documentation with AWS best practices section
- Updated README with comprehensive examples
- Improved test coverage (74 tests passing)

## [0.1.0] - 2025-09-29
### Added
- Initial release of FsCDK
- Core F# DSL for AWS CDK
- Support for Lambda functions
- Support for S3 buckets
- Support for DynamoDB tables
- Support for SNS topics
- Support for SQS queues
- Template for creating Lambda-based applications
- Comprehensive test suite
- Documentation and examples

[unreleased]: https://github.com/totallymoney/FsCDK/compare/0.1.0...HEAD
[0.1.0]: https://github.com/totallymoney/FsCDK/releases/tag/0.1.0
