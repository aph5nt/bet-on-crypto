namespace GameClient.Connectivity
open Akka.FSharp
open Shared
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs


type IConnectivityProxyHub =
    abstract State : ConnectionState -> unit

[<HubName("ConnectivityProxyHub")>]
type ConnectivityProxyHub() =
    inherit Hub<IConnectivityProxyHub>()

module ClientProxy =

    let run system = 
        let _hub = GlobalHost.ConnectionManager.GetHubContext<ConnectivityProxyHub, IConnectivityProxyHub>();
        spawn system "connectivityProxy"
            (fun mailbox ->
                let rec loop state = actor { 
                    let! msg = mailbox.Receive()
                    match msg with
                    | Set(state) ->
                        _hub.Clients.All.State(state)
                        return! loop state
                    | Get -> mailbox.Context.Sender <! state
                             return! loop state
                    
                }
                loop { IsConnected = false})