// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module SettingsTests

open System
open Helpers
open NUnit.Framework
open Helpers
open Wireclub.Models

[<SetUp>]
let setup () =
    Helpers.setup ()
    Helpers.login ()

[<Test>]
let ``Update Profile`` () =
    Settings.updateProfile username GenderType.Male  (DateTime.UtcNow.AddYears(-30)) "ca" "British Columbia" "Surrey" "A little about myself"
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Fetch Countries`` () = 
    let countries = 
        Settings.countries ()
        |> run
        |> assertApiResult

    printfn "%A" countries
    ()

[<Test>]
let ``Fetch Regions`` () = 
    let regions = 
        Settings.regions "CA"
        |> run
        |> assertApiResult

    printfn "%A" regions
    ()

[<Test>]
let ``Update Email`` () =
    Settings.email Helpers.email Helpers.email Helpers.password
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Password`` () =
    Settings.password Helpers.password Helpers.password Helpers.password
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Fetch Notifications`` () =
    Settings.notifications ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Notifications`` () =
    Settings.suppressNotifications [ "AlertMessage"; "AlertInvitedToEntity" ]
    |> run
    |> assertApiResult
    |> ignore


[<Test>]
let ``Fetch Chat Options`` () =
    Settings.chat ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Chat Options`` () =
    Settings.updateChat 0 3 false 1
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Fetch Privacy`` () =
    Settings.privacy ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Privacy`` () =
    Settings.updatePrivacy         
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        RelationshipRequiredType.Anyone
        false
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Fetch Messaging`` () =
    Settings.messaging ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Messaging`` () =
    Settings.updateMessaging true true true
    |> run
    |> assertApiResult
    |> ignore


[<Test>]
let ``Fetch Blocks`` () =
    Settings.blocked ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Unblock`` () =
    Settings.unblock [ "AAAAAAAAAAAANj7F0" ; "AAAAAAAAAAAAHu8O0" ]
    |> run
    |> assertApiResult
    |> ignore

    
[<Test>]
let ``Fetch Profile`` () =
    Settings.profile ()
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Update Content Rating`` () =
    Settings.updateContentOptions true
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Register Device & Update Push Token`` () =
    let deviceId = 
        Settings.registerDevice "123"
        |> run
        |> assertApiResult

    printfn "Device id: %s" deviceId

    Settings.updateDevicePushToken deviceId "456"
    |> run
    |> assertApiResult

[<Test>]
let ``Delete Device`` () =
    let deviceId = 
        Settings.registerDevice "123"
        |> run
        |> assertApiResult

    Settings.deleteDevice deviceId
    |> run
    |> assertApiResult
