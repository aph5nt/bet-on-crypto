namespace GameEngineTests.General


open Xunit
open FsUnit.Xunit
open Shared
open GameEngine.Balances
open Akka
open Akka.FSharp
open Akka.Actor
open GameEngine
open System
open Shared
open BalanceTypes.BalanceCommands
open BlockApi
open BlockApi.Types

[<Trait("BalanceActor", "IntegrationTest")>]
type ``BalanceActorScenarios``() = 
    do GameEngineTests.Logging.Init()
 
    [<Fact>]
    let ``Update address balance``() = 

        let system = System.create "TestSystem" (Configuration.load())
        let balanceActorRef = BalanceActor.run system <| Network.BTCTEST
        
        System.Threading.Thread.Sleep(3000)

        let defaultAddress = "2N3Lqtkjh8rbv55LDhL1EEEULFpC8pqxWYA"
        let secondaryAddress = "2N46o6DPPjLaUJyeFENyKUtoiRGt7eqzzed"

        let defaultBalance = balanceActorRef.Ask<decimal>({ GetBalance.Address = defaultAddress}, TimeSpan.FromSeconds(300.)) |> Async.RunSynchronously
        let secondaryBalance = balanceActorRef.Ask<decimal>({ GetBalance.Address = secondaryAddress} , TimeSpan.FromSeconds(300.)) |> Async.RunSynchronously

        let withdrawResult = 
            BlockApi.WithdrawFromAddresses
            <| Settings.PaymentGateway.ApiKey
            <| Settings.PaymentGateway.Pin
            <| [|defaultAddress|]
            <| [|secondaryAddress|]
            <| [|0.0001m|]

        try
            match withdrawResult with
            | Success(value) -> 
                value.AmountSent |> should equal 0.0001m
                value.TxId |> should not' Empty
                value.Network |> should equal (Network.toString Network.BTCTEST)
            | Fail(msg) -> ()
        with exn ->
            ()
       
        System.Threading.Thread.Sleep(6000)

        let defaultBalanceAfter = balanceActorRef.Ask<decimal>({ GetBalance.Address = defaultAddress}, TimeSpan.FromSeconds(300.)) |> Async.RunSynchronously
        let secondaryBalanceAfter = balanceActorRef.Ask<decimal>({ GetBalance.Address = secondaryAddress}, TimeSpan.FromSeconds(300.)) |> Async.RunSynchronously
        
        defaultBalance > defaultBalanceAfter |> should equal true
        secondaryBalance < secondaryBalanceAfter |> should equal true