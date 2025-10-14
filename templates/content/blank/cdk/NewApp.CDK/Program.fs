namespace NewApp.CDK

open Amazon.CDK.AWS.Lambda
open FsCDK

module Program =
    [<EntryPoint>]
    let main _argv =
        let app = app { }

        // Define a stack with a simple HelloWorld Lambda and a public Function URL
        stack "NewApp" {
            app

            lambda "HelloWorld" {
                runtime Runtime.DOTNET_8
                // Namespace: NewApp; Type: Function; Method: HandleAsync
                handler "NewApp::NewApp.Function::HandleAsync"
                // Use the published output from the src/NewApp project
                code "../src/NewApp/bin/Release/net8.0/publish"

                // Public function URL (no auth) for quick testing
                functionUrl (functionUrl { authType FunctionUrlAuthType.NONE })
            }
        }

        app.Synth()
        0
