# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- No unreleased changes

## [0.1.0] - 2025-10-15
### Added
- Initial release of FsCDK Templates with the `FsCDK Lambda App` (short name: `fscdk-lambda`).
- Scaffolds a solution with:
  - `src/NewApp` (F# Lambda handler)
  - `cdk/NewApp.CDK` (CDK app using FsCDK)
  - `tests` (Expecto Unit and Integration tests) and a minimal `NewApp.FakeAPI` (/healthcheck)
  - Central package management (Directory.Packages.props)
  - `build.fsx` CI pipeline and a GitHub Actions workflow
  - `docker-compose.yml` for local DynamoDB and integration tests
  - `.editorconfig` and `.config/dotnet-tools.json`
### Changed
- Updated packaging project and README to reflect FsCDK and AWS CDK focus.
