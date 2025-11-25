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
    { ConstructId: string option
      Widgets: IWidget seq seq
      DefaultInterval: Duration option
      PeriodOverride: PeriodOverride option
      Start: string option
      End: string option
      Variables: IVariable seq }

type DashboardSpec =
    { DashboardName: string
      ConstructId: string
      Props: DashboardProps
      mutable Dashboard: Dashboard option }

type DashboardBuilder(name: string) =
    member _.Yield _ : DashboardConfig =
        { ConstructId = None
          Widgets = []
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None
          Variables = [] }

    member _.Yield(widget: IWidget) : DashboardConfig =
        { ConstructId = None
          Widgets = [ [ widget ] ]
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None
          Variables = [] }

    member _.Zero() : DashboardConfig =
        { ConstructId = None
          Widgets = []
          DefaultInterval = Some(Duration.Minutes(5.0))
          PeriodOverride = None
          Start = None
          End = None
          Variables = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DashboardConfig) : DashboardConfig = f ()

    member inline x.For(config: DashboardConfig, [<InlineIfLambda>] f: unit -> DashboardConfig) : DashboardConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: DashboardConfig, b: DashboardConfig) : DashboardConfig =
        { ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Widgets = Seq.toList a.Widgets @ Seq.toList b.Widgets
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
            | None -> b.End
          Variables = Seq.toList a.Variables @ Seq.toList b.Variables }

    member _.Run(config: DashboardConfig) : DashboardSpec =
        let props = DashboardProps()
        props.DashboardName <- name
        let constructId = config.ConstructId |> Option.defaultValue name

        // Convert widget rows to CDK format
        if not (Seq.isEmpty config.Widgets) then
            let widgetRows =
                config.Widgets |> Seq.map (fun row -> row |> Seq.toArray) |> Seq.toArray

            props.Widgets <- widgetRows

        config.DefaultInterval
        |> Option.iter (fun interval -> props.DefaultInterval <- interval)

        config.PeriodOverride |> Option.iter (fun po -> props.PeriodOverride <- po)

        config.Start |> Option.iter (fun s -> props.Start <- s)
        config.End |> Option.iter (fun e -> props.End <- e)

        if not (Seq.isEmpty config.Variables) then
            props.Variables <- config.Variables |> Seq.toArray

        { DashboardName = name
          ConstructId = constructId
          Props = props
          Dashboard = None }

    /// <summary>Sets the construct ID for the dashboard.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DashboardConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Adds a single widget to the dashboard.</summary>
    [<CustomOperation("widgets")>]
    member _.Widgets(config: DashboardConfig, widgets: IWidget seq) =
        { config with
            Widgets = [ Seq.toList widgets ] }

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
    member _.StartTime(config: DashboardConfig, endTime: string) = { config with Start = Some endTime }

    /// <summary>Sets the end time for the dashboard.</summary>
    [<CustomOperation("endTime")>]
    member _.EndTime(config: DashboardConfig, endTime: string) = { config with End = Some endTime }

type GraphWidgetConfig =
    { End: string option
      Left: IMetric seq
      LeftAnnotations: IHorizontalAnnotation seq
      LeftYAxis: IYAxisProps option
      LegendPosition: LegendPosition option
      LiveData: bool option
      Period: Duration option
      Right: IMetric seq
      RightAnnotations: IHorizontalAnnotation seq
      RightYAxis: IYAxisProps option
      SetPeriodToTimeRange: bool option
      Stacked: bool option
      Start: string option
      Statistic: string option
      VerticalAnnotations: IVerticalAnnotation seq
      View: GraphWidgetView option
      AccountId: string option
      Height: float option
      Region: string option
      Title: string
      Width: float option }

type GraphWidgetBuilder(title: string) =

    member _.Yield(_: unit) : GraphWidgetConfig =
        { Title = title
          Left = []
          Right = []
          End = None
          LeftAnnotations = []
          LeftYAxis = None
          LegendPosition = None
          LiveData = None
          Period = None
          RightAnnotations = []
          RightYAxis = None
          SetPeriodToTimeRange = None
          Stacked = None
          Start = None
          Statistic = None
          VerticalAnnotations = []
          View = None
          AccountId = None
          Height = None
          Region = None
          Width = None }

    member _.Zero() : GraphWidgetConfig =
        { Title = title
          Left = []
          Right = []
          End = None
          LeftAnnotations = []
          LeftYAxis = None
          LegendPosition = None
          LiveData = None
          Period = None
          RightAnnotations = []
          RightYAxis = None
          SetPeriodToTimeRange = None
          Stacked = None
          Start = None
          Statistic = None
          VerticalAnnotations = []
          View = None
          AccountId = None
          Height = None
          Region = None
          Width = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> GraphWidgetConfig) : GraphWidgetConfig = f ()

    member inline x.For
        (
            config: GraphWidgetConfig,
            [<InlineIfLambda>] f: unit -> GraphWidgetConfig
        ) : GraphWidgetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: GraphWidgetConfig, b: GraphWidgetConfig) : GraphWidgetConfig =
        { Title = a.Title
          Left = Seq.toList a.Left @ Seq.toList b.Left
          Right = Seq.toList a.Right @ Seq.toList b.Right
          End =
            match a.End with
            | Some _ -> a.End
            | None -> b.End
          LeftAnnotations = Seq.toList a.LeftAnnotations @ Seq.toList b.LeftAnnotations
          LeftYAxis =
            match a.LeftYAxis with
            | Some _ -> a.LeftYAxis
            | None -> b.LeftYAxis
          LegendPosition =
            match a.LegendPosition with
            | Some _ -> a.LegendPosition
            | None -> b.LegendPosition
          LiveData =
            match a.LiveData with
            | Some _ -> a.LiveData
            | None -> b.LiveData
          Period =
            match a.Period with
            | Some _ -> a.Period
            | None -> b.Period
          RightAnnotations = Seq.toList a.RightAnnotations @ Seq.toList b.RightAnnotations
          RightYAxis =
            match a.RightYAxis with
            | Some _ -> a.RightYAxis
            | None -> b.RightYAxis
          SetPeriodToTimeRange =
            match a.SetPeriodToTimeRange with
            | Some _ -> a.SetPeriodToTimeRange
            | None -> b.SetPeriodToTimeRange
          Stacked =
            match a.Stacked with
            | Some _ -> a.Stacked
            | None -> b.Stacked
          Start =
            match a.Start with
            | Some _ -> a.Start
            | None -> b.Start
          Statistic =
            match a.Statistic with
            | Some _ -> a.Statistic
            | None -> b.Statistic
          VerticalAnnotations = Seq.toList a.VerticalAnnotations @ Seq.toList b.VerticalAnnotations
          View =
            match a.View with
            | Some _ -> a.View
            | None -> b.View
          AccountId =
            match a.AccountId with
            | Some _ -> a.AccountId
            | None -> b.AccountId
          Height =
            match a.Height with
            | Some _ -> a.Height
            | None -> b.Height
          Region =
            match a.Region with
            | Some _ -> a.Region
            | None -> b.Region
          Width =
            match a.Width with
            | Some _ -> a.Width
            | None -> b.Width }


    member _.Run(config: GraphWidgetConfig) : GraphWidget =
        let props = GraphWidgetProps()

        props.Title <- config.Title

        config.End |> Option.iter (fun e -> props.End <- e)

        if not (Seq.isEmpty config.Left) then
            props.Left <- config.Left |> Seq.toArray

        if not (Seq.isEmpty config.LeftAnnotations) then
            props.LeftAnnotations <- config.LeftAnnotations |> Seq.toArray

        config.LeftYAxis |> Option.iter (fun y -> props.LeftYAxis <- y)

        config.LegendPosition |> Option.iter (fun p -> props.LegendPosition <- p)

        config.LiveData |> Option.iter (fun l -> props.LiveData <- l)

        config.Period |> Option.iter (fun p -> props.Period <- p)

        if not (Seq.isEmpty config.Right) then
            props.Right <- config.Right |> Seq.toArray

        if not (Seq.isEmpty config.RightAnnotations) then
            props.RightAnnotations <- config.RightAnnotations |> Seq.toArray

        if not (Seq.isEmpty config.Right) then
            props.Right <- config.Right |> Seq.toArray

        config.RightYAxis |> Option.iter (fun y -> props.RightYAxis <- y)

        config.SetPeriodToTimeRange
        |> Option.iter (fun p -> props.SetPeriodToTimeRange <- p)

        config.Stacked |> Option.iter (fun s -> props.Stacked <- s)

        config.Start |> Option.iter (fun s -> props.Start <- s)

        config.Statistic |> Option.iter (fun s -> props.Statistic <- s)

        if not (Seq.isEmpty config.VerticalAnnotations) then
            props.VerticalAnnotations <- config.VerticalAnnotations |> Seq.toArray

        config.View |> Option.iter (fun v -> props.View <- v)

        config.AccountId |> Option.iter (fun a -> props.AccountId <- a)

        config.Height |> Option.iter (fun h -> props.Height <- h)

        config.Region |> Option.iter (fun r -> props.Region <- r)

        GraphWidget(props)


    [<CustomOperation("end")>]
    member _.End(config: GraphWidgetConfig, endTime: string) = { config with End = Some endTime }

    [<CustomOperation("left")>]
    member _.Left(config: GraphWidgetConfig, metrics: IMetric seq) =
        { config with
            Left = Seq.toList metrics }

    [<CustomOperation("right")>]
    member _.Right(config: GraphWidgetConfig, metrics: IMetric seq) =
        { config with
            Right = Seq.toList metrics }

    [<CustomOperation("stacked")>]
    member _.Stacked(config: GraphWidgetConfig, stacked: bool) = { config with Stacked = Some stacked }

    [<CustomOperation("period")>]
    member _.Period(config: GraphWidgetConfig, period: Duration) = { config with Period = Some period }

    [<CustomOperation("leftYAxis")>]
    member _.LeftYAxis(config: GraphWidgetConfig, yAxis: IYAxisProps) = { config with LeftYAxis = Some yAxis }

    [<CustomOperation("rightYAxis")>]
    member _.RightYAxis(config: GraphWidgetConfig, yAxis: IYAxisProps) = { config with RightYAxis = Some yAxis }

    [<CustomOperation("legendPosition")>]
    member _.LegendPosition(config: GraphWidgetConfig, position: LegendPosition) =
        { config with
            LegendPosition = Some position }

    [<CustomOperation("view")>]
    member _.View(config: GraphWidgetConfig, view: GraphWidgetView) = { config with View = Some view }

    [<CustomOperation("liveData")>]
    member _.LiveData(config: GraphWidgetConfig, liveData: bool) =
        { config with LiveData = Some liveData }

    [<CustomOperation("setPeriodToTimeRange")>]
    member _.SetPeriodToTimeRange(config: GraphWidgetConfig, set: bool) =
        { config with
            SetPeriodToTimeRange = Some set }

    [<CustomOperation("statistic")>]
    member _.Statistic(config: GraphWidgetConfig, statistic: string) =
        { config with
            Statistic = Some statistic }

    [<CustomOperation("accountId")>]
    member _.AccountId(config: GraphWidgetConfig, accountId: string) =
        { config with
            AccountId = Some accountId }

    [<CustomOperation("height")>]
    member _.Height(config: GraphWidgetConfig, height: float) = { config with Height = Some height }

    [<CustomOperation("region")>]
    member _.Region(config: GraphWidgetConfig, region: string) = { config with Region = Some region }

    [<CustomOperation("width")>]
    member _.Width(config: GraphWidgetConfig, width: float) = { config with Width = Some width }

    [<CustomOperation("leftAnnotations")>]
    member _.LeftAnnotations(config: GraphWidgetConfig, annotations: IHorizontalAnnotation seq) =
        { config with
            LeftAnnotations = Seq.toList annotations }


    [<CustomOperation("rightAnnotations")>]
    member _.RightAnnotations(config: GraphWidgetConfig, annotations: IHorizontalAnnotation seq) =
        { config with
            RightAnnotations = Seq.toList annotations }

    [<CustomOperation("verticalAnnotations")>]
    member _.VerticalAnnotations(config: GraphWidgetConfig, annotations: IVerticalAnnotation seq) =
        { config with
            VerticalAnnotations = Seq.toList annotations }

    [<CustomOperation("start")>]
    member _.Start(config: GraphWidgetConfig, startTime: string) = { config with Start = Some startTime }

type AlarmWidgetConfig =
    { Alarm: IAlarm option
      LeftYAxis: IYAxisProps option
      AccountId: string option
      Height: float option
      Region: string option
      Title: string
      Width: float option }


type AlarmWidgetBuilder(title: string) =
    member _.Yield(_: unit) : AlarmWidgetConfig =
        { Title = title
          Alarm = None
          LeftYAxis = None
          AccountId = None
          Height = None
          Region = None
          Width = None }

    member _.Yield(alarm: IAlarm) : AlarmWidgetConfig =
        { Title = title
          Alarm = Some alarm
          LeftYAxis = None
          AccountId = None
          Height = None
          Region = None
          Width = None }


    member _.Zero() : AlarmWidgetConfig =
        { Title = title
          Alarm = None
          LeftYAxis = None
          AccountId = None
          Height = None
          Region = None
          Width = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> AlarmWidgetConfig) : AlarmWidgetConfig = f ()

    member inline x.For
        (
            config: AlarmWidgetConfig,
            [<InlineIfLambda>] f: unit -> AlarmWidgetConfig
        ) : AlarmWidgetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)


    member _.Combine(a: AlarmWidgetConfig, b: AlarmWidgetConfig) : AlarmWidgetConfig =
        { Title = a.Title
          Alarm =
            match a.Alarm with
            | Some _ -> a.Alarm
            | None -> b.Alarm
          LeftYAxis =
            match a.LeftYAxis with
            | Some _ -> a.LeftYAxis
            | None -> b.LeftYAxis
          AccountId =
            match a.AccountId with
            | Some _ -> a.AccountId
            | None -> b.AccountId
          Height =
            match a.Height with
            | Some _ -> a.Height
            | None -> b.Height
          Region =
            match a.Region with
            | Some _ -> a.Region
            | None -> b.Region
          Width =
            match a.Width with
            | Some _ -> a.Width
            | None -> b.Width }


    member _.Run(config: AlarmWidgetConfig) : AlarmWidget =
        let props = AlarmWidgetProps()

        props.Title <- config.Title

        config.Alarm |> Option.iter (fun a -> props.Alarm <- a)

        config.LeftYAxis |> Option.iter (fun y -> props.LeftYAxis <- y)

        config.AccountId |> Option.iter (fun a -> props.AccountId <- a)

        config.Height |> Option.iter (fun h -> props.Height <- h)

        config.Region |> Option.iter (fun r -> props.Region <- r)

        AlarmWidget(props)

    [<CustomOperation("alarm")>]
    member _.Alarm(config: AlarmWidgetConfig, alarm: IAlarm) = { config with Alarm = Some alarm }

    [<CustomOperation("leftYAxis")>]
    member _.LeftYAxis(config: AlarmWidgetConfig, yAxis: IYAxisProps) = { config with LeftYAxis = Some yAxis }

    [<CustomOperation("accountId")>]
    member _.AccountId(config: AlarmWidgetConfig, accountId: string) =
        { config with
            AccountId = Some accountId }

    [<CustomOperation("height")>]
    member _.Height(config: AlarmWidgetConfig, height: float) = { config with Height = Some height }

    [<CustomOperation("region")>]
    member _.Region(config: AlarmWidgetConfig, region: string) = { config with Region = Some region }

    [<CustomOperation("width")>]
    member _.Width(config: AlarmWidgetConfig, width: float) = { config with Width = Some width }


type TextWidgetConfig =
    { Markdown: string option
      Background: TextWidgetBackground option
      Height: float option
      Width: float option }

type TextWidgetBuilder() =
    member _.Yield(_: unit) : TextWidgetConfig =
        { Markdown = None
          Background = None
          Height = None
          Width = None }

    member _.Zero() : TextWidgetConfig =
        { Background = None
          Markdown = None
          Height = None
          Width = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> TextWidgetConfig) : TextWidgetConfig = f ()

    member inline x.For(config: TextWidgetConfig, [<InlineIfLambda>] f: unit -> TextWidgetConfig) : TextWidgetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: TextWidgetConfig, b: TextWidgetConfig) : TextWidgetConfig =
        { Background =
            match a.Background with
            | Some _ -> a.Background
            | None -> b.Background
          Markdown =
            match a.Markdown with
            | Some _ -> a.Markdown
            | None -> b.Markdown
          Height =
            match a.Height with
            | Some _ -> a.Height
            | None -> b.Height
          Width =
            match a.Width with
            | Some _ -> a.Width
            | None -> b.Width }

    member _.Run(config: TextWidgetConfig) : TextWidget =
        let props = TextWidgetProps()

        match config.Markdown with
        | None -> failwith "Markdown content is required for TextWidget"
        | Some value -> props.Markdown <- value

        config.Background |> Option.iter (fun b -> props.Background <- b)

        config.Height |> Option.iter (fun h -> props.Height <- h)

        config.Width |> Option.iter (fun w -> props.Width <- w)

        TextWidget(props)

    [<CustomOperation("markdown")>]
    member _.Markdown(config: TextWidgetConfig, markdown: string) =
        { config with Markdown = Some markdown }

    [<CustomOperation("background")>]
    member _.Background(config: TextWidgetConfig, background: TextWidgetBackground) =
        { config with
            Background = Some background }

    [<CustomOperation("height")>]
    member _.Height(config: TextWidgetConfig, height: float) = { config with Height = Some height }

    [<CustomOperation("width")>]
    member _.Width(config: TextWidgetConfig, width: float) = { config with Width = Some width }

type SingleValueWidgetConfig =
    { Title: string
      Metrics: IMetric seq
      End: string option
      FullPrecision: bool option
      Period: Duration option
      SetPeriodToTimeRange: bool option
      Sparkline: bool option
      Start: string option
      AccountId: string option
      Height: float option
      Width: float option
      Region: string option }

type SingleValueWidgetBuilder(title) =
    member _.Yield(_: unit) : SingleValueWidgetConfig =
        { Title = title
          Metrics = []
          End = None
          FullPrecision = None
          Period = None
          SetPeriodToTimeRange = None
          Sparkline = None
          Start = None
          AccountId = None
          Height = None
          Width = None
          Region = None }

    member _.Zero() : SingleValueWidgetConfig =
        { Title = title
          Metrics = []
          End = None
          FullPrecision = None
          Period = None
          SetPeriodToTimeRange = None
          Sparkline = None
          Start = None
          AccountId = None
          Height = None
          Width = None
          Region = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SingleValueWidgetConfig) : SingleValueWidgetConfig = f ()

    member inline x.For
        (
            config: SingleValueWidgetConfig,
            [<InlineIfLambda>] f: unit -> SingleValueWidgetConfig
        ) : SingleValueWidgetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: SingleValueWidgetConfig, b: SingleValueWidgetConfig) : SingleValueWidgetConfig =
        { Title = a.Title
          Metrics = Seq.toList a.Metrics @ Seq.toList b.Metrics
          End =
            match a.End with
            | Some _ -> a.End
            | None -> b.End
          FullPrecision =
            match a.FullPrecision with
            | Some _ -> a.FullPrecision
            | None -> b.FullPrecision
          Period =
            match a.Period with
            | Some _ -> a.Period
            | None -> b.Period
          SetPeriodToTimeRange =
            match a.SetPeriodToTimeRange with
            | Some _ -> a.SetPeriodToTimeRange
            | None -> b.SetPeriodToTimeRange
          Sparkline =
            match a.Sparkline with
            | Some _ -> a.Sparkline
            | None -> b.Sparkline
          Start =
            match a.Start with
            | Some _ -> a.Start
            | None -> b.Start
          AccountId =
            match a.AccountId with
            | Some _ -> a.AccountId
            | None -> b.AccountId
          Height =
            match a.Height with
            | Some _ -> a.Height
            | None -> b.Height
          Width =
            match a.Width with
            | Some _ -> a.Width
            | None -> b.Width
          Region =
            match a.Region with
            | Some _ -> a.Region
            | None -> b.Region }

    member _.Run(config: SingleValueWidgetConfig) : SingleValueWidget =
        let props = SingleValueWidgetProps()

        props.Title <- config.Title

        if not (Seq.isEmpty config.Metrics) then
            props.Metrics <- config.Metrics |> Seq.toArray
        else
            failwith "At least one metric is required for SingleValueWidget"

        config.End |> Option.iter (fun e -> props.End <- e)

        config.FullPrecision |> Option.iter (fun fp -> props.FullPrecision <- fp)

        config.Period |> Option.iter (fun p -> props.Period <- p)

        config.SetPeriodToTimeRange
        |> Option.iter (fun s -> props.SetPeriodToTimeRange <- s)

        config.Sparkline |> Option.iter (fun s -> props.Sparkline <- s)

        config.Start |> Option.iter (fun s -> props.Start <- s)

        config.AccountId |> Option.iter (fun a -> props.AccountId <- a)

        config.Height |> Option.iter (fun h -> props.Height <- h)

        config.Width |> Option.iter (fun w -> props.Width <- w)

        config.Region |> Option.iter (fun r -> props.Region <- r)

        SingleValueWidget(props)

    [<CustomOperation("metrics")>]
    member _.Metrics(config: SingleValueWidgetConfig, metrics: IMetric seq) =
        { config with
            Metrics = Seq.toList metrics }

    [<CustomOperation("end")>]
    member _.End(config: SingleValueWidgetConfig, endTime: string) = { config with End = Some endTime }

    [<CustomOperation("fullPrecision")>]
    member _.FullPrecision(config: SingleValueWidgetConfig, fullPrecision: bool) =
        { config with
            FullPrecision = Some fullPrecision }

    [<CustomOperation("period")>]
    member _.Period(config: SingleValueWidgetConfig, period: Duration) = { config with Period = Some period }

    [<CustomOperation("setPeriodToTimeRange")>]
    member _.SetPeriodToTimeRange(config: SingleValueWidgetConfig, set: bool) =
        { config with
            SetPeriodToTimeRange = Some set }

    [<CustomOperation("sparkline")>]
    member _.Sparkline(config: SingleValueWidgetConfig, sparkline: bool) =
        { config with
            Sparkline = Some sparkline }

    [<CustomOperation("start")>]
    member _.Start(config: SingleValueWidgetConfig, startTime: string) = { config with Start = Some startTime }

    [<CustomOperation("accountId")>]
    member _.AccountId(config: SingleValueWidgetConfig, accountId: string) =
        { config with
            AccountId = Some accountId }

    [<CustomOperation("height")>]
    member _.Height(config: SingleValueWidgetConfig, height: float) = { config with Height = Some height }

    [<CustomOperation("width")>]
    member _.Width(config: SingleValueWidgetConfig, width: float) = { config with Width = Some width }

    [<CustomOperation("region")>]
    member _.Region(config: SingleValueWidgetConfig, region: string) = { config with Region = Some region }

type LogQueryWidgetConfig =
    { Title: string
      AccountId: string option
      LogGroupNames: string seq
      QueryLanguage: LogQueryLanguage option
      QueryLines: string seq
      QueryString: string option
      Width: int option
      Height: int option
      Region: string option
      View: LogQueryVisualizationType option }

type LogQueryWidgetBuilder(title) =

    member _.Yield(_: unit) : LogQueryWidgetConfig =
        { Title = title
          AccountId = None
          LogGroupNames = []
          QueryLanguage = None
          QueryLines = []
          QueryString = None
          Width = None
          Height = None
          Region = None
          View = None }

    member _.Zero() : LogQueryWidgetConfig =
        { Title = title
          AccountId = None
          LogGroupNames = []
          QueryLanguage = None
          QueryLines = []
          QueryString = None
          Width = None
          Height = None
          Region = None
          View = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LogQueryWidgetConfig) : LogQueryWidgetConfig = f ()

    member inline x.For
        (
            config: LogQueryWidgetConfig,
            [<InlineIfLambda>] f: unit -> LogQueryWidgetConfig
        ) : LogQueryWidgetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: LogQueryWidgetConfig, b: LogQueryWidgetConfig) : LogQueryWidgetConfig =
        { Title = a.Title
          AccountId =
            match a.AccountId with
            | Some _ -> a.AccountId
            | None -> b.AccountId
          LogGroupNames = Seq.toList a.LogGroupNames @ Seq.toList b.LogGroupNames
          QueryLanguage =
            match a.QueryLanguage with
            | Some _ -> a.QueryLanguage
            | None -> b.QueryLanguage
          QueryLines = Seq.toList a.QueryLines @ Seq.toList b.QueryLines
          QueryString =
            match a.QueryString with
            | Some _ -> a.QueryString
            | None -> b.QueryString
          Width =
            match a.Width with
            | Some _ -> a.Width
            | None -> b.Width
          Height =
            match a.Height with
            | Some _ -> a.Height
            | None -> b.Height
          Region =
            match a.Region with
            | Some _ -> a.Region
            | None -> b.Region
          View =
            match a.View with
            | Some _ -> a.View
            | None -> b.View }

    member _.Run(config: LogQueryWidgetConfig) : LogQueryWidget =
        let props = LogQueryWidgetProps()

        props.Title <- config.Title

        if not (Seq.isEmpty config.LogGroupNames) then
            props.LogGroupNames <- config.LogGroupNames |> Seq.toArray
        else
            failwith "At least one Log Group Name is required for LogQueryWidget"

        config.QueryString |> Option.iter (fun qs -> props.QueryString <- qs)

        if not (Seq.isEmpty config.QueryLines) then
            props.QueryLines <- config.QueryLines |> Seq.toArray
        else
            failwith "At least one Query Line is required for LogQueryWidget"

        config.Width |> Option.iter (fun w -> props.Width <- w)

        config.Height |> Option.iter (fun h -> props.Height <- h)

        config.AccountId |> Option.iter (fun a -> props.AccountId <- a)

        config.Region |> Option.iter (fun r -> props.Region <- r)

        config.View |> Option.iter (fun v -> props.View <- v)

        config.QueryLanguage |> Option.iter (fun ql -> props.QueryLanguage <- ql)

        LogQueryWidget(props)


    [<CustomOperation("accountId")>]
    member _.AccountId(config: LogQueryWidgetConfig, accountId: string) =
        { config with
            AccountId = Some accountId }

    [<CustomOperation("logGroupNames")>]
    member _.LogGroupNames(config: LogQueryWidgetConfig, logGroupNames: string seq) =
        { config with
            LogGroupNames = Seq.toList logGroupNames }

    [<CustomOperation("queryLanguage")>]
    member _.QueryLanguage(config: LogQueryWidgetConfig, queryLanguage: LogQueryLanguage) =
        { config with
            QueryLanguage = Some queryLanguage }

    [<CustomOperation("queryLines")>]
    member _.QueryLines(config: LogQueryWidgetConfig, queryLines: string seq) =
        { config with
            QueryLines = Seq.toList queryLines }

    [<CustomOperation("queryString")>]
    member _.QueryString(config: LogQueryWidgetConfig, queryString: string) =
        { config with
            QueryString = Some queryString }

    [<CustomOperation("width")>]
    member _.Width(config: LogQueryWidgetConfig, width: int) = { config with Width = Some width }

    [<CustomOperation("height")>]
    member _.Height(config: LogQueryWidgetConfig, height: int) = { config with Height = Some height }

    [<CustomOperation("region")>]
    member _.Region(config: LogQueryWidgetConfig, region: string) = { config with Region = Some region }

    [<CustomOperation("view")>]
    member _.View(config: LogQueryWidgetConfig, view: LogQueryVisualizationType) = { config with View = Some view }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module DashboardBuilders =
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
    let dashboard name = DashboardBuilder(name)

    /// <summary>Creates a Graph Widget for CloudWatch Dashboards.</summary>
    /// <param name="title">The widget title.</param>
    /// <code lang="fsharp">
    /// graphWidget "API Requests" {
    ///     left [ apiRequestMetric ]
    ///     period (Duration.Minutes(1.0))
    ///     stacked true
    /// }
    /// </code>
    let graphWidget title = GraphWidgetBuilder(title)

    /// <summary>Creates an Alarm Widget for CloudWatch Dashboards.</summary>
    /// <param name="title">The widget title.</param>
    /// <code lang="fsharp">
    /// alarmWidget "High Error Rate" {
    ///     alarm errorAlarm
    ///     leftYAxis (YAxisProps(...))
    /// }
    /// </code>
    let alarmWidget title = AlarmWidgetBuilder(title)

    /// <summary>Creates a Text Widget for CloudWatch Dashboards.</summary>
    /// <code lang="fsharp">
    /// textWidget {
    ///     markdown "# Application Alerts\n- High CPU Usage\n- Memory Leak Detected"
    ///     background TextWidgetBackground.SOLID
    ///     height 6.0
    /// }
    /// </code>
    let textWidget = TextWidgetBuilder()

    /// <summary>Creates a Single Value Widget for CloudWatch Dashboards.</summary>
    /// <param name="title">The widget title.</param>
    /// <code lang="fsharp">
    /// singleValueWidget "Active Users" {
    ///     metrics [ activeUserMetric ]
    ///     fullPrecision true
    /// }
    /// </code>
    let singleValueWidget title = SingleValueWidgetBuilder(title)

    /// <summary>Creates a Log Query Widget for CloudWatch Dashboards.</summary>
    /// <param name="title">The widget title.</param>
    /// <code lang="fsharp">
    /// logQueryWidget "Error Logs" {
    ///     logGroupNames [ "/aws/lambda/my-function" ]
    ///     queryLines [ "fields @timestamp, @message"
    ///                  "filter @message like /ERROR/" ]
    /// }
    /// </code>
    let logQueryWidget title = LogQueryWidgetBuilder(title)
