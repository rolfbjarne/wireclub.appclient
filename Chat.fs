module Chat

open Wireclub.Boundary.Chat

let directory () =
    Api.req<ChatDirectoryViewModel> "chat/directory" "get" []

let join slug =
    Api.req<JoinResult> ("chat/room/" + slug + "/join") "post" []

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
