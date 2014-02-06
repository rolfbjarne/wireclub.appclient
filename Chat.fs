module Chat

open Wireclub.Boundary.Chat

let directory () =
    Api.req<ChatRoomDataViewModel[]> "chat/directory" "get" []
    (*Api.ApiResult.ApiOk (
        [|
            {
                Id = "aaa"
                Name = "Chat Room"
                Slug = "chat_room"
                Url = "/chat/rooms/chat_room"
                Apps = [| |]
                JoinPolicy = 0
            }
        |])*)

let join slug =
    Api.req<JoinResult> ("chat/" + slug + "/join") "post" []