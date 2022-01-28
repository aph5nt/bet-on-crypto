namespace GameEngineTests.Rates

open Xunit
open FsUnit.Xunit
open System
open GameEngine.Rates
open Shared
open GameEngineTests

[<Trait("StorageService", "IntegrationTest")>]
type StorageServiceTest() = 
    do  Logging.Init()
        Storage.Init()
        Storage.CleanUp()
        SystemTime.ResetDateTime()

    let service = Storage.StorageService()

    [<Fact>]
    let ``Insert 5 rates and retrive last 4 rates``() = 
        
        service.InsertRate { Shared.RateTypes.Rate.Date = SystemTime.UtcNow().AddDays(1.0); Shared.RateTypes.Rate.Day7 = 1m; Network = BTCTEST}
        service.InsertRate { Shared.RateTypes.Rate.Date = SystemTime.UtcNow().AddDays(2.0); Shared.RateTypes.Rate.Day7 = 2m; Network = BTCTEST}
        service.InsertRate { Shared.RateTypes.Rate.Date = SystemTime.UtcNow().AddDays(3.0); Shared.RateTypes.Rate.Day7 = 3m; Network = BTCTEST}
        service.InsertRate { Shared.RateTypes.Rate.Date = SystemTime.UtcNow().AddDays(4.0); Shared.RateTypes.Rate.Day7 = 4m; Network = BTCTEST}
        service.InsertRate { Shared.RateTypes.Rate.Date = SystemTime.UtcNow().AddDays(5.0); Shared.RateTypes.Rate.Day7 = 5m; Network = BTCTEST}

        let rates = service.GetLastNRates BTCTEST 4 
        rates |> Array.length |> should equal 4
