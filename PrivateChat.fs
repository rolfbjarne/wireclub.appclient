module PrivateChat

open Newtonsoft.Json
open Wireclub.Boundary

let online () = async {
    let! resp = Api.req "privateChat/online" "get" Map.empty
    return Api.toObject<PrivateChatFriendsOnline> resp
}

let session id = async {
    ()
}