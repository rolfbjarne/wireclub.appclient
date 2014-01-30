module Account

open Newtonsoft.Json

let login username password = async {
    let! resp = 
        Api.req "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ] |> Map.ofList)
    
    match Api.toObject<Wireclub.Boundary.LoginResult> resp with
    | Api.ApiOk result ->
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        return Api.ApiOk result
    | resp -> return resp
}

let home () = async {
    return! Api.req "home" "get" Map.empty
}

let test () = async {
    return! Api.req "test/csrf" "post" Map.empty
}