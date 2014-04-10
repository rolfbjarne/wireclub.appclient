// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module ChannelEvent

open Wireclub.Boundary.Chat
open Wireclub.Boundary.Models

type ChannelEventType =
| Unknown
| Notification of string
| Message of (*color:*) string * (*font:*) int * (*message:*) string
| Join of (* user: *) UserProfile
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
    Channel: string
    Event: ChannelEventType
}
