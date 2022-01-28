namespace GameEngine.Games
 
open Chessie.ErrorHandling
open Akka.FSharp
open Akka.Persistence
open Akka.Persistence.FSharp
open System

module GameActor = 
    open Shared
    open Types
  
    let update : Update  =
        fun state event ->
            match event with
            | :? GameEvent<Bet> as e -> 
                let bet = e.Payload
                let found = Querying.findGame state bet
                let updated = { found with Bets = bet::found.Bets }
                let games' = 
                    state.Games
                    |> Map.remove found.Name
                    |> Map.add updated.Name updated
                { state with Games = games' }
            | :? GameEvent<AddNewPendingGame> as e ->
                let evnt = e.Payload
                let gameName = Game.GetName evnt.Date
                let exists = state.Games.ContainsKey(gameName)
                match exists with
                | true -> state
                | false ->
                    let game = Game.Create e.Network Game.GetName evnt.Date
                    let games' = state.Games.Add(gameName, game)
                    { state with Games = games' }
            | :? GameEvent<UpdateToOpenedGame> as e ->
                let evnt = e.Payload
                let gameName = Game.GetName evnt.Date
                let exists = state.Games.ContainsKey(gameName)
                match exists with
                | false -> 
                    let game = Game.Create e.Network Game.GetName evnt.Date
                    let game' = { game with Status = Opened }
                    let games' = state.Games.Add(gameName, game')
                    { state with Games = games' }
                | true -> 
                    let game = Map.find (Game.GetName evnt.Date) state.Games 
                    let game' = { game with Status = Opened }
                    let games = state.Games |> Map.remove game.Name |> Map.add game'.Name game'
                    { state with Games = games }
            | :? GameEvent<UpdateToClosedGame> as e ->
                let evnt = e.Payload
                let gameName = Game.GetName evnt.Date
                let exists = state.Games.ContainsKey(gameName)
                match exists with
                | false -> state
                | true ->
                    let game = Map.find (Game.GetName evnt.Date) state.Games 
                    let game' = { game with Status = Closed }
                    let games = state.Games |> Map.remove game.Name |> Map.add game'.Name game'
                    { state with Games = games }
            | :? GameEvent<GameDrawed> as e -> 
                let evnt = e.Payload
                let exists = state.Games.ContainsKey(evnt.Name)
                match exists with
                | false -> state
                | true -> 
                    let game = Map.find evnt.Name state.Games 
                    let game' = { game with Status = Drawed; Win = evnt.Win; Profit = evnt.Profit; Winners = evnt.Winners; WinningVote = evnt.WinningVote }
                    let games = state.Games |> Map.remove game.Name |> Map.add game'.Name game'
                    { state with Games = games; Accumulation = evnt.Accumulation }

            | :? GameEvent<GameRefunded> as e ->
                let evnt = e.Payload
                let exists = state.Games.ContainsKey(evnt.Name)
                match exists with
                | false -> state
                | true ->
                    let game = Map.find evnt.Name state.Games
                    let game' = { game with Status = Refunded }
                    let games = state.Games |> Map.remove game.Name |> Map.add game'.Name game'
                    { state with Games = games }
            | :? GameEvent<TrimGames> as e ->
                let evnt = e.Payload
                match state.Games.Count with
                | n when n > 24 ->
                    let games = state.Games |> Map.toSeq |> Seq.sortByDescending(fun(_, game) -> game.OpenedAt) |> Seq.take(24)
                    { state with Games = Map.ofSeq(games) }
                | _ -> 
                    let games = state.Games |> Map.toSeq |> Seq.sortByDescending(fun(_, game) -> game.OpenedAt)
                    { state with Games = Map.ofSeq(games) }
            | _ -> 
                let e = event
                state
 
    let apply : Apply = 
        fun mailbox state event -> 
            match event with 
            | :? GameEvent<AddNewPendingGame> as evnt -> update state evnt
            | :? GameEvent<UpdateToOpenedGame> as evnt -> update state evnt
            | :? GameEvent<UpdateToClosedGame> as evnt -> update state evnt
            | :? GameEvent<GameDrawed> as evnt -> update state evnt
            | :? GameEvent<GameRefunded> as evnt -> update state evnt
            | :? GameEvent<TrimGames> as evnt -> update state evnt
            | :? GameEvent<Bet> as evnt -> update state evnt
            | :? SnapshotOffer as o -> 
                Logger.Debug("GameActor - SnapshotOffer received")
                o.Snapshot :?> GameState
            | :? LoadSnapshotResult as o -> 
                Logger.Debug("GameActor - LoadSnapshotResult received")
                o.Snapshot.Snapshot :?> GameState
            | :? SaveSnapshotSuccess as m -> mailbox.DeleteSnapshots <| new SnapshotSelectionCriteria(m.Metadata.SequenceNr - 1L)
                                             Logger.Debug("GameActor - SaveSnapshotSuccess")
                                             state
            | :? SaveSnapshotFailure as e -> Logger.Error("GameActor - SaveSnapshotFailure {@Cause} {@Metadata}", e.Cause, e.Metadata)
                                             state
            | x ->  mailbox.Unhandled x
                    state
  
    let applyEvent<'T> (state : GameState) (evnt : 'T) (mailbox : MailBox) = 
                let e = { Network = state.Network; Payload = evnt ; PlacedOn = SystemTime.UtcNow()}
                mailbox.PersistEvent (update state) [e]
                update state e

    let handleDraw (game : Game) state services mailbox =
        let refund state (game : Game) message =
            game.Bets |> List.iter(fun(bet) -> services.SendActorRef <! SendCommand.Refund(bet, message))
            let stateR =
                applyEvent 
                <| state
                <| { GameRefunded.Name = game.Name }
                <| mailbox
            Logger.Information("GameActor - Refunded {@Game}", game)
            stateR

        let diff = (SystemTime.UtcNow() - game.DrawingAt).TotalMinutes
        match game.Status with
        | GameStatus.Closed when diff <= 5.0 ->
            let rate = services.RateActorRef <? new RateCommand.Current() |> Async.RunSynchronously
            match rate with
            | None -> 
                refund state game GameRefundedError
            | Some(value) ->
                let evnt = Drawing.drawGame services state { game with WinningVote = Some(value.Day7) } 
                let stateR = 
                    applyEvent 
                    <| state
                    <| evnt
                    <| mailbox
                Logger.Information("GameActor - Processed {@Game}", game)
                stateR
                            
        | GameStatus.Closed when diff > 5.0  ->
                    Logger.Warning("Missed draw date for {@Game}, refunding", game)
                    refund state game GameNotDrawnOnTime
        | _ -> Logger.Warning("Tried to draw on not closed {@Game}", game)
               state

    let isGameInvalid (game : Game) = 
        let pending = (game.Status = GameStatus.Pending && ( SystemTime.UtcNow() - game.OpenedAt).TotalHours > 4.0)
        let opened = (game.Status = GameStatus.Opened && ( SystemTime.UtcNow() - game.OpenedAt).TotalHours > 3.0)
        let closed = (game.Status = GameStatus.Closed && ( SystemTime.UtcNow() - game.OpenedAt).TotalHours > 2.0)
        pending || opened || closed

    let handleRefund state actorRefs mailbox =
        let filter (game : Game) =
            //let invalid = game.Status = GameStatus.Invalid
            //invalid || 
            isGameInvalid game

        let folder state (game : Game) =
            game.Bets |> List.iter(fun(bet) -> actorRefs.SendActorRef <! SendCommand.Refund(bet, GameRefundedError))
            Logger.Information("GameActor - Refunded {@Name}", game.Name)
            applyEvent 
                <| state
                <| { GameRefunded.Name = game.Name }
                <| mailbox

        let gamesToRefund = 
            state.Games 
            |> Map.toSeq
            |> Seq.filter(fun(_, game) -> filter game) 
            |> Seq.map(fun(_, game) -> game)
                    
        let stateR = gamesToRefund |> Seq.fold(fun state game -> folder state game) state
        stateR

    let exec : Exec =
        fun services mailbox state cmd ->
            let save (state : GameState) (mailbox : MailBox) = 
                mailbox.SaveSnapshot state
                state
            match cmd with
            | QueryAll ->
                mailbox.Context.Sender <! state
            | Bet(bet) -> 
                let exists = Querying.tryFindGame state.Games bet.GameName
                match exists with
                | Some(game : Game) when game.Status = Pending || game.Status = Opened ->
                    applyEvent 
                    <| state
                    <| bet
                    <| mailbox
                     |> ignore
                    Logger.Information("GameActor - Bet placed {@Bet}", bet)
                | Some(game) -> 
                    Logger.Error("{@Game} was not opened. {@Bet}", game, bet)
                | _ ->
                    Logger.Error("No matching game was found for {@GameName}. {@Bet}", bet.GameName, bet)
            | Tick ->
                // validate date (fully hour + max 5 minutes)
                let now = SystemTime.UtcNow()
                let referenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
                let diff = (now - referenceDate).TotalMinutes 

                Logger.Information("Tick at {@Now} for {@ReferenceDate}, {@Diff} ", now, referenceDate, diff)
                Logger.Information("SystemTime: {@SystemTime}", SystemTime.UtcNow())
                if (diff <= 5.0) then do

                    (* 
                        EMPTY GAME - 16:00 - 16:59
                        Opened at |	Closing at | Drawing on	| Status
                        17:00       18:00       19:00         Pending  2
                        16:00       17:00       18:00         Opened   1 ===========

                        17:00 - 17:59
                        Opened at |	Closing at | Drawing on	| Status
                        18:00       19:00       20:00         Pending  3
                        17:00       18:00       19:00         Opened   2 ===========
                        16:00       17:00       18:00         Closed   1

                        18:00 - 18:59
                        Opened at |	Closing at | Drawing on	| Status
                        19:00       20:00       21:00         Pending  4
                        18:00       19:00       20:00         Opened   3 ===========
                        17:00       18:00       19:00         Closed   2

                    *)

                    // reference date : 17:00 NOW
                    
                    let stateR1 = 
                       applyEvent 
                       <| state
                       <| { AddNewPendingGame.Date = referenceDate.AddHours(1.0)}
                       <| mailbox

                    // updated pending to opened (if pending do not exists then create new one)
                    let stateR2 = 
                       applyEvent 
                       <| stateR1
                       <| { UpdateToOpenedGame.Date = referenceDate.AddHours(0.0) }
                       <| mailbox

                    // update opened to close (if exists)
                    let stateR3 = 
                       applyEvent 
                       <| stateR2
                       <| { UpdateToClosedGame.Date = referenceDate.AddHours(-1.0) }
                       <| mailbox

                    // closed to drawed or refunded
                    let drawDate = referenceDate.AddHours(-2.0)
                    let gameToDraw = Game.GetName <| drawDate
                    let handleDrawIfGameExists name state =
                        if state.Games.ContainsKey(name) then
                            let game = Map.find (Game.GetName drawDate) stateR3.Games
                            let stateR4 = 
                                handleDraw 
                                <| game
                                <| state
                                <| services
                                <| mailbox
                            stateR4
                        else state

                    let stateR4 = 
                        handleDrawIfGameExists 
                        <| gameToDraw 
                        <| stateR3
 
                    // refund missed games and update them to refunded
                    let stateR5 = 
                        handleRefund 
                        <| stateR4
                        <| services
                        <| mailbox
                        

                    // trim state
                    let stateR6 = 
                       applyEvent 
                       <| stateR5
                       <| new TrimGames()
                       <| mailbox

                    // save snapshot
                    save 
                    <| stateR6
                    <| mailbox
                    |> ignore

                else
                    Logger.Warning("Tick was missed!")

            | Init -> 
                let now = SystemTime.UtcNow()
                let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
                let stateR1 = 
                    applyEvent 
                    <| state
                    <| { AddNewPendingGame.Date = nowReferenceDate.AddHours(1.0);}
                    <| mailbox
                applyEvent 
                <| stateR1
                <| { UpdateToOpenedGame.Date = nowReferenceDate;}
                <| mailbox |> ignore

            | _ -> failwith "game actor does not support this command"

    let run system network state services =
        spawnPersist system "game" {
            state = state
            apply = apply
            exec = exec  services
        } []

