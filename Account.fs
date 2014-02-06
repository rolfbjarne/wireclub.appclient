module Account

open Wireclub.Boundary
open Wireclub.Boundary.Models
open Newtonsoft.Json

let login username password = async {
    let! resp = 
        Api.req<LoginResult> "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ])
    
    match resp with
    | Api.ApiOk result ->
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        Api.userId <- result.Identity.Id
        Api.userHash <- result.Csrf
        return Api.ApiOk result
    | resp -> return resp
}

let home () =
    Api.req<string> "home" "get" []

let test () = 
    Api.req<string> "test/csrf" "post" []

let identity () =
    Api.req<User> "account/identity" "post" []
