module FsCdk.Tests.TestHelpers

open Amazon.CDK.AWS.S3.Assets

module S3 =
    let excludeCommonAssetDirs =
        let opts = AssetOptions()
        // Exclude common bulky/irrelevant directories that can cause ENAMETOOLONG during jsii/CDK asset staging
        opts.Exclude <-
            [| "node_modules/**"
               "cdk.out/**"
               "bin/**"
               "obj/**"
               ".git/**"
               ".idea/**"
               ".ionide/**"
               ".vs/**"
               "tmp/**" |]

        opts

module Lambda =
    /// A temp directory with a placeholder file, usable as a Lambda code asset without staging the test bin folder.
    let dummyAssetDir () =
        let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fscdk-test-lambda")
        System.IO.Directory.CreateDirectory(dir) |> ignore
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Handler.dll"), "placeholder")
        dir

module Templates =
    open System.Collections.Generic
    open Amazon.CDK
    open Amazon.CDK.Assertions

    /// The synthesized template of a stack created with `stack name { scope app ... }`.
    let ofStack (app: App) (stackName: string) =
        Template.FromStack(app.Node.FindChild(stackName) :?> Stack)

    /// A JSON object for Match.ObjectLike / HasResourceProperties.
    let obj (pairs: (string * obj) list) : IDictionary<string, obj> =
        let d = Dictionary<string, obj>()

        for k, v in pairs do
            d.[k] <- v

        d :> IDictionary<string, obj>

    /// The single resource of a type, as nested dictionaries.
    let single (template: Template) (resourceType: string) =
        let resources = template.FindResources(resourceType)

        if resources.Count <> 1 then
            failwith $"Expected one {resourceType}, found {resources.Count}"

        resources.Values |> Seq.head

    /// Serializes a template fragment to JSON for substring assertions.
    let json (value: obj) =
        System.Text.Json.JsonSerializer.Serialize(value)
