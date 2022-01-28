namespace GameEngineTests.Rates

open Xunit
open FsUnit.Xunit
open System
open GameEngine.Rates.Query
open Shared
open GameEngineTests
 
[<Trait("Query", "IntegrationTest")>]
type RateServiceTest() = 
    do SystemTime.ResetDateTime()
    let service = RateQueryService()

    [<Fact>]
    let ``Query coinmarketcap for current 7d change rate``() = 
        let result = service.Get BTC
        sleep(1)
        let now = DateTime.UtcNow
        match result with
        | None -> failwith "unable to query coinmarketcap"
        | Some(rate) -> rate.Date |> should lessThan now
 