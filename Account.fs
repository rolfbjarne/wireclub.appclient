module Account

let login username password = async {
    return! Api.req "account/doLogin" "post" (
        [
            "username", username
            "password", password
        ] |> Map.ofList)
}

let home () = async {
    return! Api.req "home" "get" Map.empty
}

Async.RunSynchronously (login "braden" "notinthedictionary")

Async.RunSynchronously (home ())