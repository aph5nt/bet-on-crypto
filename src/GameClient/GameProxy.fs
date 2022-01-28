namespace GameClient.Games

open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open Akka.Actor
open Akka.FSharp
open Shared
open System

(* GAME HUB *)
type IGameHub =
    abstract OnBetPlaced : CurrentGame -> unit
    abstract OnCurrentGamesUpdate : CurrentGame[] -> unit
    abstract OnPreviousGamesUpdate : PreviousGame[] -> unit
    abstract OnAccumulationUpdate : Amount -> unit
    abstract OnShortStatUpdate : ShortStat -> unit
    abstract OnBetReceived : CurrentBet[] -> unit

[<HubName("GameHub")>]
type GameHub() =
    inherit Hub<IGameHub>()
 
(* === ACTORS === *)
module GameProxyActor =
    open Shared
   
    let run system  =
        let _hub = GlobalHost.ConnectionManager.GetHubContext<GameHub, IGameHub>();
        spawn system "gameProxy"
            (fun mailbox ->
                let rec loop() = actor {
                    let! msg = mailbox.Receive()
                    match box msg with
                    | :? GameUIEvents.OnBetPlaced as dto     -> _hub.Clients.All.OnBetPlaced(dto.CurrentGame)
                    | :? GameUIEvents.Reload as dto          -> _hub.Clients.All.OnAccumulationUpdate(dto.ReloadData.Accumulation)
                                                                _hub.Clients.All.OnCurrentGamesUpdate(dto.ReloadData.CurrentGames)
                                                                _hub.Clients.All.OnPreviousGamesUpdate(dto.ReloadData.PreviousGames)
                    | :? GameUIEvents.OnShortStat as dto     -> _hub.Clients.All.OnShortStatUpdate(dto.ShortStat)
                    | _ -> ()
                    return! loop()
                }
                loop())


