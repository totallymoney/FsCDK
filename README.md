<div align="center">
  <img src="assets/logo/fscdn-logo-constructs.svg" alt="FsCDK Logo" width="800" />
</div>

<div align="center">

[![Build](https://github.com/totallymoney/FsCDK/actions/workflows/build.yml/badge.svg)](https://github.com/totallymoney/FsCDK/actions/workflows/build.yml)

</div>

FsCDK is an F# library for the AWS Cloud Development Kit (CDK), enabling you to define cloud infrastructure using F#'s type safety and functional programming style. It provides F#-friendly builders on top of AWS CDK constructs, so you can compose infrastructure in a concise, declarative way to build reliable and maintainable cloud infrastructure as code.

## Features

- **Type-Safe Infrastructure**: Leverage F#'s strong type system to catch configuration errors at compile time
- **Functional-First Approach**: Use F#'s functional programming features to create reusable and composable infrastructure components
- **Native AWS CDK Integration**: Full access to AWS CDK constructs and patterns with F#-friendly APIs
- **Immutable Infrastructure**: Define your infrastructure using immutable constructs, promoting reliable and predictable deployments
- **IDE Support**: Excellent tooling support with type hints and IntelliSense in your favorite F# IDE

## Quick Start

1) Install the package:
```bash
dotnet add package FsCDK
```

2) Define a simple stack with a bucket and a Lambda function using a single builder API:
```fsharp
open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.Lambda
open FsCDK

let config = Config.get () // e.g., reads AWS account and region from env

stack "MyFirstStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "My first FsCDK stack"

    // S3 bucket
    bucket "MyBucket" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
    }

    // Lambda function
    lambda "HelloFunction" {
        runtime Runtime.DOTNET_8
        handler "Playground::Playground.Handlers::sayHello"
        code "../Playground/bin/Release/net8.0/publish"
        timeout 30.0
        memory 256
        description "A simple hello world lambda"
    }
}
```

3) Synthesize and deploy:
```bash
cdk synth   # Review the generated CloudFormation template
cdk deploy  # Deploy to AWS
```

## Documentation

For detailed documentation, examples, and best practices, visit our [Documentation Site](https://totallymoney.github.io/FsCDK/).

Notes:
- Defaults and configuration come from AWS CDK and your code; review the synthesized template to verify settings meet your needs.
- You can mix FsCDK builders with direct AWS CDK constructs anywhere you need lower-level control.

## Examples

- See the repository’s examples and samples (if available in the tree) for additional patterns such as API gateways, queues, and databases.

## Contributing

Contributions are welcome! Whether it's:
- Reporting a bug
- Submitting a fix
- Proposing new features

Please check out our [Contributing Guide](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
