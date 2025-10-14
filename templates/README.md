# FsCDK Templates

Project templates for building AWS Lambda applications with the AWS CDK using F# and FsCDK.

These templates scaffold a solution similar to the "Highlights" structure: a Lambda function project, a CDK app that deploys it, Expecto-based tests, optional local DynamoDB via Docker Compose, and a CI pipeline using `build.fsx`.

## Installation

Install the templates from a local checkout (useful while developing):

```sh
dotnet new install .
```

Or from NuGet (once published):

```sh
dotnet new install FsCDK.Templates
```

## Create a new app

```sh
dotnet new fscdk-lambda -n NewApp
```

This creates the following structure:

- NewApp.sln
- Directory.Packages.props (central package versions)
- build.fsx (CI pipeline: build, test, publish, cdk synth)
- .editorconfig
- .config/dotnet-tools.json (fsdocs, fantomas, lambda tools)
- docker-compose.yml (DynamoDB local + integration tests)
- .github/workflows/build.yml (GitHub Actions pipeline)
- src/
  - NewApp/ (F# Lambda handler project)
    - Handler.fs
    - NewApp.fsproj
- cdk/
  - NewApp.CDK/ (CDK app using FsCDK)
    - Program.fs
    - NewApp.CDK.fsproj
  - cdk.json
- tests/
  - NewApp.UnitTests/ (Expecto unit tests)
  - NewApp.IntegrationTests/ (Expecto integration tests)
  - NewApp.FakeAPI/ (minimal ASP.NET Core app with /healthcheck)

## Build, test, and synth

From the template root:

```sh
dotnet fsi build.fsx
```

This will:
- Restore tools
- Build `src` and `cdk`
- Run tests in `tests`
- Publish the Lambda project
- Run `cdk synth`

You can also run individual steps manually:
- Build Lambda: `dotnet build src -c Release`
- Run tests: `dotnet test tests -c Release`
- Publish Lambda: `dotnet publish src/NewApp -c Release -f net8.0`
- CDK synth (from cdk folder): `npx cdk synth`

## Local DynamoDB and integration tests

A docker-compose file is included to run DynamoDB Local and integration tests:

```sh
docker compose up -d dynamodb init-dynamo
# Run integration tests
docker compose run --rm integration_tests
```

## Customizing package versions

The template uses central package management and exposes version parameters via `.template.config/template.json`.
You can override them when creating a new app, for example:

```sh
dotnet new fscdk-lambda \
  --FsCDKPkgVersion 1.2.3 \
  --FSharpCorePkgVersion 8.0.403 \
  --AmazonLambdaCorePkgVersion 2.7.0
```

See `templates/content/blank/.template.config/template.json` for the full list of available parameters.

## License

MIT