namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

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

    member _.Run(config: EventInvokeConfigOptionsConfig) =
        let opts = EventInvokeConfigOptions()
        config.MaxEventAge |> Option.iter (fun d -> opts.MaxEventAge <- d)
        config.RetryAttempts |> Option.iter (fun r -> opts.RetryAttempts <- r)

        opts

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
