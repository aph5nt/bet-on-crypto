namespace GameEngine.Balances

open Akka.FSharp
open WebSocketSharp
open Shared

module BalanceActor =
    open BlockApi.Types
    open BalanceTypes.BalanceCommands
    open System.Net.Sockets

    let getBalance() =
        let balances = BlockApi.BlockApi.Api().GetMyAddresses(Settings.PaymentGateway.ApiKey)
        match balances with
        | Success(result) ->
            result.Data
            |> Array.map(fun(i) -> (i.Address, i.AvailableBalance))
            |> Map.ofArray
        | Fail(msg) -> 
            Logger.Error(msg)
            Map.empty<string, decimal>
 
    let run system (network : Network) =

        let balance = getBalance()
        let authCmd = sprintf "{  \"type\": \"account\", \"api_key\": \"%s\" }" <| Settings.PaymentGateway.ApiKey

        let actorRef = 
            spawn system "balance"
                (fun mailbox ->
                    let listener = new WebSocket (Settings.PaymentGateway.NotificationUrl)
                    listener.EmitOnPing <- true
                    listener.OnClose.Add (fun e -> Logger.Debug("BalanceActor - websocket {@Event}", e)) 
                    listener.OnOpen.Add (fun e ->  Logger.Debug("BalanceActor - websocket {@Event}", e)) 
                    listener.OnError.Add (fun e -> 
                        match e.Exception with
                        | :? WebSocketException 
                        | :? SocketException -> mailbox.Context.System.Scheduler.ScheduleTellOnce(System.TimeSpan.FromMinutes(1.), mailbox.Self, new Reconnect())
                        | _ -> ()
                        Logger.Warning("BalanceActor - websocket {@Event}", e)) 
                    listener.OnMessage.Add (fun e -> 
                        Logger.Debug(e.Data)
                        let response = BalanceTypes.AddressType.Parse e.Data
                        match response.Type with
                        | "address" ->
                            if (response.Data.Network = (network |> Network.toString) && response.Data.Confirmations = 0) then
                                mailbox.Context.Self <! { UpdateBalance.Address = response.Data.Address; BalanceChanged = response.Data.BalanceChange }
                        | _ -> ())
                    listener.Connect();
                    listener.Send(authCmd)

                    let rec loop (balance : Map<Address, decimal>) = actor {
                        let! message = mailbox.Receive()
                        let sender = mailbox.Context.Sender
                        match box message with
                        | :? PublishBalances -> 
                                let balance = getBalance()
                                balance |> Map.iter(fun a b -> mailbox.Context.System.EventStream.Publish({ OnBalanceUpdated.Address = a; OnBalanceUpdated.ActualBalance = b}))
                                return! loop balance
                        | :? Reconnect -> 
                                listener.Connect()
                                listener.Send(authCmd)
                                mailbox.Self <! new PublishBalances()
                        | :? GetBalance as cmd -> 
                            if balance.ContainsKey(cmd.Address) then
                                sender <! balance.[cmd.Address]
                            else 
                                sender <! 0.0m
                                Logger.Warning("Missed balance retrival for {@Address}", cmd.Address)
                        | :? AddBalance as cmd -> 
                            listener.Send(authCmd)
                            return! loop <| balance.Add(cmd.Address, 0.0m)
                        | :? UpdateBalance as cmd ->
                             let actualBalance = balance.[cmd.Address] + cmd.BalanceChanged
                             mailbox.Context.System.EventStream.Publish({ OnBalanceUpdated.Address = cmd.Address; OnBalanceUpdated.ActualBalance = actualBalance})
                             return! loop <|(balance |> Map.remove(cmd.Address) |> Map.add cmd.Address actualBalance )
                        | _ -> ()
                        return! loop balance
                    }
                    loop balance)
        actorRef