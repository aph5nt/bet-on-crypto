namespace GameEngineTests.Games

open Xunit
open FsUnit.Xunit
open System
open Shared
open Chessie.ErrorHandling
open GameEngine.Games
open GameEngineTests

[<Trait("GameActor", "UnitTest")>]
type ``GameActor Update``() =
    do Logging.Init()
    let date = SystemTime.UtcNow()
    let gameName = Game.GetName date
    let now = SystemTime.UtcNow()
    let referenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
    let pendingDate = referenceDate.AddHours(1.0)

    [<Fact>]
    let ``Add bet to opened game``() =
        // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; GameEvent.PlacedOn = referenceDate }
        
        let betR = (GameCommon.bet 1 referenceDate)
        let stateR2 = GameActor.update stateR1 { Network = BTCTEST; GameEvent.Payload = betR; PlacedOn = referenceDate }
       
        stateR2.Games.Count |> should equal 1
        stateR2.Games.[betR.GameName].Bets.[0].TransactionId |> should equal betR.TransactionId
        stateR2.Accumulation |> should equal 0m
    
    [<Fact>]
    let ``Add  bet to pending game``() = 
        // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = referenceDate }; PlacedOn = referenceDate }
        
        let betR = (GameCommon.bet 1 referenceDate)
        let stateR2 = GameActor.update stateR1 { Network = BTCTEST; GameEvent.Payload = betR; PlacedOn = referenceDate }
       
        stateR2.Games.Count |> should equal 1
        stateR2.Games.[betR.GameName].Bets.[0].TransactionId |> should equal betR.TransactionId
        stateR2.Accumulation |> should equal 0m

    [<Fact>]
    let ``Add pending game``() = 
        // Act
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = referenceDate }; PlacedOn = referenceDate }
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| {Network = BTCTEST;  GameEvent.Payload = { AddNewPendingGame.Date = referenceDate }; PlacedOn = referenceDate }
        
        // Assert
        let gameName = Game.GetName referenceDate
        stateR2.Games.[gameName].Status |> should equal Pending
        stateR2.Games.Count |> should equal 1
 
    [<Fact>]
    let ``Update pending game to opened if exists``() = 
        // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Act
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }
        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Assert
        stateR3.Games.[Game.GetName referenceDate].Status |> should equal Opened
        stateR3.Games.Count |> should equal 1

    [<Fact>]
    let ``Add opened game if no pending game exists``() = 
        // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = pendingDate }; PlacedOn = pendingDate }
        
        // Act
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| {Network = BTCTEST;  GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Assert
        stateR2.Games.[Game.GetName pendingDate].Status |> should equal Pending
        stateR2.Games.[Game.GetName referenceDate].Status |> should equal Opened
        stateR2.Games.Count |> should equal 2

    [<Fact>]
    let ``Update opened game to closed if exists``() = 
        // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Act
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToClosedGame.Date = referenceDate }; PlacedOn = referenceDate }
        
        // Assert
        stateR2.Games.[Game.GetName referenceDate].Status |> should equal Closed
        stateR2.Games.Count |> should equal 1

    [<Fact>]
    let ``Update closed game to drawed if exists``() = 
         // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }
        
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToClosedGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Act
        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { GameEvent.PlacedOn = referenceDate; Network = BTCTEST;
                 GameEvent.Payload = {  Name = Game.GetName referenceDate
                                        Win = 0m
                                        Profit = 0m
                                        Winners = []
                                        Status = GameStatus.Drawed
                                        WinningVote = Some(1.0m)
                                        Accumulation = 0m} }

        // Assert
        stateR3.Games.[Game.GetName referenceDate].Status |> should equal Drawed
        stateR3.Games.Count |> should equal 1


    [<Fact>]
    let ``Update closed game to refunded if exists``() = 
         // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }
        
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToClosedGame.Date = referenceDate }; PlacedOn = referenceDate }

        // Act
        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { Network = BTCTEST; GameEvent.Payload = { GameRefunded.Name = Game.GetName referenceDate }; PlacedOn = referenceDate }

        // Assert
        stateR3.Games.[Game.GetName referenceDate].Status |> should equal Refunded
        stateR3.Games.Count |> should equal 1

    [<Fact>]
    let ``Update outdated games to refunded if exists``() = 
         // Arrange
        let stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = pendingDate }; PlacedOn = pendingDate }
        
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }

        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate.AddHours(-1.0) }; PlacedOn = referenceDate.AddHours(-1.0) }

        // Act
        let stateR4 = 
           GameActor.update 
           <| stateR3
           <| { Network = BTCTEST; GameEvent.Payload = { GameRefunded.Name = Game.GetName pendingDate }; PlacedOn = pendingDate }

        let stateR5 = 
           GameActor.update 
           <| stateR4
           <| { Network = BTCTEST; GameEvent.Payload = { GameRefunded.Name = Game.GetName referenceDate}; PlacedOn = referenceDate }

        let stateR6 = 
           GameActor.update 
           <| stateR5
           <| { Network = BTCTEST; GameEvent.Payload = { GameRefunded.Name = (Game.GetName <| referenceDate.AddHours(-1.0)) }; PlacedOn = referenceDate.AddHours(-1.0) }
         
        // Assert
        stateR6.Games.[Game.GetName pendingDate].Status |> should equal Refunded
        stateR6.Games.[Game.GetName referenceDate].Status |> should equal Refunded
        stateR6.Games.[(Game.GetName <| referenceDate.AddHours(-1.0))].Status |> should equal Refunded
        stateR6.Games.Count |> should equal 3

    [<Fact>]
    let ``Remove 25th+ game``() = 
        // Arrange
        let mutable stateR1 = 
            GameActor.update 
            <| emptyState
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate }; PlacedOn = referenceDate }

        for i in [1..30] do
            stateR1 <- GameActor.update 
                        <| stateR1
                        <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = referenceDate.AddHours((float i)) }; PlacedOn = referenceDate.AddHours((float i)) }
            

        stateR1.Games.Count |> should equal 31
        let stateR2 =
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = new TrimGames(); PlacedOn = referenceDate }

        stateR2.Games.Count |> should equal 24