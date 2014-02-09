module PrivateChat

open Newtonsoft.Json
open Wireclub.Models
open Wireclub.Boundary
open Wireclub.Boundary.Chat
open ChannelEvent

let online () =
    Api.req<PrivateChatFriendsOnline> "privateChat/online" "get" []

let session id = 
    Api.req<SessionResponse> ("privateChat/session/" + id) "post" []

let send receiver message = 
    Api.req<PrivateChatSendResponse> "privateChat/sendPrivateMessage3" "post" (
        [
            "receiver", receiver
            "line", message
        ])

let changeOnlineState (state:OnlineStateType) = 
    Api.req<unit> ("privateChat/changeOnlineState") "post" [ "state", string (int state) ]


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