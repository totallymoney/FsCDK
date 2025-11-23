namespace FsCDK

open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM

// ============================================================================
// Lambda Function Permissions Builder DSL
// ============================================================================

type PermissionConfig =
    { Id: string
      Principal: IPrincipal option
      Action: string option
      SourceArn: string option
      SourceAccount: string option
      EventSourceToken: string option }

type PermissionBuilder(id: string) =
    member _.Yield(_: unit) : PermissionConfig =
        { Id = id
          Principal = None
          Action = None
          SourceArn = None
          SourceAccount = None
          EventSourceToken = None }

    member _.Zero() : PermissionConfig =
        { Id = id
          Principal = None
          Action = None
          SourceArn = None
          SourceAccount = None
          EventSourceToken = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PermissionConfig) : PermissionConfig = f ()

    member _.Run(config: PermissionConfig) =
        let p = Permission()

        let principal =
            match config.Principal with
            | Some pr -> pr
            | None -> failwith "permission.principal is required"

        p.Principal <- principal
        config.Action |> Option.iter (fun a -> p.Action <- a)
        config.SourceArn |> Option.iter (fun arn -> p.SourceArn <- arn)
        config.SourceAccount |> Option.iter (fun acc -> p.SourceAccount <- acc)
        config.EventSourceToken |> Option.iter (fun t -> p.EventSourceToken <- t)

        p

    [<CustomOperation("principal")>]
    member _.Principal(config: PermissionConfig, principal: IPrincipal) : PermissionConfig =
        { config with
            Principal = Some principal }

    [<CustomOperation("action")>]
    member _.Action(config: PermissionConfig, action: string) = { config with Action = Some action }

    [<CustomOperation("sourceArn")>]
    member _.SourceArn(config: PermissionConfig, arn: string) = { config with SourceArn = Some arn }

    [<CustomOperation("sourceAccount")>]
    member _.SourceAccount(config: PermissionConfig, account: string) =
        { config with
            SourceAccount = Some account }

    [<CustomOperation("eventSourceToken")>]
    member _.EventSourceToken(config: PermissionConfig, token: string) =
        { config with
            EventSourceToken = Some token }

    member _.Combine(state1: PermissionConfig, state2: PermissionConfig) =
        { Id = state1.Id
          Principal =
            if state1.Principal.IsSome then
                state1.Principal
            else
                state2.Principal
          Action =
            if state1.Action.IsSome then
                state1.Action
            else
                state2.Action
          SourceArn =
            if state1.SourceArn.IsSome then
                state1.SourceArn
            else
                state2.SourceArn
          SourceAccount =
            if state1.SourceAccount.IsSome then
                state1.SourceAccount
            else
                state2.SourceAccount
          EventSourceToken =
            if state1.EventSourceToken.IsSome then
                state1.EventSourceToken
            else
                state2.EventSourceToken }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module FunctionPermissionsBuilders =
    let permission id = PermissionBuilder(id)
