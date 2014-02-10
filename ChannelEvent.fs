module ChannelEvent

open Wireclub.Boundary.Chat

type ChannelEventType =
| Unknown
| Notification of string
| Message of (*color:*) string * (*font:*) int * (*message:*) string
| Join of (* user: *) ChatUser
| Leave of string
| Modifier
| Drink
| ThumbsUp
| ThumbsDown
| Preference
| AddedInvitation
| AddedModerator
| RemovedModerator
| Ticker
| AppEvent
| AcceptDrink
| CustomAppEvent
| StartApp
| QuitApp
| GameChallenge
| GameMatch
| KeepAlive
| DisposableMessage
| PrivateMessage of (*color:*) string * (*font:*) int * (*message:*) string
| PrivateMessageSent of (*color:*) string * (*font:*) int * (*message:*) string
| PeekAvailable
| BingoRoundChanged
| BingoRoundDraw
| BingoRoundWon

[<CLIMutable>]
type ChannelEvent = {
    Sequence: int64
    User: string
    Event: ChannelEventType
}
