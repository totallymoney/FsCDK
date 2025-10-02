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
