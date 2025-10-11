namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Grant/Permissions Configuration DSL
// ============================================================================

// Grant access type
type GrantAccessType =
    | Read
    | Write
    | ReadWrite
    | Custom of (Table -> Function -> unit)

// Grant configuration DSL
type GrantConfig =
    { TableConstructId: string option // Construct ID of the table
      LambdaConstructId: string option // Construct ID of the lambda
      Access: GrantAccessType option }

type GrantSpec =
    { TableConstructId: string
      LambdaConstructId: string
      Access: GrantAccessType }

type GrantBuilder() =
    member _.Yield _ : GrantConfig =
        { TableConstructId = None
          LambdaConstructId = None
          Access = None }

    member _.Zero() : GrantConfig =
        { TableConstructId = None
          LambdaConstructId = None
          Access = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> GrantConfig) : GrantConfig = f ()

    member _.Combine(state1: GrantConfig, state2: GrantConfig) : GrantConfig =
        { TableConstructId = state2.TableConstructId |> Option.orElse state1.TableConstructId
          LambdaConstructId = state2.LambdaConstructId |> Option.orElse state1.LambdaConstructId
          Access = state2.Access |> Option.orElse state1.Access }

    member inline x.For(config: GrantConfig, [<InlineIfLambda>] f: unit -> GrantConfig) : GrantConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: GrantConfig) : GrantSpec =
        match config.TableConstructId, config.LambdaConstructId, config.Access with
        | Some t, Some l, Some a ->
            { TableConstructId = t
              LambdaConstructId = l
              Access = a }
        | _ -> failwith "Grant must specify table construct ID, lambda construct ID, and access type"

    [<CustomOperation("table")>]
    member _.Table(config: GrantConfig, tableConstructId: string) =
        { config with
            TableConstructId = Some tableConstructId }

    [<CustomOperation("lambda")>]
    member _.Lambda(config: GrantConfig, lambdaConstructId: string) =
        { config with
            LambdaConstructId = Some lambdaConstructId }

    [<CustomOperation("readAccess")>]
    member _.ReadAccess(config: GrantConfig) = { config with Access = Some Read }

    [<CustomOperation("writeAccess")>]
    member _.WriteAccess(config: GrantConfig) = { config with Access = Some Write }

    [<CustomOperation("readWriteAccess")>]
    member _.ReadWriteAccess(config: GrantConfig) = { config with Access = Some ReadWrite }

    [<CustomOperation("customAccess")>]
    member _.CustomAccess(config: GrantConfig, grantFunc: Table -> Function -> unit) =
        { config with
            Access = Some(Custom grantFunc) }

module Grants =
    // Grant processing function for Stack builder
    let processGrant (stack: Stack) (grantSpec: GrantSpec) =
        try
            // Look for resources by their construct IDs
            let table = stack.Node.FindChild(grantSpec.TableConstructId) :?> Table
            let lambda = stack.Node.FindChild(grantSpec.LambdaConstructId) :?> Function

            match grantSpec.Access with
            | Read -> table.GrantReadData(lambda) |> ignore
            | Write -> table.GrantWriteData(lambda) |> ignore
            | ReadWrite -> table.GrantReadWriteData(lambda) |> ignore
            | Custom grantFunc -> grantFunc table lambda
        with _ ->
            () // Ignore if resources not found

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module GrantsBuilders =
    let grant = GrantBuilder()
