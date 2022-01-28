namespace GameEngineTests.Payments

open Xunit
open FsUnit.Xunit
open System
open Shared
open GameEngine.Payments
open GameEngineTests

[<Trait("StorageService", "IntegrationTest")>]
type StorageServiceTest() = 
    do  Logging.Init()
        Storage.Init()
        Storage.CleanUp()
 
    let service = Storage.StorageService()
 
    [<Fact>]
    let ``Insert widthraw and check if it exists``() = 
        service.InsertWidthraw 
        <| bet 
        <| 0.1m

        let exists =
            service.WidthrawExists
            <| bet

        exists |> should be True

    [<Fact>]
    let ``Insert refund and check if it exists``() = 
        service.InsertRefund 
        <| bet 
        <| "Bet refunded"

        let exists =
            service.RefundExists
            <| bet

        exists |> should be True

    [<Fact>]
    let ``Insert profit and check if it exists``() = 
        service.InsertProfit
        <| bet.GameName
        <| Settings.Game.ProfitAddress
        <| SystemTime.UtcNow()
        <| 1m

        let exists =
           service.ProfitExists
           <| bet.GameName
           <| Settings.Game.ProfitAddress

        exists |> should be True

    override x.Finalize() = 
        Storage.CleanUp()