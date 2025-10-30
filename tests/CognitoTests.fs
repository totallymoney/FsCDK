module FsCDK.Tests.CognitoTests

open System
open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.Lambda

[<Tests>]
let tests =
    // Some of these test doesn't run well in parallel
    testSequenced
    <| testList
        "Cognito"
        [

          testCase "userPool applies secure defaults"
          <| fun _ ->
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

          // This test is skipped because it works well as single execution, but may fail when run on parallel with other tests.
          testCase "userPool custom attributes populate dictionary"
          <| fun _ ->

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
                      attr1
                      attr2
                  }

              Expect.isNotNull spec.Props.CustomAttributes "CustomAttributes dictionary should be set"
              Expect.isTrue (spec.Props.CustomAttributes.Count >= 2) "At least two custom attributes expected"

          testCase "userPoolClient requires userPool"
          <| fun _ ->
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

          testCase "userPoolClient applies secure client defaults"
          <| fun _ ->
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

          testCase "userPool accepts signInAliases builder yield"
          <| fun _ ->
              let spec =
                  userPool "PoolWithAliases" {
                      signInAliases {
                          email true
                          phone true
                          preferredUsername true
                          username false
                      }
                  }

              Expect.isTrue spec.Props.SignInAliases.Email.HasValue "Email flag should be set"
              Expect.isTrue spec.Props.SignInAliases.Email.Value "Email enabled"
              Expect.isTrue spec.Props.SignInAliases.Phone.HasValue "Phone flag should be set"
              Expect.isTrue spec.Props.SignInAliases.Phone.Value "Phone enabled"
              Expect.isTrue spec.Props.SignInAliases.PreferredUsername.HasValue "PreferredUsername flag should be set"
              Expect.isTrue spec.Props.SignInAliases.PreferredUsername.Value "PreferredUsername enabled"
              Expect.isTrue spec.Props.SignInAliases.Username.HasValue "Username flag should be present"
              Expect.isFalse spec.Props.SignInAliases.Username.Value "Username explicitly disabled"

          testCase "userPool accepts autoVerifiedAttrs builder yield"
          <| fun _ ->
              let spec =
                  userPool "PoolWithAutoVerify" {
                      autoVerifiedAttrs {
                          email false
                          phone true
                      }
                  }

              Expect.isTrue spec.Props.AutoVerify.Email.HasValue "Email auto-verify flag should be set"
              Expect.isFalse spec.Props.AutoVerify.Email.Value "Email auto-verify disabled"
              Expect.isTrue spec.Props.AutoVerify.Phone.HasValue "Phone auto-verify flag should be set"
              Expect.isTrue spec.Props.AutoVerify.Phone.Value "Phone auto-verify enabled"

          testCase "userPool accepts standardAttributes builder and leaf standardAttribute"
          <| fun _ ->
              let spec =
                  userPool "PoolWithStandardAttrs" {
                      standardAttributes {
                          email (standardAttribute { required true })
                          phoneNumber (standardAttribute { mutable' true })
                          fullname (standardAttribute { required true })
                      }
                  }

              let sa = spec.Props.StandardAttributes
              // Email required
              Expect.isTrue sa.Email.Required.Value "Email should be required"
              // PhoneNumber mutable
              Expect.isTrue sa.PhoneNumber.Mutable.Value "PhoneNumber should be mutable"
              // Fullname required
              Expect.isTrue sa.Fullname.Required.Value "Fullname should be required"

          testCase "userPool accepts mfaSecondFactor builder yield"
          <| fun _ ->
              let spec =
                  userPool "PoolWithMfaSecondFactor" {
                      mfaSecondFactor {
                          otp true
                          sms true
                      }
                  }

              let m = spec.Props.MfaSecondFactor
              Expect.isTrue m.Otp "Otp enabled"
              Expect.isTrue m.Sms "Sms enabled"

          test "userPool accepts userPoolTriggers builder yield" {
              stack "TriggerStack" {

                  let! fn1 =
                      lambda "PostConfirmationFn" {
                          runtime Runtime.NODEJS_18_X
                          handler "index.handler"
                          code (Code.FromInline("exports.handler = async () => { return {}; }"))
                      }

                  let! fn2 =
                      lambda "PreSignUpFn" {
                          runtime Runtime.NODEJS_18_X
                          handler "index.handler"
                          code (Code.FromInline("exports.handler = async () => { return {}; }"))
                      }

                  let spec =
                      userPool "PoolWithTriggers" {
                          userPoolTriggers {
                              preSignUp fn1
                              postConfirmation fn2
                          }
                      }

                  let t = spec.Props.LambdaTriggers
                  Expect.equal t.PreSignUp fn1 "PreSignUp trigger should be assigned"
                  Expect.equal t.PostConfirmation fn2 "PostConfirmation trigger should be assigned"

              }

          }
          testCase "userPool supports implicit yield of ICustomAttribute instances"
          <| fun _ ->
              let attr1 = BooleanAttribute(CustomAttributeProps(Mutable = Nullable<_>(true)))
              let attr2 = DateTimeAttribute()

              let spec =
                  userPool "PoolWithImplicitAttrs" {
                      attr1
                      attr2
                  }

              Expect.isNotNull spec.Props.CustomAttributes "CustomAttributes dictionary should be set"
              Expect.isTrue (spec.Props.CustomAttributes.Count >= 2) "At least two custom attributes expected" ]
