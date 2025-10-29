namespace FsCDK
//
// open Amazon.CDK
// open Amazon.CDK.AWS.DynamoDB
// open Amazon.CDK.AWS.KMS
// open Amazon.CDK.AWS.Lambda
//
// // ============================================================================
// // Grant/Permissions Configuration DSL
// // ============================================================================
//
// // Grant access type
// type GrantAccessType =
//     | Read
//     | Write
//     | ReadWrite
//     | Custom of (Table -> Function -> unit)
//
// // Grant configuration DSL
// type GrantConfig =
//     { TableConstructId: ITable option
//       LambdaConstructId: IFunction option
//       Access: GrantAccessType option }
//
// type GrantSpec =
//     { TableConstructId: ITable
//       LambdaConstructId: IFunction
//       Access: GrantAccessType }
//
// type GrantBuilder() =
//     member _.Yield _ : GrantConfig =
//         { TableConstructId = None
//           LambdaConstructId = None
//           Access = None }
//
//     member _.Zero() : GrantConfig =
//         { TableConstructId = None
//           LambdaConstructId = None
//           Access = None }
//
//     member inline _.Delay([<InlineIfLambda>] f: unit -> GrantConfig) : GrantConfig = f ()
//
//     member _.Combine(state1: GrantConfig, state2: GrantConfig) : GrantConfig =
//         { TableConstructId = state2.TableConstructId |> Option.orElse state1.TableConstructId
//           LambdaConstructId = state2.LambdaConstructId |> Option.orElse state1.LambdaConstructId
//           Access = state2.Access |> Option.orElse state1.Access }
//
//     member inline x.For(config: GrantConfig, [<InlineIfLambda>] f: unit -> GrantConfig) : GrantConfig =
//         let newConfig = f ()
//         x.Combine(config, newConfig)
//
//     member _.Run(config: GrantConfig) : GrantSpec =
//         let access =
//             match config.Access with
//             | Some a -> a
//             | None -> failwith "grant access type is required (readAccess, writeAccess, readWriteAccess, customAccess)"
//
//         { TableConstructId =
//             match config.TableConstructId with
//             | Some t -> t
//             | None -> failwith "grant.table is required"
//
//           LambdaConstructId =
//             match config.LambdaConstructId with
//             | Some l -> l
//             | None -> failwith "grant.lambda is required"
//
//           Access = access }
//
//     /// <summary>Sets the DynamoDB table for the grant.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <param name="tableConstructId">The construct ID of the table.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     /// }
//     /// </code>
//     [<CustomOperation("table")>]
//     member _.Table(config: GrantConfig, tableConstructId: ITable) =
//         { config with
//             TableConstructId = Some tableConstructId }
//
//     /// <summary>Sets the Lambda function for the grant.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <param name="lambdaConstructId">The construct ID of the Lambda function.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     lambda "MyFunction"
//     /// }
//     /// </code>
//     [<CustomOperation("lambdaId")>]
//     member _.Lambda(config: GrantConfig, lambdaConstructId: IFunction) =
//         { config with
//             LambdaConstructId = Some lambdaConstructId }
//
//     /// <summary>Grants read access to the table.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     ///     lambda "MyFunction"
//     ///     readAccess
//     /// }
//     /// </code>
//     [<CustomOperation("readAccess")>]
//     member _.ReadAccess(config: GrantConfig) = { config with Access = Some Read }
//
//     /// <summary>Grants write access to the table.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     ///     lambda "MyFunction"
//     ///     writeAccess
//     /// }
//     /// </code>
//     [<CustomOperation("writeAccess")>]
//     member _.WriteAccess(config: GrantConfig) = { config with Access = Some Write }
//
//     /// <summary>Grants read and write access to the table.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     ///     lambda "MyFunction"
//     ///     readWriteAccess
//     /// }
//     /// </code>
//     [<CustomOperation("readWriteAccess")>]
//     member _.ReadWriteAccess(config: GrantConfig) = { config with Access = Some ReadWrite }
//
//     /// <summary>Applies custom grant logic.</summary>
//     /// <param name="config">The current grant configuration.</param>
//     /// <param name="grantFunc">A function that takes the table and Lambda function to apply custom grants.</param>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     ///     lambda "MyFunction"
//     ///     customAccess (fun table fn -> table.Grant(fn, "dynamodb:Query"))
//     /// }
//     /// </code>
//     [<CustomOperation("customAccess")>]
//     member _.CustomAccess(config: GrantConfig, grantFunc: Table -> Function -> unit) =
//         { config with
//             Access = Some(Custom grantFunc) }
//
// module Grants =
//     let processGrant (stack: Stack) (grantSpec: GrantSpec) =
//         let table = grantSpec.TableConstructId :?> Table
//         let lambda = (grantSpec.LambdaConstructId) :?> Function
//
//         match grantSpec.Access with
//         | Read -> table.GrantReadData(lambda) |> ignore
//         | Write -> table.GrantWriteData(lambda) |> ignore
//         | ReadWrite -> table.GrantReadWriteData(lambda) |> ignore
//         | Custom grantFunc -> grantFunc table lambda
//
// // ============================================================================
// // Builders
// // ============================================================================
//
// [<AutoOpen>]
// module GrantsBuilders =
//     /// <summary>Creates a grant configuration for permissions between resources.</summary>
//     /// <code lang="fsharp">
//     /// grant {
//     ///     table "MyTable"
//     ///     lambda "MyFunction"
//     ///     readWriteAccess
//     /// }
//     /// </code>
//     let grant = GrantBuilder()
