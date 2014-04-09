// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module ApiTests

open Helpers
open NUnit.Framework


[<SetUp>]
let setup () =
    Helpers.setup ()

[<Test>]
let ``Bad Request`` () =
    run <| async {
        let! resp = Api.req<string> "apiTest/badRequest" "GET" []
        match resp with
        | Api.BadRequest [ { Key = "Error"; Value = "Bad Request" } ] -> ()
        | resp -> Assert.Fail (sprintf "Response: %A" resp)
    }
