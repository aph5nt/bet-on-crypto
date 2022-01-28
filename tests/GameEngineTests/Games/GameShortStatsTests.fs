namespace GameEngineTests.Games

open Xunit
open FsUnit.Xunit
open System
open Shared
open GameEngine.Games
open GameEngineTests
open Akka.Persistence.FSharp
 

[<Trait("Drawing", "UnitTest")>]
type GameShortStatsTest() =
    do Logging.Init()
 
    [<Fact>]
    let  ``Calculate statistics``() = 
     
        let view = Foq.Mock<View<obj, ShortStat>>().Create()
        let stateR0 = { ShortStat.TotalWon = 0m; ShortStat.HighestWin = 0m; ShortStat.TotalWins = 0L; }
        
        let event1 = {
            Name = "2015-01-16 17:00"
            Win = 0m
            Profit = 0m
            Winners = list<Bet>.Empty
            Status = GameStatus.Drawed;
            Accumulation = 0m
            WinningVote = Some(1.0m) }

        // no winners
        let stateR1 = 
            ShortStats.apply
            <| view
            <| stateR0
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = event1 }
        
        stateR1.HighestWin |> should equal 0m
        stateR1.TotalWins |> should equal 0L
        stateR1.TotalWon |> should equal 0m

        // 1st winner
        let stateR2 = 
            ShortStats.apply
            <| view
            <| stateR1
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = { event1 with Win = 100m; Winners = [{ bet with Bet.Address = "addr-1"; GameName = event1.Name }] } }

        stateR2.HighestWin |> should equal 100m
        stateR2.TotalWins |> should equal 1L
        stateR2.TotalWon |> should equal 100m

        // 2nd winner
        let stateR3 = 
            ShortStats.apply
            <| view
            <| stateR2
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = { event1 with Win = 50m; Winners = [{ bet with Bet.Address = "addr-1" }; { bet with Bet.Address = "addr-2" }] } }

        stateR3.HighestWin |> should equal 100m
        stateR3.TotalWins |> should equal 3L
        stateR3.TotalWon |> should equal 200m

        // no winners again
        let stateR4 = 
            ShortStats.apply
            <| view
            <| stateR3
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = event1 }

        stateR4.HighestWin |> should equal 100m
        stateR4.TotalWins |> should equal 3L
        stateR4.TotalWon |> should equal 200m

         // 3nd winner
        let stateR5 = 
            ShortStats.apply
            <| view
            <| stateR4
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = { event1 with Win = 500m; Winners = [{ bet with Bet.Address = "addr-1" }; { bet with Bet.Address = "addr-2" }] } }

        stateR5.HighestWin |> should equal 500m
        stateR5.TotalWins |> should equal 5L
        stateR5.TotalWon |> should equal 1200m

        // no winners again
        let stateR6 = 
            ShortStats.apply
            <| view
            <| stateR5
            <| { Network = BTCTEST; PlacedOn = DateTime.UtcNow.AddHours(-1.); Payload = event1 }

        stateR6.HighestWin |> should equal 500m
        stateR6.TotalWins |> should equal 5L
        stateR6.TotalWon |> should equal 1200m

        ()