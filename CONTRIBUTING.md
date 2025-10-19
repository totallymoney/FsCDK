# Contributing to FsCDK

Thank you for your interest in contributing to FsCDK! This document provides guidelines and instructions for contributors.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Building the Project](#building-the-project)
- [Running Tests](#running-tests)
- [Adding New Builders](#adding-new-builders)
- [Coding Style](#coding-style)
- [Submitting Changes](#submitting-changes)
- [Running Examples](#running-examples)

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for AWS CDK CLI)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- Git
- An IDE with F# support (VS Code with Ionide, Visual Studio, or Rider)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/FsCDK.git
   cd FsCDK
   ```
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/Thorium/FsCDK.git
   ```

## Development Setup

### Install Dependencies

```bash
# Restore NuGet packages
dotnet restore

# Install AWS CDK CLI globally
npm install -g aws-cdk@2
```

### Project Structure

```
FsCDK/
├── src/                          # Main library source code
│   ├── FsCDK.fsproj             # Library project file
│   ├── S3.fs, Function.fs, ...  # Existing builders
│   └── FsCDK/                   # New modular builders
│       ├── Storage/             # S3 with enhanced security
│       ├── Compute/             # Lambda with best practices
│       ├── Security/            # IAM helpers
│       ├── Observability/       # CloudWatch, CloudTrail
│       └── Meta/                # Tags and metadata
├── tests/                       # Test suite
│   ├── FsCdk.Tests.fsproj      # Test project
│   └── SnapshotTests/          # Snapshot tests for new modules
├── examples/                    # Example applications
│   ├── s3-quickstart/          # S3 example
│   └── lambda-quickstart/      # Lambda example
├── docs/                        # Documentation
├── ROADMAP.md                  # Project roadmap
└── CONTRIBUTING.md             # This file
```

## Building the Project

### Build the Library

```bash
# Build in Debug mode
dotnet build src/FsCDK.fsproj

# Build in Release mode
dotnet build src/FsCDK.fsproj -c Release
```

### Build Everything

```bash
# Build solution (all projects)
dotnet build FsCDK.sln
```

## Running Tests

### Run All Tests

```bash
# Run all tests
dotnet test tests/FsCdk.Tests.fsproj

# Run with verbose output
dotnet test tests/FsCdk.Tests.fsproj -v n

# Run specific test
dotnet test tests/FsCdk.Tests.fsproj --filter "TestName~S3"
```

### Test Guidelines

- **Unit Tests**: Test individual builder configurations
- **Snapshot Tests**: Verify CloudFormation template output
- **Integration Tests**: (Future) Test deployed resources
- **No AWS Credentials Required**: Tests use CDK synthesis only

All tests should run in CI without AWS credentials. Use `app.Synth()` to generate CloudFormation templates locally.

## Adding New Builders

### Builder Guidelines

When adding a new builder for an AWS service, follow these principles:

1. **Security by Default**: Choose secure defaults (encryption, least-privilege IAM, etc.)
2. **Minimal API**: Start with essential properties, add more as needed
3. **Composability**: Allow builders to be combined and reused
4. **Escape Hatch**: Provide access to underlying CDK construct
5. **Documentation**: Add XML comments explaining defaults and rationale

### Builder Template

```fsharp
namespace FsCDK.ServiceCategory

open Amazon.CDK
open Amazon.CDK.AWS.ServiceName

/// <summary>
/// High-level builder for ServiceName following AWS best practices.
/// 
/// **Default Settings:**
/// - Setting1 = Value1 (rationale)
/// - Setting2 = Value2 (rationale)
/// 
/// **Escape Hatch:**
/// Access the underlying CDK construct via the `Resource` property
/// </summary>
type ServiceConfig =
    { Name: string
      ConstructId: string option
      Property1: Type option
      Property2: Type option }

type ServiceResource =
    { Name: string
      ConstructId: string
      /// The underlying CDK construct - use for advanced scenarios
      Resource: ServiceConstruct }

type ServiceBuilder(name: string) =
    member _.Yield _ : ServiceConfig =
        { Name = name
          ConstructId = None
          Property1 = Some defaultValue1
          Property2 = Some defaultValue2 }

    // Add builder members...

    member _.Run(config: ServiceConfig) : ServiceResource =
        let props = ServiceProps()
        // Set properties...
        { Name = config.Name
          ConstructId = config.ConstructId |> Option.defaultValue name
          Resource = null }

    [<CustomOperation("property1")>]
    member _.Property1(config: ServiceConfig, value: Type) =
        { config with Property1 = Some value }

[<AutoOpen>]
module ServiceBuilders =
    let serviceName name = ServiceBuilder(name)
```

### Adding Tests

Create snapshot tests for your new builder:

```fsharp
module FsCDK.Tests.ServiceNameTests

open Expecto
open FsCDK.ServiceCategory

[<Tests>]
let service_tests =
    testList
        "ServiceName Tests"
        [
            test "serviceName builder applies secure defaults" {
                let resource = serviceName "test-resource" { () }
                Expect.equal resource.Name "test-resource" "Name should be set"
            }
        ]
```

## Coding Style

### F# Style Guidelines

- **Indentation**: 4 spaces (no tabs)
- **Line Length**: 120 characters maximum
- **Naming**:
  - PascalCase for types, modules, members
  - camelCase for parameters, local bindings
  - SCREAMING_CASE for constants
- **Comments**: Use XML doc comments for public APIs
- **Formatting**: Use Fantomas for consistent formatting

### Builder Conventions

- Builder names match AWS service (e.g., `s3Bucket`, `lambdaFunction`)
- Config types end with `Config` (e.g., `S3BucketConfig`)
- Resource types end with `Resource` (e.g., `S3BucketResource`)
- Custom operations use descriptive names (e.g., `versioned`, `memorySize`)

## Submitting Changes

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(storage): add S3 bucket encryption options
fix(compute): correct Lambda timeout default
docs(readme): update quickstart examples
test(s3): add snapshot tests for lifecycle rules
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Test additions/changes
- `refactor`: Code refactoring
- `style`: Formatting changes
- `chore`: Build/tooling changes

### Pull Request Process

1. **Create a Branch**: `git checkout -b feature/your-feature-name`
2. **Make Changes**: Follow coding style and guidelines
3. **Add Tests**: Ensure your changes are tested
4. **Build and Test**: Verify everything works
5. **Commit**: Use conventional commit messages
6. **Push**: Push to your fork
7. **Open PR**: Create a pull request against `main`

### PR Checklist

- [ ] Code builds successfully
- [ ] All tests pass
- [ ] New tests added for new features
- [ ] Documentation updated (if needed)
- [ ] XML doc comments added for public APIs
- [ ] CHANGELOG.md updated (for significant changes)
- [ ] No breaking changes (or clearly documented)

## Running Examples

### S3 Quickstart Example

```bash
cd examples/s3-quickstart
dotnet build
cdk synth   # Generate CloudFormation template
cdk deploy  # Deploy to AWS (requires credentials)
```

### Lambda Quickstart Example

```bash
cd examples/lambda-quickstart
dotnet build
cdk synth
cdk deploy
```

### Creating New Examples

Examples should:
- Demonstrate a specific feature or pattern
- Include a README.md with setup instructions
- Use realistic configurations
- Be runnable with `cdk synth` (no AWS credentials required for synth)

## Code of Conduct

Be respectful, inclusive, and professional. We value diverse perspectives and constructive feedback.

## Getting Help

- **GitHub Issues**: Report bugs or request features
- **GitHub Discussions**: Ask questions or discuss ideas
- **Discord**: Real-time chat with maintainers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to FsCDK! 🚀
