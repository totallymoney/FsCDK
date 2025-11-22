namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Lambda Event Invoke Config Types
// ============================================================================

type EventInvokeConfigSpec = { Options: IEventInvokeConfigOptions }

// ============================================================================
// Lambda Event Invoke Config Options Builder DSL
// ============================================================================

type EventInvokeConfigOptionsConfig =
    { MaxEventAge: Duration option
      RetryAttempts: int option }

type EventInvokeConfigOptionsBuilder() =
    member _.Yield(_: unit) : EventInvokeConfigOptionsConfig =
        { MaxEventAge = None
          RetryAttempts = None }

    member _.Zero() : EventInvokeConfigOptionsConfig =
        { MaxEventAge = None
          RetryAttempts = None }

    member _.Run(config: EventInvokeConfigOptionsConfig) : EventInvokeConfigSpec =
        let o = EventInvokeConfigOptions()
        config.MaxEventAge |> Option.iter (fun d -> o.MaxEventAge <- d)
        config.RetryAttempts |> Option.iter (fun r -> o.RetryAttempts <- r)
        { Options = o :> IEventInvokeConfigOptions }

    [<CustomOperation("maxEventAge")>]
    member _.MaxEventAge(config: EventInvokeConfigOptionsConfig, duration: Duration) =
        { config with
            MaxEventAge = Some duration }

    [<CustomOperation("retryAttempts")>]
    member _.RetryAttempts(config: EventInvokeConfigOptionsConfig, attempts: int) =
        { config with
            RetryAttempts = Some attempts }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module EventInvokeConfigBuilders =
    let eventInvokeConfigOptions = EventInvokeConfigOptionsBuilder()
    let configureAsyncInvoke = EventInvokeConfigOptionsBuilder()
