module Chat

let directory () =
    ()

let join () =
    ()

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