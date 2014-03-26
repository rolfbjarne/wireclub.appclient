module Settings

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
    Settings.profile username GenderType.Male  (DateTime.UtcNow.AddYears(-30)) "ca" "British Columbia" "Surrey" "A little about myself"
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
