module FsCDK.Tests.CognitoTests

open System
open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Cognito

[<Tests>]
let tests =
    testList
        "Cognito"
        [ test "userPool applies secure defaults" {
              let spec = userPool "Pool" { () }

              // Sign-in aliases default to email + username
              Expect.isTrue spec.Props.SignInAliases.Email.HasValue "Email sign-in should be enabled by default"
              Expect.isTrue spec.Props.SignInAliases.Email.Value "Email sign-in should be enabled by default"
              Expect.isTrue spec.Props.SignInAliases.Username.HasValue "Username sign-in should be enabled by default"
              Expect.isTrue spec.Props.SignInAliases.Username.Value "Username sign-in should be enabled by default"

              // Auto-verify email by default
              Expect.isTrue spec.Props.AutoVerify.Email.HasValue "Email auto-verify should be enabled"
              Expect.isTrue spec.Props.AutoVerify.Email.Value "Email auto-verify should be enabled"

              // Strong password policy defaults
              Expect.equal spec.Props.PasswordPolicy.MinLength.Value 8 "Min password length should be 8"
              Expect.isTrue spec.Props.PasswordPolicy.RequireLowercase.Value "Lowercase required"
              Expect.isTrue spec.Props.PasswordPolicy.RequireUppercase.Value "Uppercase required"
              Expect.isTrue spec.Props.PasswordPolicy.RequireDigits.Value "Digits required"
              Expect.isTrue spec.Props.PasswordPolicy.RequireSymbols.Value "Symbols required"

              // Account recovery and self-signup defaults
              Expect.equal
                  spec.Props.AccountRecovery.Value
                  AccountRecovery.EMAIL_ONLY
                  "Recovery should default to email only"

              Expect.isFalse spec.Props.SelfSignUpEnabled.Value "Self sign-up should default to false"

          }

          test "userPool custom attributes populate dictionary" {
              let attr1 =
                  StringAttribute(
                      StringAttributeProps(
                          Mutable = Nullable<_>(true),
                          MinLen = Nullable<_>(1.),
                          MaxLen = Nullable<_>(10.)
                      )
                  )

              let attr2 = NumberAttribute(NumberAttributeProps(Mutable = Nullable<_>(true)))

              let spec =
                  userPool "PoolWithAttrs" {
                      customAttribute attr1
                      customAttribute attr2
                  }

              Expect.isNotNull spec.Props.CustomAttributes "CustomAttributes dictionary should be set"
              Expect.isTrue (spec.Props.CustomAttributes.Count >= 2) "At least two custom attributes expected"

          }

          test "userPoolClient requires userPool" {
              let mutable ex: exn option = None

              try
                  let _ = userPoolClient "Client" { () }
                  ()
              with e ->
                  ex <- Some e

              Expect.isSome ex "Expected an ArgumentException for missing user pool"

              match ex with
              | Some(:? ArgumentException as ae) ->
                  Expect.equal ae.ParamName "userPool" "ParamName should be 'userPool'"
              | Some e -> failtestf "Unexpected exception type: %A" e
              | None -> ()

          }

          test "userPoolClient applies secure client defaults" {
              // Create a minimal real UserPool to satisfy required prop type
              let app = App()
              let stack = Stack(app, "TestStack")
              let up = UserPool(stack, "UP")

              let spec = userPoolClient "Client" { userPool up }

              // Defaults
              Expect.isFalse spec.Props.GenerateSecret.Value "Public clients should not generate secret by default"

              Expect.isTrue
                  spec.Props.PreventUserExistenceErrors.Value
                  "PreventUserExistenceErrors should default to true"

              // Auth flows default to SRP + password
              Expect.isTrue spec.Props.AuthFlows.UserSrp.Value "SRP auth flow should be enabled"
              Expect.isTrue spec.Props.AuthFlows.UserPassword.Value "Password auth flow should be enabled"

          }

          test "resourceServer requires userPool" {
              let mutable ex: exn option = None

              try
                  let _ = userPoolResourceServer "ApiServer" { () }
                  ()
              with e ->
                  ex <- Some e

              Expect.isSome ex "Expected an ArgumentException for missing user pool"

              match ex with
              | Some(:? ArgumentException as ae) ->
                  Expect.equal ae.ParamName "userPool" "ParamName should be 'userPool'"
              | Some e -> failtestf "Unexpected exception type: %A" e
              | None -> ()

          }

          test "resourceServer builds with identifier and scopes" {
              let app = new App()
              let stack = new Stack(app, "TestStack")
              let up = new UserPool(stack, "UP")

              let spec =
                  userPoolResourceServer "ApiServer" {
                      userPool up
                      identifier "api"
                      name "API Resource Server"
                      scope "read" "Read access"
                      scope "write" "Write access"
                      scope "admin" "Admin access"
                  }

              Expect.equal spec.Props.Identifier "api" "Identifier should be 'api'"
              Expect.equal spec.Props.UserPoolResourceServerName "API Resource Server" "Name should be set"
              Expect.isNotNull spec.Props.Scopes "Scopes should be set"

          } ]
    |> testSequenced
