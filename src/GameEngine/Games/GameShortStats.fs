namespace GameEngine.Games

open Akka.Actor
open Akka.FSharp
open Akka.Persistence
open Akka.Persistence.FSharp

module ShortStats =
    open Shared
    open Types
     
    let apply (view : View<obj, ShortStat>) (state: ShortStat) (event : obj)  =
        match event with
        | :? GameEvent<GameDrawed> as e ->
            let evnt = e.Payload
            let getHighestWin state (game : GameDrawed) =
                if game.Winners.Length = 0 then state.HighestWin
                else if state.HighestWin > game.Win then state.HighestWin
                else game.Win
            let stateR = 
                { state with 
                    TotalWins = state.TotalWins + (int64 evnt.Winners.Length); 
                    TotalWon = roundAmount <| state.TotalWon + (evnt.Win  * (decimal evnt.Winners.Length));
                    HighestWin = roundAmount <| getHighestWin state evnt; }
 
            if (SystemTime.UtcNow() - e.PlacedOn).TotalSeconds < 6. then
                view.Context.System.EventStream.Publish({ GameUIEvents.OnShortStat.ShortStat = stateR })
                view.SaveSnapshot(stateR)
            stateR

        // restore from the VIEW snapshot
        | :? SnapshotOffer as o -> 
            Logger.Debug("GameShortStats - SnapshotOffer received")
            o.Snapshot :?> ShortStat
        | :? LoadSnapshotResult as o -> 
            Logger.Debug("GameShortStats - LoadSnapshotResult received")
            o.Snapshot.Snapshot :?> ShortStat
        | :? GetShortStats as q -> 
                view.Sender().Tell(state)
                state
        | _ ->  state

    let run (system : ActorSystem) network = 
        spawnView system "short-stats" "game"  {
            state = { ShortStat.TotalWon = 0m; ShortStat.HighestWin = 0m; ShortStat.TotalWins = 0L; }
            apply = apply
        } []

