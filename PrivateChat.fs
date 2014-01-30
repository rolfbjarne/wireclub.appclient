module PrivateChat

open Newtonsoft.Json
open Wireclub.Boundary
open Wireclub.Boundary.Chat

let online () = async {
    let! resp = Api.req "privateChat/online" "get" Map.empty
    return Api.toObject<PrivateChatFriendsOnline> resp
}

let session id = async {
    ()
}

let send receiver message = async {
    let! resp = 
        Api.req "privateChat/sendPrivateMessage3" "post" (
            [
                "receiver", receiver
                "line", message
            ] |> Map.ofList)

    return Api.toObject<PrivateChatSendResponse> resp
}

// change online state