(**
---
title: GitHub Actions OIDC deploy roles
category: Best Practices
categoryindex: 3
description: Deploy CDK stacks from GitHub Actions without long-lived AWS access keys
---

# ![IAM](img/icons/Arch_AWS-Identity-and-Access-Management_48.png) GitHub Actions OIDC deploy roles

GitHub Actions can assume an IAM role through OpenID Connect (OIDC): each workflow run gets a short-lived token from
GitHub, and AWS STS exchanges it for temporary credentials. No access keys are stored in GitHub secrets, and the
role's trust policy decides exactly which repository, branch, tag or environment may deploy.

`githubActionsRole` builds that role with a trust policy pinned to:

- `token.actions.githubusercontent.com:aud` = `sts.amazonaws.com` (change with `audience`)
- `token.actions.githubusercontent.com:sub` = one of the subjects built from `repository` plus `branch`, `tag`,
  `environment` or `pullRequests`

FsCDK refuses a role that only names a repository (any workflow of the repo could assume it) and wildcard
repositories such as `mycorp/*`.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.IAM

(**
## A deploy role for `cdk deploy`

`cdkDeployAccess` lets the role assume the CDK bootstrap roles (deploy, file and image publishing, lookup) of the
deploying account. That is all `cdk deploy` needs: CloudFormation then runs with the bootstrap execution role, so the
GitHub role itself gets no direct access to your resources.
*)

stack "CiRoles" {
    githubActionsRole "platform-deploy" {
        repository "mycorp/platform"
        branch "main"
        environment "production"
        cdkDeployAccess
        maxSessionDuration (Duration.Hours(1.0))
    }
}

(**
### Branches and environments are alternatives

With GitHub's default subject format, a job that uses an environment gets the subject
`repo:OWNER/REPO:environment:NAME` instead of `repo:OWNER/REPO:ref:refs/heads/BRANCH`. So the role above trusts
**either** pushes to `main` **or** jobs in the `production` environment, not only jobs that satisfy both. To require
both, restrict the environment's deployment branches to `main` in the repository's environment settings.

## Tags, wildcards and pull requests

Branch and tag names accept wildcards (`release/*`, `v*`); FsCDK then uses `StringLike` for the subject condition
instead of `StringEquals`. `pullRequests` trusts `pull_request` runs, which can come from any branch of the
repository, so anyone who can open a pull request can use the role. Give such a role the least you can: for
`cdk diff`, only the CDK bootstrap **lookup role**, which is read-only and denies `kms:Decrypt`. Avoid broad managed
policies such as `ReadOnlyAccess`, which can read data (S3 objects, DynamoDB items), not just configuration.
*)

stack "ReleaseRoles" {
    githubActionsRole "release" {
        repository "mycorp/platform"
        tag "v*"
        cdkDeployAccess
    }

    githubActionsRole "pr-diff" {
        repository "mycorp/platform"
        pullRequests
        description "cdk diff on pull requests: may only assume the CDK lookup role"

        policyStatement (
            PolicyStatement(
                PolicyStatementProps(
                    Actions = [| "sts:AssumeRole" |],
                    Resources = [| $"arn:{Aws.PARTITION}:iam::{Aws.ACCOUNT_ID}:role/cdk-hnb659fds-lookup-role-*" |]
                )
            )
        )
    }
}

(**
## The OIDC provider

An AWS account has one IAM OIDC provider per issuer URL. By default the role references the account's existing
`arn:aws:iam::ACCOUNT:oidc-provider/token.actions.githubusercontent.com`, so several stacks can create roles
without fighting over the provider.

If the provider does not exist yet, create it once with `oidcProvider` and pass it with `provider` (or pass an
existing provider's ARN with `providerArn`):
*)

stack "GitHubOidc" {
    let! github =
        oidcProvider "GitHub" {
            url OIDCProviders.GitHubActionsUrl
            clientId OIDCProviders.GitHubActionsClientId
        }

    githubActionsRole "infra-deploy" {
        provider github
        repository "mycorp/infra"
        branch "main"
        cdkDeployAccess
    }
}

(**
## Using the role in a workflow

Output the role ARN (bind the role with `let!` and use `cfnOutput`) and use it in the workflow:

```yaml
permissions:
  id-token: write   # required to request the OIDC token
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/platform-deploy
          aws-region: eu-west-2
      - run: npx cdk deploy --require-approval never
```

## Other operations

| Operation | Purpose |
|-----------|---------|
| `policyStatement` | Add an inline statement for anything beyond `cdk deploy` (for example `s3:PutObject` for a manual upload step). |
| `managedPolicy` | Attach a managed policy. |
| `permissionsBoundary` | Limit the role with a permissions boundary. |
| `audience` | Use a custom token audience. |
| `constructId` | Set the construct ID (the role name stays the builder name). |

## Related documentation

- [IAM Best Practices](iam-best-practices.html)
- [CloudFront static sites and SPAs](cloudfront-static-site.html) - a typical stack deployed this way
*)
