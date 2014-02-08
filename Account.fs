module Account

open Wireclub.Boundary
open Wireclub.Boundary.Models
open Newtonsoft.Json

let handleLogin result =
    match result with
    | Api.ApiOk result ->
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        Api.userId <- result.Identity.Id
        Api.userHash <- result.Csrf
        Api.ApiOk result
    | resp -> resp

let login username password = async {
    let! resp = 
        Api.req<LoginResult> "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ])
    
    return handleLogin resp
}

let loginToken token = async {
    let! resp = 
        Api.req<LoginResult> "account/tokenLogin" "post" (
            [
                "token", token
            ])
    
    return handleLogin resp
}

let home () =
    Api.req<string> "home" "get" []

let test () = 
    Api.req<string> "test/csrf" "post" []

let identity () =
    Api.req<User> "account/identity" "post" []
