open System.Reflection
[<assembly: AssemblyTitle("BetOnCrypto Server")>]
()

open Serilog
open Serilog.Events
open Shared
open GameEngine
open Topshelf
open Time 
open Akka.Actor
open GameEngine.Payments.SendActorTypes
open System.Threading

let settings = Settings.Get()

StackifyLib.Logger.GlobalApiKey <- settings.Stackify.GlobalApiKey
StackifyLib.Logger.GlobalAppName <- settings.Stackify.GlobalAppName
StackifyLib.Logger.GlobalEnvironment <- settings.Stackify.GlobalEnvironment
let loggerCfg = new Serilog.LoggerConfiguration()
Log.Logger <- loggerCfg.MinimumLevel.Verbose()
               .WriteTo.Console(LogEventLevel.Debug)
               .WriteTo.Stackify()
               .CreateLogger();

[<EntryPoint>]
let main argv = 

    let mutable actorModule : obj = null
    let system = ActorSystem.Create(settings.Systems.Server)
    
    match settings.Enviroment.Configuration with
    | DEBUG
    | STAGING
    | RELEASE ->
        let sendActorService = { SendActorService.Storage = GameEngine.Payments.Storage.StorageService(); SendActorService.Gateway = BlockApi.BlockApi.Api()}
        let appServices = { Module.AppServices.RateQueryService = Rates.Query.RateQueryService();
                            Module.AppServices.RateStorageService = Rates.Storage.StorageService();
                            Module.AppServices.SendActorService = sendActorService }
        actorModule <- Module.Create BTCTEST system appServices

    let start hc = 
        Module.Start(actorModule :?> Module.ModuleActors)
        Logger.Information("Service started in {@Mode}", settings.Enviroment.Configuration |> toString);
        Logger.Information("Current settings : {@Settings}", settings)
        true

    let stop hc =
        (actorModule :?> Module.ModuleActors).ServerActor.Tell(new Commands.ServerCommand.ServerShutdown())
        System.Threading.Thread.Sleep(10000)
        Module.Kill(actorModule :?> Module.ModuleActors)
        System.Threading.Thread.Sleep(1000)
        Logger.Information("Service stopped.");
        true
    
    Service.Default
    |> service_name  "BetOnCryptoServer"
    |> instance_name "BetOnCrypto Server"
    |> display_name  "BetOnCrypto Server"
    |> run_as_network_service 
    |> start_auto 
    |> with_start start
    |> with_recovery (ServiceRecovery.Default |> restart (min 1))
    |> with_stop stop
    |> run