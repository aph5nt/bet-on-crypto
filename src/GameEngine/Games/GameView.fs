namespace GameEngine.Games

open Akka.Actor
open Akka.FSharp
open Akka.Persistence
open Akka.Persistence.FSharp
open System

module GameView = 
    open Shared
    open GameViewCommand
     
    let apply (mailbox : View<obj, GameState>) (state: GameState) (event : obj)  =
        let publish (evnt : obj) (mailbox : View<obj, GameState>) =
                mailbox.Context.System.EventStream.Publish evnt
 
        match event with 
        // State events
        | :? GameEvent<AddNewPendingGame> as e  ->  GameActor.update state e
        | :? GameEvent<UpdateToOpenedGame> as e ->  GameActor.update state e
        | :? GameEvent<UpdateToClosedGame> as e ->  GameActor.update state e
        | :? GameEvent<GameDrawed> as e         ->
            let stateR = GameActor.update state e
            if (SystemTime.UtcNow() - e.PlacedOn).TotalSeconds < 6. then
                let evnt = e.Payload
                let message = { 
                    GameDrawedData.Name = evnt.Name
                    GameDrawedData.Accumulation = evnt.Accumulation
                    GameDrawedData.Profit = evnt.Profit
                    GameDrawedData.Win = evnt.Win
                    GameDrawedData.Winners = evnt.Winners
                    GameDrawedData.WinningVote = evnt.WinningVote
                }
                publish
                <| { GameUIEvents.OnGameDrawed.GameDrawedData = message }
                <| mailbox

                mailbox.SaveSnapshot(stateR)

            stateR
        | :? GameEvent<GameRefunded> as evnt       ->  GameActor.update state evnt

        | :? GameEvent<TrimGames> as e ->
            let stateR = GameActor.update state e
            if (SystemTime.UtcNow() - e.PlacedOn).TotalSeconds < 6. then
                publish
                <| { GameUIEvents.Reload.ReloadData = { CurrentGames  = Querying.getCurrentGames stateR.Games;
                                                        PreviousGames = Querying.getPreviousGames stateR.Games;
                                                        Accumulation  = stateR.Accumulation } }
                <| mailbox
            stateR

        | :? GameEvent<Bet> as e ->
            let stateR = GameActor.update state e
            if (SystemTime.UtcNow() - e.PlacedOn).TotalSeconds < 6. then
                publish 
                <| { GameUIEvents.OnBetPlaced.CurrentGame = (Map.find e.Payload.GameName stateR.Games |> toCurrentGame) }
                <| mailbox
                // update bets for given game, group them by account id
            stateR
             
       // Querying
        | :? QueryAll ->
            mailbox.Context.Sender <! state
            state
        | :? QueryCurrent -> 
            mailbox.Context.Sender <! Querying.getCurrentGames state.Games
            state
        | :? QueryPrevious -> 
            mailbox.Context.Sender <! Querying.getPreviousGames state.Games
            state
        | :? QueryAccumulation ->
            mailbox.Context.Sender <! state.Accumulation
            state
        | :? QueryPastBets as cmd -> 
            mailbox.Context.Sender <! Querying.getPastBets cmd.GameName state.Games
            state
        | :? QueryCurrentBets as cmd  ->
            mailbox.Context.Sender <! Querying.getCurrentBets cmd.GameName cmd.Account state.Games
            state

        // View specific
        | :? SnapshotOffer as o         -> 
            Logger.Debug("GameView - SnapshotOffer received") 
            o.Snapshot :?> GameState
        | :? LoadSnapshotResult as o    -> 
            Logger.Debug("GameView - LoadSnapshotResult received") 
            o.Snapshot.Snapshot :?> GameState
        | :? SaveSnapshotSuccess as m   -> Logger.Debug("GameView - SaveSnapshotSuccess")
                                           state
        | :? SaveSnapshotFailure as e -> Logger.Error("GameView - SaveSnapshotFailure {@Cause} {@Metadata}", e.Cause, e.Metadata)
                                         state
        | x -> 
            mailbox.Unhandled x
            state

    let run (system : ActorSystem) network = 
        spawnView system "readonly-game" "game"  {
                state = { GameState.Accumulation = 0m; GameState.Games = Map.empty; Network = network }
                apply = apply
            } []