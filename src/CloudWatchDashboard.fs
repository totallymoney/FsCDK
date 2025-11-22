namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CloudWatch

// ============================================================================
// CloudWatch Dashboard Configuration DSL
// ============================================================================

/// <summary>
/// High-level CloudWatch Dashboard builder following AWS monitoring best practices.
///
/// **Default Settings:**
/// - Dashboard name = construct name
/// - Period for metrics = 5 minutes (good balance of granularity and cost)
/// - Widgets arranged in rows and columns
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework operational excellence pillar:
/// - Clear dashboard names improve discoverability
/// - 5-minute periods provide good visibility without excessive cost
/// - Structured layout improves readability
///
/// **Best Practices:**
/// - Organize related metrics together
/// - Use alarms to highlight critical issues
/// - Include both system and business metrics
/// - Set appropriate Y-axis ranges for clarity
///
/// **Escape Hatch:**
/// Access the underlying CDK Dashboard via the `Dashboard` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type DashboardConfig =
    { DashboardName: string
      ConstructId: string option
      DashboardName_: string option
      Widgets: IWidget list list // List of rows, each row is a list of widgets
      DefaultInterval: Duration option
      PeriodOverride: PeriodOverride option
      Start: string option
      End: string option }

type DashboardSpec =
    { DashboardName: string
      ConstructId: string
      Props: DashboardProps
      mutable Dashboard: Dashboard option }

    /// Gets the underlying Dashboard resource. Must be called after the stack is built.
    member this.Resource =
        match this.Dashboard with
        | Some dashboard -> dashboard
        | None ->
            failwith
                $"Dashboard '{this.DashboardName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type DashboardBuilder(name: string) =
    member _.Yield(_: unit) : DashboardConfig =
        { DashboardName = name
          ConstructId = None
          DashboardName_ = None
          Widgets = []
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None }

    member _.Yield(widget: IWidget) : DashboardConfig =
        { DashboardName = name
          ConstructId = None
          DashboardName_ = None
          Widgets = [ [ widget ] ]
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None }

    member _.Zero() : DashboardConfig =
        { DashboardName = name
          ConstructId = None
          DashboardName_ = None
          Widgets = []
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DashboardConfig) : DashboardConfig = f ()

    member inline x.For(config: DashboardConfig, [<InlineIfLambda>] f: unit -> DashboardConfig) : DashboardConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: DashboardConfig, b: DashboardConfig) : DashboardConfig =
        { DashboardName = a.DashboardName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          DashboardName_ =
            match a.DashboardName_ with
            | Some _ -> a.DashboardName_
            | None -> b.DashboardName_
          Widgets = a.Widgets @ b.Widgets
          DefaultInterval =
            match a.DefaultInterval with
            | Some _ -> a.DefaultInterval
            | None -> b.DefaultInterval
          PeriodOverride =
            match a.PeriodOverride with
            | Some _ -> a.PeriodOverride
            | None -> b.PeriodOverride
          Start =
            match a.Start with
            | Some _ -> a.Start
            | None -> b.Start
          End =
            match a.End with
            | Some _ -> a.End
            | None -> b.End }

    member _.Run(config: DashboardConfig) : DashboardSpec =
        let props = DashboardProps()
        let constructId = config.ConstructId |> Option.defaultValue config.DashboardName

        // AWS Best Practice: Use meaningful dashboard names
        props.DashboardName <- config.DashboardName_ |> Option.defaultValue config.DashboardName

        // Convert widget rows to CDK format
        if not (List.isEmpty config.Widgets) then
            let widgetRows =
                config.Widgets |> List.map (fun row -> row |> List.toArray) |> List.toArray

            props.Widgets <- widgetRows

        config.DefaultInterval
        |> Option.iter (fun interval -> props.DefaultInterval <- interval)

        config.PeriodOverride |> Option.iter (fun po -> props.PeriodOverride <- po)

        config.Start |> Option.iter (fun s -> props.Start <- s)
        config.End |> Option.iter (fun e -> props.End <- e)

        { DashboardName = config.DashboardName
          ConstructId = constructId
          Props = props
          Dashboard = None }

    /// <summary>Sets the construct ID for the dashboard.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DashboardConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the dashboard name as it appears in CloudWatch.</summary>
    [<CustomOperation("dashboardName")>]
    member _.DashboardName(config: DashboardConfig, name: string) =
        { config with
            DashboardName_ = Some name }

    /// <summary>Adds a single widget to the dashboard.</summary>
    [<CustomOperation("widget")>]
    member _.Widget(config: DashboardConfig, widget: IWidget) =
        { config with
            Widgets = config.Widgets @ [ [ widget ] ] }

    /// <summary>Adds a row of widgets to the dashboard.</summary>
    [<CustomOperation("widgetRow")>]
    member _.WidgetRow(config: DashboardConfig, widgets: IWidget list) =
        { config with
            Widgets = config.Widgets @ [ widgets ] }

    /// <summary>Sets the default time range interval for metrics.</summary>
    [<CustomOperation("defaultInterval")>]
    member _.DefaultInterval(config: DashboardConfig, interval: Duration) =
        { config with
            DefaultInterval = Some interval }

    /// <summary>Sets how metrics periods are rendered.</summary>
    [<CustomOperation("periodOverride")>]
    member _.PeriodOverride(config: DashboardConfig, periodOverride: PeriodOverride) =
        { config with
            PeriodOverride = Some periodOverride }

    /// <summary>Sets the start time for the dashboard.</summary>
    [<CustomOperation("startTime")>]
    member _.StartTime(config: DashboardConfig, start: string) = { config with Start = Some start }

    /// <summary>Sets the end time for the dashboard.</summary>
    [<CustomOperation("endTime")>]
    member _.EndTime(config: DashboardConfig, end_: string) = { config with End = Some end_ }

// ============================================================================
// Helper module for common dashboard widgets
// ============================================================================

module DashboardWidgets =
    /// Creates a metric widget (line graph)
    let metricWidget (title: string) (metrics: IMetric list) =
        GraphWidget(props = GraphWidgetProps(Title = title, Left = (metrics |> List.toArray)))

    /// Creates a metric widget with both left and right Y-axes
    let metricWidgetDualAxis (title: string) (leftMetrics: IMetric list) (rightMetrics: IMetric list) =
        GraphWidget(
            props =
                GraphWidgetProps(
                    Title = title,
                    Left = (leftMetrics |> List.toArray),
                    Right = (rightMetrics |> List.toArray)
                )
        )

    /// Creates a single value widget (shows latest metric value)
    let singleValueWidget (title: string) (metrics: IMetric list) =
        SingleValueWidget(props = SingleValueWidgetProps(Title = title, Metrics = (metrics |> List.toArray)))

    /// Creates a text widget with markdown content
    let textWidget (markdown: string) =
        TextWidget(props = TextWidgetProps(Markdown = markdown))

    /// Creates an alarm widget
    let alarmWidget (alarm: IAlarm) =
        AlarmWidget(props = AlarmWidgetProps(Alarm = alarm))

    /// Creates an alarm widget
    let alarmWidgetSpec (alarmSpec: CloudWatchAlarmSpec) =
        match alarmSpec.Alarm with
        | Some alarm -> AlarmWidget(props = AlarmWidgetProps(Alarm = alarm))
        | None ->
            // Todo: This should carry the process forward and resolve it on Run instead of here.
            failwith $"Sorry, a new alarm ({alarmSpec.AlarmName}) from a new spec not implemented yet."

    /// Creates a log query widget
    let logQueryWidget
        (title: string)
        (logGroupNames: string list)
        (queryString: string)
        (width: int option)
        (height: int option)
        =
        let props = LogQueryWidgetProps()
        props.Title <- title
        props.LogGroupNames <- logGroupNames |> List.toArray
        props.QueryString <- queryString
        width |> Option.iter (fun w -> props.Width <- w)
        height |> Option.iter (fun h -> props.Height <- h)
        LogQueryWidget(props)

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CloudWatchDashboardBuilders =
    /// <summary>Creates a CloudWatch Dashboard with AWS monitoring best practices.</summary>
    /// <param name="name">The dashboard name.</param>
    /// <code lang="fsharp">
    /// dashboard "MyAppDashboard" {
    ///     dashboardName "my-app-production"
    ///     defaultInterval (Duration.Minutes(5.0))
    ///     widgetRow [
    ///         DashboardWidgets.metricWidget "API Requests" [requestMetric]
    ///         DashboardWidgets.metricWidget "Error Rate" [errorMetric]
    ///     ]
    ///     widgetRow [
    ///         DashboardWidgets.singleValueWidget "Current Users" [userMetric]
    ///     ]
    /// }
    /// </code>
    let dashboard (name: string) = DashboardBuilder name
