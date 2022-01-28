namespace GameEngineTests.Payments

open Xunit
open FsUnit.Xunit
open System
open GameEngine.Payments
open Shared
open GameEngineTests
open BlockApi.Types
open GameEngine.Payments.StorageTypes
open SendActorTypes

type SendActorTest() as x =
    do  Logging.Init()
        Storage.Init()
        Storage.CleanUp()

    let fee = 0.0001m
    let storage = Storage.StorageService()
    let gateway = {
        GetAddressBalanceByAddresses = (fun _ _ -> Fail("fail"))
        GetAddressBalanceByLabels = (fun _ _ -> Fail("fail"))
        GetNewAddress = (fun _ -> Fail("fail"))
        GetMyAddresses = (fun _ -> Fail("fail"))
        WithdrawFromAddresses = (fun _ _ fromAddresses toAddresses amounts -> x.WithdrawFromAddresses fromAddresses toAddresses amounts; Fail("fail"))
        GetNetworkFeeEstimate = (fun _ _ _ -> Success(fee)) }
    let service = { Storage = storage; Gateway = gateway }

    [<DefaultValue>] val mutable FromAddresses : Addresses
    [<DefaultValue>] val mutable ToAddresses : Addresses
    [<DefaultValue>] val mutable Amounts : Amounts
    [<DefaultValue>] val mutable Invoked : bool

    member x.WithdrawFromAddresses fromAddresses toAddresses amounts  =
        x.FromAddresses <- fromAddresses
        x.ToAddresses <- toAddresses
        x.Amounts <- amounts
        x.Invoked <- true
    member x.Service = service
    member x.Fee = fee
    override x.Finalize() = 
        Storage.CleanUp()

[<Trait("SendActor", "UnitTest")>]
type ``SendActor Profit``() as x =
    inherit SendActorTest()
    
    [<Fact>]
    let  ``Send profit with fee``() =
        SendActor.Profit
        <| x.Service
        <| bet.GameName
        <| SystemTime.UtcNow()
        <| 1000m

        // check if profit exists
        x.Service.Storage.ProfitExists
        <| bet.GameName
        <| Settings.Game.ProfitAddress
        |> should be True

        // check if send was invoked
        x.FromAddresses |> should contain Settings.Game.IncomingAddress
        x.ToAddresses |> should contain Settings.Game.ProfitAddress
        x.Amounts |> should contain (1000m - x.Fee)
        x.Invoked |> should be True

        // reset
        x.Invoked <- false

        // try again to take profit
        SendActor.Profit
        <| x.Service
        <| bet.GameName
        <| SystemTime.UtcNow()
        <| 1000m

        // send should not be invoked
        x.Invoked |> should be False

    [<Fact>]
    let  ``Do not take profit when amount is lessOrEq than fee``() =
        SendActor.Profit
        <| x.Service
        <| bet.GameName
        <| SystemTime.UtcNow()
        <| x.Fee

        // send should not be invoked
        x.Invoked |> should be False
            

[<Trait("SendActor", "UnitTest")>]
type ``SendActor Refund``() as x =
    inherit SendActorTest()
     
    [<Fact>]
    let  ``Refund coins with fee``() =
        let bet = { bet with Amount = (100m) }
        SendActor.Refund
        <| x.Service
        <| bet
        <| GameRefundedError

        // check if widthraw exists
        x.Service.Storage.RefundExists
        <| bet
        |> should be True

        // check if send was invoked
        x.FromAddresses |> should contain Settings.Game.IncomingAddress
        x.ToAddresses |> should contain bet.Address
        x.Amounts |> should contain (100m - x.Fee)
        x.Invoked |> should be True

        // reset
        x.Invoked <- false

        // try again to refund coins
        SendActor.Refund
        <| x.Service
        <| bet
        <| GameRefundedError

        // send should not be invoked
        x.Invoked |> should be False

    [<Fact>]
    let  ``Do not refund coins when amount is lessOrEq than fee``() =
        let bet = { bet with Amount = x.Fee }
        let storage = Storage.StorageService()

        // refund coins
        SendActor.Refund
        <| x.Service
        <| bet
        <| GameRefundedError

        // send should not be invoked
        x.Invoked |> should be False

[<Trait("SendActor", "UnitTest")>]
type ``SendActor Widthraw``() as x =
    inherit SendActorTest()
    let storage = Storage.StorageService()

    [<Fact>]
    let  ``Widthraw coins with fee``() =
        // widthraw coins
        SendActor.Widthraw
        <| x.Service
        <| 1000m
        <| bet

        // check if widthraw exists
        storage.WidthrawExists
        <| bet
        |> should be True

        // check if send was invoked
        x.ToAddresses |> should contain bet.Address
        x.FromAddresses |> should contain Settings.Game.IncomingAddress
        x.Amounts |> should contain (1000m - x.Fee)
        x.Invoked |> should be True

        // reset
        x.Invoked <- false

        // try again to widthraw coins
        SendActor.Widthraw
        <| x.Service
        <| 1000m
        <| bet

        // send should not be invoked
        x.Invoked |> should be False

    [<Fact>]
    let  ``Do not widthraw coins when amount is lessOrEq than fee``() =
        // widthraw coins
        SendActor.Widthraw
        <| x.Service
        <| x.Fee
        <| bet

        // send should not be invoked
        x.Invoked |> should be False