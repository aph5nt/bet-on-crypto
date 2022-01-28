namespace GameClient.Balances

open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open Akka.Actor
open Akka.FSharp
open Shared
open System
open Shared.BalanceTypes
open Shared.BalanceTypes.BalanceCommands

(* GAME HUB *)
type IBalanceHub =
    abstract OnBalanceUpdated : Balance -> unit
    abstract OnRegistered : Address -> unit

[<HubName("BalanceHub")>]
type BalanceHub() =
    inherit Hub<IBalanceHub>()
    override x.OnConnected() =
        x.Groups.Add(x.Context.ConnectionId, x.Context.Request.QueryString.["address"]) |> ignore
        base.OnConnected()

(* === ACTORS === *)
module BalanceProxyActor =
    open Shared
 
    let run system network =
        let _hub = GlobalHost.ConnectionManager.GetHubContext<BalanceHub, IBalanceHub>();
        spawn system "balanceProxy"
            (fun mailbox ->
                let rec loop() = actor {
                    let! msg = mailbox.Receive()
                    match box msg with
                    | :? OnBalanceUpdated as dto -> 
                        _hub.Clients.Group(dto.Address).OnBalanceUpdated(dto.ActualBalance)
                    return! loop()
                }
                loop())


