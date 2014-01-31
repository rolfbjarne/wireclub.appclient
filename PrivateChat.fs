module PrivateChat

open Newtonsoft.Json
open Wireclub.Boundary
open Wireclub.Boundary.Chat

let online () = async {
    let! resp = Api.req "privateChat/online" "get" Map.empty
    return Api.toObject<PrivateChatFriendsOnline> resp
}

let session id = async {
    let! resp = Api.req ("privateChat/session/" + id) "post" Map.empty
    return Api.toObject<SessionResponse> resp
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

let changeOnlineState state = async {
    failwith "not implemented"
}

let initSession id = async {
    let! session = session id
    match session with
    | Api.ApiOk session as response ->
        let processor = new MailboxProcessor<ChannelEvent>(fun inbox ->             
            let rec loop () = async {
                let! event = inbox.Receive()

                match event with
                | { User = user } when user <> id  -> ()
                | { Event = PrivateMessage (color, font, message) } -> printfn "%s: %s" session.DisplayName message
                | { Event = PrivateMessageSent (color, font, message) } -> printfn "Sent: %s: %s" event.User message
                | _ -> ()

                return! loop ()
            }
            loop ())

        processor.Start()
        ChannelClient.handlers.TryAdd(Api.userId, processor) |> ignore
        return response

    | error -> return error
}