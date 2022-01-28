namespace GameEngineTests.General

open Xunit
open Xunit.Extensions
open FsUnit.Xunit
open Akka
open Akka.FSharp
open Akka.Actor
open System
open GameEngine
open Shared
open GameEngine.Payments
open GameEngine.Rates
open GameEngine.Games
open Games.Types
open GameEngineTests
open SendActorTypes
open BlockApi.Types

[<Trait("GameActor", "IntegrationTest")>]
type GameActorScenarios() =
    do 
        Logging.Init()
        Payments.Storage.CleanUp()
        Shared.Storage.CleanUpBlobs()
 
    let rateService = { RateQueryService.Get = fun _ -> Some(Arrange.rate) }
    let sleep seconds = System.Threading.Thread.Sleep(seconds * 1000)

    let getBet date =
        { bet with GameName = Game.GetName date; PlacedAt = date; VoteFor = 1.0m }

    let gateway = {
            GetAddressBalanceByAddresses = (fun _ _ -> Fail("fail"))
            GetAddressBalanceByLabels = (fun _ _ -> Fail("fail"))
            GetNewAddress = (fun _ -> Fail("fail"))
            GetMyAddresses = (fun _ -> Fail("fail"))
            WithdrawFromAddresses = (fun _ _ fromAddresses toAddresses amounts -> Fail("fail")) 
            GetNetworkFeeEstimate = (fun _ _ _ -> Fail("fail"))
            }

    let getGameActorWith state gateway =
        let rateQuery = { Get = fun(network) -> Some( { Rate.Date = SystemTime.UtcNow();  Rate.Day7 = 1.0m; Network = network })}
 
        let services = { 
            Module.AppServices.RateQueryService = rateQuery
            Module.AppServices.SendActorService = { Storage = Payments.Storage.StorageService(); Gateway = gateway }
            Module.AppServices.RateStorageService = Rates.Storage.StorageService() } 

        let system = System.create "TestSystem" (Configuration.load())
        let rateActorRef = RateActor.run system BTCTEST 48 { RateQueryService = services.RateQueryService; Storage = services.RateStorageService }
        
        let sendActorRef = SendActor.run system BTCTEST <| services.SendActorService
        let gameActorServices =  { SendActorRef = sendActorRef; RateActorRef = rateActorRef; }
        let gameActorRef = Games.GameActor.run system BTCTEST state gameActorServices
        sleep(6)
        gameActorRef

    let getGameActor (state : GameState) = 
        getGameActorWith 
        <| state 
        <| gateway
        
    let getGames (gameActorRef : IActorRef) =
        let state = gameActorRef.Ask<GameState>(GameCommand.QueryAll, TimeSpan.FromSeconds(120.0)) |> Async.RunSynchronously
        let games = state.Games |> Map.toSeq |> Seq.map(fun (key, game) -> game) |> Seq.sortByDescending(fun(game) -> game.Name) |> Seq.toArray
        games

    [<Fact>]
    let ``Place bet on pending game``() =
        // Arrange
        let now = SystemTime.UtcNow()
        let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
        SystemTime.SetDateTime(nowReferenceDate)

         // Init
        let gameActorRef = getGameActor { Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
        
        gameActorRef <! Commands.GameCommand.Init
        sleep(6)

        gameActorRef <! Bet(getBet <| SystemTime.UtcNow().AddHours(1.))

        // Assert
        let games = getGames gameActorRef
        displayGames games
        games.[0].Status |> should equal Pending
        games.[0].Bets.Length |> should equal 1

    [<Fact>]
    let ``Tick empty``() =
        // Arrange
        let now = SystemTime.UtcNow()
        let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
        SystemTime.SetDateTime(nowReferenceDate)
         
         // Act
        let gameActorRef = getGameActor { Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
        gameActorRef <! Tick

        // Assert
        let games = getGames gameActorRef
        displayGames games
        games.[0].Status |> should equal Pending
        games.[1].Status |> should equal Opened
 
    [<Fact>]
    let ``Tick should widthraw ``() =
        // Arrange
        let now = SystemTime.UtcNow()
        let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
        SystemTime.SetDateTime(nowReferenceDate)

        // pending
        let stateR1 = 
            Games.GameActor.update 
            <| { Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = SystemTime.UtcNow().AddHours(2.0) }; PlacedOn = DateTime.UtcNow }

        // Opened
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = SystemTime.UtcNow().AddHours(1.0) }; PlacedOn = DateTime.UtcNow }

        // Closed with 1 bet
        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { Network = BTCTEST;  GameEvent.Payload = { UpdateToOpenedGame.Date = SystemTime.UtcNow() }; PlacedOn = DateTime.UtcNow }
   
        let stateR4 = 
            GameActor.update
            <| stateR3
            <| { Network = BTCTEST; GameEvent.Payload = (getBet nowReferenceDate); PlacedOn = DateTime.UtcNow } 

        let stateR5 = 
            GameActor.update 
            <| stateR4
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToClosedGame.Date = SystemTime.UtcNow() }; PlacedOn = DateTime.UtcNow }

        // Act
        let gameActorRef = getGameActorWith stateR5 gateway
        SystemTime.SetDateTime(nowReferenceDate.AddHours(2.0))
        gameActorRef <! Tick

        // Assert
        let state = gameActorRef.Ask<GameState>(GameCommand.QueryAll, TimeSpan.FromSeconds(120.0)) |> Async.RunSynchronously
        let drawed = Map.find (Game.GetName nowReferenceDate) state.Games
        drawed.Status |> should equal GameStatus.Drawed
        drawed.WinningVote.Value |> should equal 1.0m
        drawed.Winners.Length |> should equal 1

        Payments.Storage.WidthrawExists bet
        |> should be False

    [<Fact>]
    let ``Tick cycles``() =
        // Arrange
        let now = SystemTime.UtcNow()
        let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
        SystemTime.SetDateTime(nowReferenceDate)

         // Init
        let gameActorRef = getGameActor { Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
        gameActorRef <! Commands.GameCommand.Init
        sleep(6)

        let games = getGames gameActorRef
        displayGames games
        games.[0].Status |> should equal Pending
        games.[1].Status |> should equal Opened
        
        for n in [1..32] do
            SystemTime.SetDateTime(nowReferenceDate.AddHours((float n)))
            gameActorRef <! Tick
            let games = getGames gameActorRef
            displayGames games

            if n > 0 then
                games.[0].Status |> should equal Pending
            if n > 1 then
                games.[1].Status |> should equal Opened
            if n > 2 then
                games.[2].Status |> should equal Closed
            if n > 3 then
                match games.[3].Status with
                | Drawed -> ()
                | Refunded -> ()
                | _ -> failwith "invalid game status"
            if n > 4 then
                match games.[games.Length-1].Status with
                | Drawed -> ()
                | Refunded -> ()
                | _ -> failwith "invalid game status"
            else ()

    [<Fact>]
    let ``Tick with refunding``() =
        // Arrange
        let now = SystemTime.UtcNow()
        let nowReferenceDate =  new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0, DateTimeKind.Utc)
        SystemTime.SetDateTime(nowReferenceDate)

        // pending
        let stateR1 = 
            Games.GameActor.update 
            <| { Games = Map.empty; Accumulation = 0m; Network = BTCTEST }
            <| { Network = BTCTEST; GameEvent.Payload = { AddNewPendingGame.Date = SystemTime.UtcNow().AddHours(2.0) }; PlacedOn = DateTime.UtcNow }

        // Opened
        let stateR2 = 
            GameActor.update 
            <| stateR1
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = SystemTime.UtcNow().AddHours(1.0) }; PlacedOn = DateTime.UtcNow }

        // Closed with 1 bet
        let stateR3 = 
            GameActor.update 
            <| stateR2
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToOpenedGame.Date = SystemTime.UtcNow() }; PlacedOn = DateTime.UtcNow }

         
        let stateR4 = 
            GameActor.update
            <| stateR3
            <| { Network = BTCTEST; GameEvent.Payload = (getBet nowReferenceDate); PlacedOn = DateTime.UtcNow }

        let stateR5 = 
            GameActor.update 
            <| stateR4
            <| { Network = BTCTEST; GameEvent.Payload = { UpdateToClosedGame.Date = SystemTime.UtcNow() }; PlacedOn = DateTime.UtcNow }

        // Act
        let gameActorRef = getGameActor stateR5
        SystemTime.SetDateTime(nowReferenceDate.AddHours(10.0))
        gameActorRef <! Tick
       
        // Assert
        let state = gameActorRef.Ask<GameState>(GameCommand.QueryAll, TimeSpan.FromSeconds(120.0)) |> Async.RunSynchronously
        let refunded = Map.find (Game.GetName nowReferenceDate) state.Games
        refunded.Status |> should equal Refunded