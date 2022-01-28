namespace GameEngineTests.General

open Xunit
open FsUnit.Xunit
open Shared
open Akka.Actor
open Akka.FSharp
open System
open GameEngine.Publishers
open GameEngine.Games
open GameEngine.Games.Types
open GameEngineTests.TestData
open Shared.GameTypes.GameUIEvents

[<Trait("TwitterPublisher", "IntegrationTest")>]
type TwitterPublisherScenario() =

    let settings = Settings.Get()
 
    [<Fact>]
    let ``Publish tweet for drawed games with winners``() =
       let system = Akka.FSharp.System.create "TestSystem" (Akka.FSharp.Configuration.load())  
        
       let twitterPublisher = TwitterPublisherActor.run system BTCTEST
       
       let evnt = { 
        GameDrawedData =  
            { Accumulation = 0m
              Name = "GameName"
              Profit = 100m
              Win = 100m
              Winners = [{ bet with Bet.Address = "addr-1" };]
              WinningVote = Some(23m) } }
       

       twitterPublisher <! evnt
       Async.Sleep(3000) |> Async.RunSynchronously
       twitterPublisher.Tell(PoisonPill.Instance)
       