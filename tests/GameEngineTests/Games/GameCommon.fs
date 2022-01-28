namespace GameEngineTests.Games

[<AutoOpen>]
module GameCommon =

    open Shared
    open Foq
    open Akka.Persistence.FSharp
    open System
    open Model

    let bet (i: int) date =
        { Bet.TransactionId = i.ToString();
          Bet.Address = sprintf "addr-%d" i;
          Bet.UserName = sprintf "usr-%d" i;
          Bet.Amount = 0.005m
          Bet.PlacedAt = date;
          Bet.GameName = Game.GetName date
          Bet.VoteFor = (decimal i) * 0.2m; }

    let gameActorMock = Mock<Eventsourced<GameCommand, obj, GameState>>().Create()
    let date = new DateTime(2015, 07, 29, 11, 36, 12, 0)
    let date2 = new DateTime(2015, 07, 29, 12, 36, 12, 0)
    let date3 = new DateTime(2015, 07, 29, 13, 36, 12, 0)
    let game = {    Game.Name = Game.GetName date
                    Game.Network = BTCTEST
                    Game.Bets = []
                    Game.OpenedAt = date
                    Game.Status = GameStatus.Opened
                    Game.Winners = []
                    Game.WinningVote = None
                    Game.Win = 0m
                    Game.DrawingAt = date.AddHours(2.0)
                    Game.CompletedAt = date.AddHours(1.0)
                    Game.Profit = 0m }
    let game1Completed = { game with Status = Drawed }
    let game1Winners = { game1Completed with WinningVote = Some(1.2m); Bets = seq { for i in 1 .. 10 do yield bet i date } |> Seq.toList }
    let gameWinners betNum = { game1Completed with WinningVote = Some(5.1m); Bets = seq { for i in 1 .. betNum do yield bet i date } |> Seq.toList }
    let emptyState = { GameState.Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
    let state1Game = { emptyState with Games = Map.ofSeq[(game.Name, game)] }
    let state1Winners = { emptyState with Games = Map.ofSeq[(game1Winners.Name, game1Winners)]; Accumulation = 4000m }
    let rate = { Rate.Date = date; Rate.Day7 = 23.45m; Network = BTCTEST}