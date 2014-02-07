
module Chat

open Wireclub.Boundary.Chat
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let directory () =
    Api.req<ChatDirectoryViewModel> "chat/directory" "get" []

let join slug = async {
        let! resp = Api.req<JoinResult> ("chat/room/" + slug + "/join") "post" []
        let resp =
            match resp with
            | Api.ApiOk result ->
                let events = 
                    JsonConvert.DeserializeObject result.Events :?> JToken
                    |> ChannelClient.deserializeEventList

                Api.ApiOk (result, events)
            | error -> failwith "ahh" // ## TODO
        return resp
    }

let leave () =
    ()

let send () =
    ()

let acceptDrink () =
    ()

let rejectDrink () =
    ()

let report () =
    ()

let getAd () =
    ()

let changePreferences (font:int) (color:int) =
    Api.req<unit> "chat/changePreferences" "post" [
            "font", (font.ToString())
            "color", (color.ToString())
        ]
