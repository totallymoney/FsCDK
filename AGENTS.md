# AGENTS.md - AI Coding Agent Guide for FsCDK

This document is designed to help AI coding agents understand the FsCDK project structure, architecture patterns, and coding conventions. Follow these guidelines when working on the codebase.

## Project Overview

**FsCDK** is an F# library for AWS Cloud Development Kit (CDK) that provides type-safe, functional-first infrastructure-as-code capabilities. It wraps AWS CDK constructs with F# computation expression builders following consistent patterns.

## Key Architectural Patterns

### 1. Builder Pattern (Computation Expressions)

All AWS resources are defined using F# computation expressions with a three-layer pattern:

#### Layer 1: Config Record (Mutable-Free)
```fsharp
type ResourceConfig = 
    { ResourceName: string
      ConstructId: string option
      Property1: Type1 option
      Property2: Type2 option }
```

- Immutable record type
- All properties except the name are `option` types
- Used during builder composition (spec-build time)
- Should never contain CDK types that require jsii initialization

#### Layer 2: Spec Record (With Mutable CDK Reference)
```fsharp
type ResourceSpec =
    { ResourceName: string
      ConstructId: string
      Property1: Type1 option
      Property2: Type2 option
      mutable Resource: IResource option }
```

- Contains resolved values (no option for required fields)
- Has a mutable field to store the created CDK resource
- Created by the builder's `Run` method
- Used for cross-resource references

#### Layer 3: Builder Class
```fsharp
type ResourceBuilder(name: string) =
    member _.Yield _ : ResourceConfig = { /* defaults */ }
    member _.Zero() : ResourceConfig = { /* defaults */ }
    member inline _.Delay([<InlineIfLambda>] f: unit -> ResourceConfig) = f()
    member _.Combine(state1, state2) = { /* merge with Option.orElse */ }
    member inline _.For(config, [<InlineIfLambda>] f) = /* ... */
    member _.Run(config: ResourceConfig) : ResourceSpec = { /* convert */ }
    
    [<CustomOperation("propertyName")>]
    member _.PropertyName(config: ResourceConfig, value: Type) =
        { config with PropertyName = Some value }
```

### 2. Cross-Resource References (Bind Pattern)

Use the monadic `let!` syntax for resource dependencies instead of creating ResourceRef types:

```fsharp
stack "MyStack" {
    let! myVpc = vpc "MyVpc" { maxAzs 2 }
    
    securityGroup "MySG" {
        vpc myVpc  // myVpc is IVpc, not VpcSpec
        description "Security group"
    }
}
```

**Implementation in StackBuilder:**
```fsharp
member inline this.Bind(spec: VpcSpec, cont: IVpc -> StackConfig) =
    // Create VPC operation
    let createVpc = fun (stack: Stack) ->
        let vpc = Vpc(stack, spec.ConstructId, spec.Props)
        spec.Vpc <- Some vpc
    
    // Execute continuation with created VPC
    let executeCont = fun (stack: Stack) ->
        match spec.Vpc with
        | Some vpc -> 
            let contConfig = cont vpc
            for op in contConfig.Operations do
                op stack
        | None -> failwith $"VPC not created"
    
    let baseCfg = this.Yield(spec)
    { baseCfg with Operations = [createVpc; executeCont] }
```

**Note:** Keep existing SpecRef patterns (e.g., SecurityGroupRef) for backwards compatibility.

### 3. Stack Processing (Stack.fs)

Unified resource creation:

```fsharp
// All operations are functions: (Stack -> unit)
for op in config.Operations do
    op stack  // Execute each operation in order
```

**Operations are now functions:**
```fsharp
type StackConfig = {
    // ...
    Operations: (Stack -> unit) list  // Unified list of functions
}

// Converting Operation to function:
let opToFunc op = fun stack -> StackOperations.processOperation stack op
```

## File Organization

### Source Files (src/)

Files are ordered in `FsCDK.fsproj` based on dependencies. Key principles:
- Lower-level utilities come first (Environment.fs, PolicyStatement.fs, etc.)
- VPC.fs comes early (defines SecurityGroupRef used by many resources)
- SQS.fs must come before Function.fs (Function uses QueueRef)
- Stack.fs comes near the end (imports all resource types)
- App.fs comes last (top-level entry point)

### Common File Structure Pattern

Each resource file typically contains:
1. `open` statements for AWS CDK namespaces
2. Config record type (immutable, for builder)
3. Spec record type (with `mutable Resource: IResource option` if supporting `let!`)
4. Builder class with standard members (`Yield`, `Zero`, `Delay`, `Combine`, `For`, `Run`)
5. Custom operations with XML documentation
6. Export function: `let resource name = ResourceBuilder(name)`

## Coding Conventions

### 1. XML Documentation

All custom operations must have XML doc comments:

```fsharp
/// <summary>Sets the timeout for the Lambda function in seconds.</summary>
/// <param name="seconds">Timeout value in seconds (max 900).</param>
/// <code lang="fsharp">
/// lambda "MyFunction" {
///     timeout 30.0
/// }
/// </code>
[<CustomOperation("timeout")>]
member _.Timeout(config: FunctionConfig, seconds: float) =
    { config with Timeout = Some seconds }
```

### 2. Construct ID Handling

Always provide a default construct ID:
```fsharp
let constructId = config.ConstructId |> Option.defaultValue config.ResourceName
```

### 3. Option Handling in Combine

Use `Option.orElse` to prefer `state2` over `state1`:
```fsharp
member _.Combine(state1: ResourceConfig, state2: ResourceConfig) =
    { ResourceName = state1.ResourceName
      Property1 = state2.Property1 |> Option.orElse state1.Property1
      Property2 = state2.Property2 |> Option.orElse state1.Property2 }
```

### 4. Duration Conversion

Convert float seconds to CDK Duration in:
- Stack.fs when creating props (for resources created there)
- Builder's Run method (for resources passed to other builders)

```fsharp
// In Stack.fs or Run method:
config.Timeout |> Option.iter (fun t -> props.Timeout <- Duration.Seconds(t))
```

### 5. List Handling

Collections in Config should be `list`, converted to arrays in Stack.fs:
```fsharp
// In Config:
SecurityGroups: SecurityGroupRef list

// In Stack.fs:
if not (List.isEmpty config.SecurityGroups) then
    props.SecurityGroups <- 
        config.SecurityGroups 
        |> List.map VpcHelpers.resolveSecurityGroupRef
        |> Array.ofList
```

### 6. Error Handling

Fail fast with descriptive errors:
```fsharp
props.Handler <- 
    match config.Handler with
    | Some h -> h
    | None -> failwith "Lambda handler is required"
```

## Common Mistakes to Avoid

### 1. Wrong Compilation Order

**Problem:** Type not found errors
**Solution:** Check `FsCDK.fsproj` - files must be ordered so dependencies come first

### 2. Missing Mutable Field in Spec

**Problem:** Can't resolve cross-resource references
**Solution:** Add `mutable Resource: IResource option` to Spec record

### 3. Not Initializing Mutable Fields

**Problem:** Runtime null reference errors
**Solution:** Initialize in Run method: `Resource = None`

### 4. Not Setting Resource After Creation

**Problem:** Cross-references fail
**Solution:** In Stack.fs: `resourceSpec.Resource <- Some resource`

### 5. Using CDK Types at Spec-Build Time

**Problem:** Tests fail with jsii errors
**Solution:** Keep Config/Spec records free of CDK Duration, use `float` for seconds

### 6. Wrong Option Type in Props Assignment

**Problem:** Type mismatch when assigning to props
**Solution:** Use `Option.iter` or pattern matching to unwrap options:
```fsharp
// Correct:
config.Timeout |> Option.iter (fun t -> props.Timeout <- Duration.Seconds(t))

// Wrong:
props.Timeout <- config.Timeout // Type error!
```

## Testing Guidelines

### Test Structure

Tests use Expecto and follow the pattern:
```fsharp
[<Tests>]
let resourceTests =
    testList "ResourceBuilder tests" [
        test "should create resource with required properties" {
            let spec = resource "TestResource" {
                property1 "value1"
                property2 42
            }
            
            Expect.equal spec.ResourceName "TestResource" "Name should match"
            Expect.equal spec.Property1 (Some "value1") "Property1 should be set"
        }
    ]
```

### Snapshot Tests

For CDK resource creation, use snapshot tests:
```fsharp
// Located in tests/SnapshotTests/
let verifyResourceSnapshot name =
    let app = FsCDK.App.app "TestApp" { (*...*) }
    // ... create stack with resource
    let template = Template.FromStack(stackInstance)
    Verifier.Verify(template.ToJSON()).ToTask() |> Async.AwaitTask
```

## Production Best Practices (Yan Cui Patterns)

FsCDK incorporates AWS Lambda production best practices from Yan Cui:

1. **Auto-create DLQ**: Automatically create dead-letter queues for Lambdas
2. **Auto-add Powertools Layer**: Automatically add AWS Lambda Powertools
3. **Event Age & Retry Defaults**: Sensible defaults for event handling
4. **CloudWatch Log Groups**: Pre-create log groups with retention

These are implemented with `autoCreateDLQ` and `autoAddPowertools` flags (default: true).

## Adding New Resources

Checklist for new AWS resources:

1. **Create `NewResource.fs`**
   - [ ] Config record (all options)
   - [ ] Spec record (add `mutable Resource: IResource option` for `let!` support)
   - [ ] Builder class with custom operations
   - [ ] Export: `let newResource name = NewResourceBuilder(name)`

2. **Update Stack.fs**
   - [ ] Add to `Operation` union
   - [ ] Add to `processOperation` (set `spec.Resource <- Some resource`)
   - [ ] Add `Yield` method in StackBuilder
   - [ ] Add `Bind` method if supporting `let!`:
     ```fsharp
     member this.Bind(spec: NewResourceSpec, cont: INewResource -> StackConfig) = ...
     ```

3. **Update FsCDK.fsproj** - Add file in dependency order

4. **Add tests** in `tests/NewResourceTests.fs`

5. **Add docs** in `docs/new-resource.fsx`

## Common CDK Patterns in FsCDK

### Grants (IAM Permissions)

```fsharp
grant {
    principal lambdaFunction
    actions [ "s3:GetObject" ]
    resources [ bucket ]
}
```

### Event Sources

```fsharp
lambda "Processor" {
    runtime Runtime.DOTNET_8
    handler "Handler::process"
    code "./publish"
    
    eventSource (S3EventSource(bucket, 
        EventType = EventType.OBJECT_CREATED))
}
```

### Environment Variables

```fsharp
lambda "Function" {
    environment "TABLE_NAME" dynamoTable.TableName
    environment "BUCKET_NAME" bucket.BucketName
}
```

## Key Files Reference

- **Stack.fs**: Main stack builder and resource processor
- **App.fs**: Application-level configuration
- **Function.fs**: Lambda function builder (good reference implementation)
- **VPC.fs**: VPC, subnets, security groups (shows SpecRef pattern)
- **SQS.fs**: Queue builder (shows SpecRef pattern)
- **DynamoDB.fs**: Table builder (shows complex property handling)
- **Grants.fs**: IAM permission grants

## Building and Testing

```bash
# Build the project
dotnet build src/FsCDK.fsproj

# Run all tests
dotnet test tests/FsCdk.Tests.fsproj

# Run specific test
dotnet test tests/FsCdk.Tests.fsproj --filter "Name~Lambda"

# Build documentation (if using fsdocs)
dotnet fsdocs build --input docs --output docs-output
```

## Version Compatibility

- **.NET**: 8.0
- **F# Core**: Latest stable
- **AWS CDK**: 2.x (Amazon.CDK.Lib)
- **Constructs**: Latest compatible with CDK 2.x

## Additional Resources

- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/v2/guide/home.html)
- [F# Language Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/)
- [Computation Expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)

## Questions or Issues?

When encountering issues:
1. Check file ordering in FsCDK.fsproj
2. Verify SpecRef pattern implementation
3. Ensure mutable Resource fields are initialized and set
4. Check that CDK types aren't used at spec-build time
5. Review similar working implementations (Function.fs, VPC.fs)

---

**Last Updated**: 2025-11-02
**Maintained By**: FsCDK Contributors
