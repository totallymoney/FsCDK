namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Lambda VersionOptions Builder DSL
// ============================================================================

type VersionOptionsConfig =
    { Description: string option
      RemovalPolicy: RemovalPolicy option
      CodeSha256: string option }

type VersionOptionsBuilder() =
    member _.Yield _ : VersionOptionsConfig =
        { Description = None
          RemovalPolicy = None
          CodeSha256 = None }

    member _.Zero() : VersionOptionsConfig =
        { Description = None
          RemovalPolicy = None
          CodeSha256 = None }

    member _.Combine(a: VersionOptionsConfig, b: VersionOptionsConfig) : VersionOptionsConfig =
        { Description =
            if a.Description.IsSome then
                a.Description
            else
                b.Description
          RemovalPolicy =
            if a.RemovalPolicy.IsSome then
                a.RemovalPolicy
            else
                b.RemovalPolicy
          CodeSha256 = if a.CodeSha256.IsSome then a.CodeSha256 else b.CodeSha256 }

    member inline _.Delay(f: unit -> VersionOptionsConfig) = f ()
    member inline x.For(state: VersionOptionsConfig, f: unit -> VersionOptionsConfig) = x.Combine(state, f ())

    member _.Run(cfg: VersionOptionsConfig) : VersionOptions =
        let o = VersionOptions()
        cfg.Description |> Option.iter (fun d -> o.Description <- d)
        cfg.RemovalPolicy |> Option.iter (fun rp -> o.RemovalPolicy <- rp)
        cfg.CodeSha256 |> Option.iter (fun s -> o.CodeSha256 <- s)
        o

    [<CustomOperation("description")>]
    member _.Desc(cfg: VersionOptionsConfig, d: string) = { cfg with Description = Some d }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(cfg: VersionOptionsConfig, rp: RemovalPolicy) = { cfg with RemovalPolicy = Some rp }

    [<CustomOperation("codeSha256")>]
    member _.CodeSha256(cfg: VersionOptionsConfig, sha: string) = { cfg with CodeSha256 = Some sha }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module FunctionVersionBuilders =
    let versionOptions = VersionOptionsBuilder()
