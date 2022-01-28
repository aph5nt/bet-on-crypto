namespace GameEngine.Games
open System
open Akka.Actor
open Akka.Persistence.FSharp
open Chessie.ErrorHandling

[<AutoOpen>]
module Types = 
    open Shared

    type GameActorServices = { SendActorRef: IActorRef; RateActorRef : IActorRef; }
    exception GameNotFound of string * Bet
    exception InvalidGameState of string * string

    (* EVENTS *)

    type GameEvent<'T> = { Network: Network; PlacedOn : DateTime; Payload : 'T }

    type AddNewPendingGame =  { Date : DateTime }
    type UpdateToOpenedGame = { Date : DateTime }
    type UpdateToClosedGame = { Date : DateTime }
    type GameRefunded = { Name : string }
    type GameDrawed = { Name : string; Win: Amount; Profit: Amount; Winners : Bet list; Status: GameStatus; Accumulation: Amount; WinningVote : Option<Vote> }
    type TrimGames() = class end
    
    type MailBox  = Eventsourced<GameCommand, obj, GameState>
    type Update  = GameState -> obj -> GameState
    type Apply   = MailBox -> GameState -> obj -> GameState

    type Exec        = GameActorServices -> MailBox -> GameState -> GameCommand -> unit
    type MatchDate   = DateTime -> float -> Bet -> bool
    type Refund      = GameActorServices -> GameState -> MailBox -> unit
    type DrawWinners = Game -> Bet list
    type GetWin      = Bet list -> Bet list -> Amount -> Amount * Amount * Amount
    type DrawGame    = GameActorServices -> GameState -> Game -> GameDrawed
 