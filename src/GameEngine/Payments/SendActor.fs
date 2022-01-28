namespace GameEngine.Payments


open Akka.FSharp
open System
open Shared
open Shared.Model

module SendActorTypes = 
    type SendActorService = { Storage : StorageTypes.StorageService; Gateway : BlockApi.Types.Api }
    type WidthrawRequest = SendActorService -> Amount -> Bet -> unit
    type RefundRequest = SendActorService -> Bet -> FailureMessage -> unit
    type ProfitRequest = SendActorService -> GameName -> DateTime -> Amount -> unit

module SendActor =
    open SendActorTypes
    open BlockApi.Types
  
    let getFee service address amounts  =
        try
            let result = 
                service.Gateway.GetNetworkFeeEstimate
                <| Settings.PaymentGateway.ApiKey
                <| address
                <| amounts
            match result with
            | Success(fee) -> Logger.Information("SendActor - GetNetworkFeeEstimate - {@Fee}", fee)
                              Some(fee)
            | Fail(msg) -> Logger.Error("SendActor - GetNetworkFeeEstimate Failed for {@Addresses} {@Amounts} - {@Msg}.", address, amounts, msg)
                           None
        with exn ->
            Logger.Error(exn, "Failed get the network fee.")
            None

    let Widthraw : WidthrawRequest =
        fun service amount bet ->
            try
                let fee = getFee service [|bet.Address|] [|amount|]
                match fee with
                | None -> 
                    Logger.Error("Failed to widthraw.")
                | Some(fee) -> 
                    let amountToWidthraw = amount - fee
                    if amountToWidthraw > fee then 
                        if  service.Storage.WidthrawExists bet |> not then
                            service.Storage.InsertWidthraw
                            <| bet 
                            <| amountToWidthraw
                            let withdrawResult = 
                                service.Gateway.WithdrawFromAddresses
                                <| Settings.PaymentGateway.ApiKey
                                <| Settings.PaymentGateway.Pin
                                <| [|Settings.Game.IncomingAddress|]
                                <| [|bet.Address|]
                                <| [|amountToWidthraw|]
                            match withdrawResult with
                            | Success(value) -> Logger.Information("SendActor - Withdrawn {@GameName} - {@AmountToWidthraw} {@Network} to {@Account}.", bet.GameName, value.AmountSent, value.Network, bet.Address)
                            | Fail(msg) -> Logger.Error("SendActor - Withdrawn Failed for {@GameName} - {@Msg}.", bet.GameName, msg)
            with exn -> 
                Logger.Error(exn, "Failed to widthraw.")
      
    let Refund : RefundRequest =         
        fun service bet failure -> 
            try
                let fee = getFee service [|bet.Address|] [|bet.Amount|]
                match fee with
                | None -> 
                    Logger.Error("Failed to refund.")
                | Some(fee) -> 
                    let amountToRefund = bet.Amount - fee
                    if amountToRefund > fee then 
                        if  service.Storage.RefundExists bet |> not then
                            service.Storage.InsertRefund 
                            <| { bet with Bet.Amount = amountToRefund }
                            <| FailureMessages.[failure]
                            let withdrawResult = 
                                service.Gateway.WithdrawFromAddresses
                                <| Settings.PaymentGateway.ApiKey
                                <| Settings.PaymentGateway.Pin
                                <| [|Settings.Game.IncomingAddress|]
                                <| [|bet.Address|]
                                <| [|amountToRefund|]
                            match withdrawResult with
                            | Success(value) -> Logger.Information("SendActor - Refunded {@GameName} - {@AmountToWidthraw} {@Network} to {@Account}.", bet.GameName, value.AmountSent, value.Network, bet.Address)
                            | Fail(msg) -> Logger.Error("SendActor - Refund failed for {@GameName}, {@Bet} - {@Msg}.", bet.GameName, bet, msg)
            with exn -> 
                Logger.Error(exn, "Failed to refund {@Bet}", bet)
    
    let Profit : ProfitRequest =
        fun service gameName playedAt profit ->
            try
                let fee = getFee service [|Settings.Game.ProfitAddress|] [|profit|]
                match fee with
                | None -> 
                    Logger.Error("Failed to profit.")
                | Some(fee) -> 
                    let amountToProfit = profit - fee
                    if amountToProfit > fee then 
                        if service.Storage.ProfitExists gameName Settings.Game.ProfitAddress |> not then
                            service.Storage.InsertProfit 
                            <| gameName 
                            <| Settings.Game.ProfitAddress
                            <| playedAt 
                            <| amountToProfit
                            let withdrawResult = 
                                service.Gateway.WithdrawFromAddresses
                                <| Settings.PaymentGateway.ApiKey
                                <| Settings.PaymentGateway.Pin
                                <| [|Settings.Game.IncomingAddress|]
                                <| [|Settings.Game.ProfitAddress|]
                                <| [|amountToProfit|]
                            match withdrawResult with
                            | Success(value) -> Logger.Information("SendActor - Profit {@GameName} - {@AmountToWidthraw} {@Network}", gameName, value.AmountSent, value.Network)
                            | Fail(msg) -> Logger.Error("SendActor - Failed to profit for {@GameName} - {@Msg}.", gameName, msg)
            with exn -> 
                Logger.Error(exn, "Failed to profit {@AmountToProfit}", profit)

    let run system network (service : SendActorTypes.SendActorService) =
        service.Storage.Init()
        spawn system "send"
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()
                    match message with
                    | SendCommand.Widthraw(amount, bet) -> 
                        Widthraw 
                        <| service 
                        <| amount 
                        <| bet
                    | SendCommand.Refund(bet, failure) -> 
                        Refund 
                        <| service
                        <| bet 
                        <| failure
                    | SendCommand.Profit(gameName, playedAt, profitNQT) ->
                        Profit
                        <| service 
                        <| gameName 
                        <| playedAt 
                        <| profitNQT
                    return! loop()
                }
                loop())