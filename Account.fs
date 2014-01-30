module Account

open Newtonsoft.Json

let login username password = async {
    let! resp = 
        Api.req "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ] |> Map.ofList)
    
    match resp with
    | Api.ApiOk content ->
        let result = JsonConvert.DeserializeObject<Wireclub.Boundary.LoginResult>(content)
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        return Api.ApiOk result
    | Api.ApiError (t, s) -> 
        return Api.ApiError (t, s)
}

let home () = async {
    return! Api.req "home" "get" Map.empty
}

let test () = async {
    return! Api.req "test/csrf" "post" Map.empty
}

Async.RunSynchronously (login "braden" "notinthedictionary")

Async.RunSynchronously (home ())

Async.RunSynchronously (test ())