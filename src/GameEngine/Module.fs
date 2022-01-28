namespace GameEngine

open Akka.Actor
open Shared
open Akka.FSharp
open GameEngine.Payments
open GameEngine.Rates
open GameEngine.Publishers
open GameEngine.Scheduler

module Module =
    open GameEngine.Games
    open GameEngine.Balances
    open GameEngine.Connectivity
    open System.Threading
 
    type AppServices = {  RateQueryService : RateQueryService;
                          RateStorageService : Rates.StorageTypes.StorageService;
                          SendActorService : SendActorTypes.SendActorService }
    
    type ModuleActors = { 
        SendActor : IActorRef;
        RateActor : IActorRef;
        GameActor : IActorRef;
        BalanceActor : IActorRef
        GameView : IActorRef;
        ServerActor : IActorRef;
        ShortStatView: IActorRef;
        TwitterPublisherActor : IActorRef;
        System : ActorSystem;
    }

    type Create = Network -> ActorSystem -> AppServices -> ModuleActors
    type Build = AppServices
    type Start = ModuleActors -> unit
    type Kill =  ModuleActors -> unit
 
    let emptyActor system name =
         spawn system name
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()
                    match message with
                    |m -> Logger.Information(name + " received: {@Message}", m)
                    return! loop()
                }
                loop())
    
    let mutable gameTimer : Timer = null
    let mutable rateTimer : Timer = null

    let Create : Create =
        fun network system services ->
         
            let j = Akka.Persistence.Persistence.Instance.Apply(system).JournalFor(null)
 
            // Core
            let sendActorRef = SendActor.run system network services.SendActorService
            let rateActorRef = RateActor.run system network 36 { Rates.Types.RateActorServices.RateQueryService = services.RateQueryService; Rates.Types.RateActorServices.Storage = services.RateStorageService }
            let gameActorRef = GameActor.run system network { Games = Map.empty; Accumulation = 0m; Network = network } { RateActorRef = rateActorRef; SendActorRef = sendActorRef; }
            let gameViewRef  = GameView.run system network
            let twitterPublisherActorRef = TwitterPublisherActor.run system network
            let serverActorRef  = ServerActor.run system network
            let balanceActorRef = BalanceActor.run system network

            // Subscriptions
            system.EventStream.Subscribe(twitterPublisherActorRef, typeof<GameUIEvents.OnGameDrawed>)  |> ignore

            // Views
            let shortStatViewRef = ShortStats.run system network
            
            Logger.Information("Bootstrapper - module created.")

            { SendActor = sendActorRef 
              RateActor = rateActorRef
              GameActor = gameActorRef
              GameView = gameViewRef
              ServerActor = serverActorRef
              BalanceActor = balanceActorRef
              ShortStatView = shortStatViewRef
              TwitterPublisherActor = twitterPublisherActorRef
              System = system }

    let Start : Start =
        fun moduleActor ->
            moduleActor.GameActor <! Commands.GameCommand.Init
            gameTimer <- schedule {
                let! job = ("0 * * * *", fun() -> System.Threading.Thread.Sleep(1500); moduleActor.GameActor <! Commands.GameCommand.Tick )
                return job
            }
            rateTimer <- schedule {
                let! job = ("0,30 * * * *", fun() -> System.Threading.Thread.Sleep(1500); moduleActor.RateActor <! new Commands.RateCommand.Fetch() )
                return job
            }
            Logger.Information("Bootstrapper - module started.")

    let Kill : Kill =
        fun moduleActor ->
             gameTimer.Dispose()
             rateTimer.Dispose()
             moduleActor.SendActor.Tell(PoisonPill.Instance)
             moduleActor.RateActor.Tell(PoisonPill.Instance)
             moduleActor.GameActor.Tell(PoisonPill.Instance)
             moduleActor.GameView.Tell(PoisonPill.Instance)
             moduleActor.BalanceActor.Tell(PoisonPill.Instance)
             moduleActor.ServerActor.Tell(PoisonPill.Instance)
             moduleActor.ShortStatView.Tell(PoisonPill.Instance)
             moduleActor.TwitterPublisherActor.Tell(PoisonPill.Instance)
             moduleActor.System.Dispose()
             Logger.Information("Bootstrapper - module killed.")