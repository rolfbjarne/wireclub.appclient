module App

open Wireclub.Boundary.Models

let mutable identity:User option = None

let imageUrl imageId = Api.baseUrl + "/images/" + imageId + "/_"

let fetchActiveChannels () =
    Api.req<Wireclub.Boundary.Chat.ChatRoomDataViewModel> "home/fetchActiveChannels"

