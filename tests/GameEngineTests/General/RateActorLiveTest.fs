namespace GameEngineTests.General

open Xunit
open FsUnit.Xunit
open GameEngineTests
open Shared
open Akka.Actor
open Akka.FSharp
open Shared.Commands.RateCommand
open GameEngine.Rates
open GameEngine.Rates.Types
open System

[<Trait("RateActor", "IntegrationTest")>]
type RateActorScenario() =
    do  Logging.Init()
        GameEngine.Rates.Storage.CleanUp()
        sleep(1)

    let settings = Settings.Get()
 
    [<Fact>]
    let ``Fetch and query rates``() =
        let system = Akka.FSharp.System.create "TestSystem" (Akka.FSharp.Configuration.load())  
        let rateActorRef = RateActor.run system BTCTEST 4 { RateQueryService = Query.RateQueryService(); Storage = Storage.StorageService(); }
       
        for i in [0..8] do 
            rateActorRef <! new Fetch()

        let rates = rateActorRef.Ask<GetRate>(new Query(), TimeSpan.FromSeconds(30.0)) |> Async.RunSynchronously
        rates.DataSets.Length |> should equal 1
        rateActorRef.Tell(PoisonPill.Instance)
        sleep(1)