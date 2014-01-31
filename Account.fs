module Account

open Wireclub.Boundary
open Wireclub.Boundary.Models
open Newtonsoft.Json

let login username password = async {
    let! resp = 
        Api.req "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ] |> Map.ofList)
    
    match Api.toObject<LoginResult> resp with
    | Api.ApiOk result ->
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        Api.userId <- result.Identity.Id
        Api.userHash <- result.Csrf
        return Api.ApiOk result
    | resp -> return resp
}

let home () = async {
    return! Api.req "home" "get" Map.empty
}

let test () = async {
    return! Api.req "test/csrf" "post" Map.empty
}

let identity () = async {
    let! resp = Api.req "/account/identity" "post" Map.empty
    return Api.toObject<User> resp
}