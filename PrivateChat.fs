module PrivateChat


let online () = async {
    return! Api.req "privateChat/online" "get" Map.empty
}

let session id = async {
    ()
}