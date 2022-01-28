namespace GameClient.Rates

open Microsoft.AspNet.SignalR.Hubs
open Microsoft.AspNet.SignalR
open Akka.Actor
open Akka.FSharp
open Shared
    
(* RATE HUB *)
type IRateChartHub =
    abstract BroadcastData : UpdateRate -> unit

[<HubName("RateChartHub")>]
type RateChartHub() =
    inherit Hub<IRateChartHub>()
 
module RateProxyActor =
    let run system  =
        let _hub = GlobalHost.ConnectionManager.GetHubContext<RateChartHub, IRateChartHub>();
        spawn system "rateProxy"
            (fun mailbox ->
                let rec loop() = actor {
                    let! msg = mailbox.Receive()
                    match (msg :obj) with
                    | :? RateTypes.Rate as rate -> 
                          let updateDTO = UpdateRate.Create rate.Date rate.Day7
                          _hub.Clients.All.BroadcastData(updateDTO)
                    | _ -> mailbox.Unhandled()
                    return! loop()
                }
                loop())
