namespace GameEngineTests.General

open FsUnit 
open FsUnit.Xunit
open GameEngine
open Akka.FSharp
open Shared
open System

module Arrange =
    
    let genesisDate = new DateTime(1970, 1, 1, 0, 0, 0 ,0, DateTimeKind.Utc)
    let rate = { Rate.Date = SystemTime.UtcNow() ; Rate.Day7 = 5.1m; Network = BTCTEST }
     
module Assert =
 
    let storage = Payments.Storage.StorageService()
    let profitExists game =
        storage.ProfitExists game.Name Settings.Game.ProfitAddress
        |> should be True

//    let widthrawsExists (game : Game) =
//        for winner in game.Winners do
//            storage.WidthrawExists game.Name winner 
//            |> should be True
 
//    let refundExists gameName id  = 
//        let notExists = storage.RefundExists gameName id  
//        notExists |> should be True

    let gameProperties (game : Game) =
        game.Bets.Length |> should equal 100
        game.WinningVote.Value |> should equal 5.1m
       
//        game.Status |> should equal GameStatus.Completed
        game.Winners.Length |> should equal 3
        game.Profit |> should equal 0L
        game.Win |> should equal 3000L

module Actors =
    open Module
 
    let timeout = TimeSpan.FromSeconds(10.0)
//    let createModuleActors paymentGatewayService rateService storageService =
//        Module.create 
//        <| System.create settings.Systems.Server (Configuration.load())
//        <| { Module.AppServices.PaymentGatewayService = paymentGatewayService;   Module.AppServices.RateService = rateService;  Module.AppServices.PaymentStorageService = storageService }
//    let startModule moduleActors = Module.start moduleActors
        
    type ModuleActorsHelper(moduleActors: ModuleActors) =
        member x.GetGameActorState() = moduleActors.GameView.Ask<GameState>(GameViewCommand.QueryAll, timeout) |> Async.RunSynchronously
            


 