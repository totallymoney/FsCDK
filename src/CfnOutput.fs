namespace FsCDK

open Amazon.CDK

// ============================================================================
// CloudFormation Output Configuration DSL
// ============================================================================

/// <summary>
/// A CloudFormation stack output (shown by `cdk deploy`, readable with `aws cloudformation describe-stacks`).
/// Outputs are visible to anyone who can read the stack: never output secrets.
/// </summary>
type CfnOutputConfig =
    { OutputName: string
      ConstructId: string option
      Value: string option
      Description: string option
      ExportName: string option }

type CfnOutputSpec =
    { OutputName: string
      ConstructId: string
      Props: CfnOutputProps
      mutable Output: CfnOutput option }

type CfnOutputBuilder(name: string) =
    member _.Yield(_: unit) : CfnOutputConfig =
        { OutputName = name
          ConstructId = None
          Value = None
          Description = None
          ExportName = None }

    member x.Zero() : CfnOutputConfig = x.Yield()

    member inline _.Delay([<InlineIfLambda>] f: unit -> CfnOutputConfig) : CfnOutputConfig = f ()

    member _.Combine(state1: CfnOutputConfig, state2: CfnOutputConfig) : CfnOutputConfig =
        { OutputName = state1.OutputName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Value = state2.Value |> Option.orElse state1.Value
          Description = state2.Description |> Option.orElse state1.Description
          ExportName = state2.ExportName |> Option.orElse state1.ExportName }

    member inline x.For(config: CfnOutputConfig, [<InlineIfLambda>] f: unit -> CfnOutputConfig) : CfnOutputConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CfnOutputConfig) : CfnOutputSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.OutputName
        let props = CfnOutputProps()

        props.Value <-
            match config.Value with
            | Some v -> v
            | None -> invalidArg "value" $"CfnOutput '{config.OutputName}' needs a value."

        config.Description |> Option.iter (fun d -> props.Description <- d)
        config.ExportName |> Option.iter (fun e -> props.ExportName <- e)

        { OutputName = config.OutputName
          ConstructId = constructId
          Props = props
          Output = None }

    /// <summary>Sets the construct ID (also the output's logical name).</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CfnOutputConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the output value (tokens such as api.Url are allowed).</summary>
    /// <param name="value">The value.</param>
    [<CustomOperation("value")>]
    member _.Value(config: CfnOutputConfig, value: string) = { config with Value = Some value }

    /// <summary>Sets the description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: CfnOutputConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Exports the value for Fn::ImportValue in other stacks of the same account and region.</summary>
    /// <param name="exportName">The export name.</param>
    [<CustomOperation("exportName")>]
    member _.ExportName(config: CfnOutputConfig, exportName: string) =
        { config with
            ExportName = Some exportName }

[<AutoOpen>]
module CfnOutputBuilders =
    /// <summary>Creates a CloudFormation stack output.</summary>
    /// <param name="name">The output name.</param>
    /// <code lang="fsharp">
    /// cfnOutput "ApiUrl" {
    ///     value api.Url
    ///     description "HTTP API endpoint"
    /// }
    /// </code>
    let cfnOutput (name: string) = CfnOutputBuilder name
