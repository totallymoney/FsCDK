# Serverless web sample

A single-page app and an authenticated .NET API defined with FsCDK:

- **ServerlessWebEdge** (us-east-1): CloudFront-scope WAF web ACL (AWS managed rules, per-IP rate limit) logging to `aws-waf-logs-serverless-web` with the `authorization` and `cookie` headers redacted.
- **ServerlessWeb** (eu-west-2):
  - private S3 bucket (SSE-S3) served by CloudFront through Origin Access Control, SPA routing with a CloudFront Function on the default behavior only, asset upload with invalidation
  - `/api/*` behavior forwarding to an HTTP API (Authorization in the cache key so the JWT reaches API Gateway, zero TTLs)
  - HTTP API with a Cognito user pool JWT authorizer, default-stage throttling and JSON access logs
  - F# Lambda on the managed `dotnet8` runtime with SnapStart, routed through its published version
  - DynamoDB table (on-demand) with read/write granted to the Lambda
  - 5xx alarm to an SNS topic, GitHub Actions OIDC deploy role, stack outputs

WAF cannot be attached to HTTP APIs, so the API is protected by WAF only when it is called through CloudFront.
The web ACL is in us-east-1 because CloudFront requires it; `crossRegionReferences` passes its ARN to the
eu-west-2 stack through SSM.

## Before deploying

- **WAF bypass:** the API's `execute-api` endpoint stays reachable directly, skipping CloudFront and WAF (the
  JWT authorizer and stage throttling still apply). To close it, require a secret header that CloudFront adds
  (custom origin header checked by a Lambda authorizer), or use a regional REST API, which WAF supports directly.
- **Bucket encryption:** CloudFront OAC cannot decrypt objects under the AWS managed `aws/s3` KMS key (the FsCDK
  bucket default), so the site bucket uses SSE-S3. Use a customer managed key (`encryptionKey`) if you need KMS;
  CDK then grants CloudFront `kms:Decrypt` in the key policy.
- **Cognito app client:** the FsCDK `userPoolClient` defaults to SRP sign-in with OAuth disabled. For the
  hosted UI set `oAuth` (code flow with PKCE, your real callback URLs).

## Synthesize

```bash
dotnet publish Api -c Release
cd Infra
npx aws-cdk synth
```

Set `CDK_DEFAULT_ACCOUNT` (or use an AWS profile) for a real account; without it a placeholder account is used.
Replace `web/dist` with your SPA build output and `mycorp/serverless-web` with your repository before deploying.
