namespace GameClient.Connectivity
open Akka.FSharp
open Shared
 
module ClientActor =
    open Akka.Actor
    open System
    open ServerCommand
    open Shared.Commands.ClientCommand

    let run (system : ActorSystem) (server : ActorSelection) (connectionProxy : IActorRef) (gameProxyAddress : string) (balanceProxyAddress : string) (rateProxyAddress : string) =
        spawn system "client"
            (fun mailbox ->
                let rec loop (state : bool) = actor {
                    let! message = mailbox.Receive()
                    match box message with
                    | :? Terminated as terminated ->
                            ()
                    | :? ClientStartUp -> 
                       try
                            let serverActorRef = server.ResolveOne(TimeSpan.FromSeconds(5.0)).Result
                            mailbox.Context.Watch(serverActorRef) |> ignore

                            let state = serverActorRef.Ask<ConnectionState>({ OnClientConnected.GameProxyAddress = gameProxyAddress; RateProxyAddress = rateProxyAddress; BalanceProxyAddress = balanceProxyAddress }, TimeSpan.FromSeconds(10.)) |> Async.RunSynchronously
                            connectionProxy <! Set(state)
                            return! loop state.IsConnected
                       with exn ->
                            mailbox.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(10.), mailbox.Self, new ClientStartUp())
                            connectionProxy <! Set({ IsConnected = false })
                            return! loop false
                    | :? ClientShutdown ->
                         try
                             let serverActorRef = server.ResolveOne(TimeSpan.FromSeconds(5.0)).Result
                             let onClientDisconnect = { OnClientDisconnect.GameProxyAddress = gameProxyAddress; RateProxyAddress = rateProxyAddress; BalanceProxyAddress = balanceProxyAddress }
                             serverActorRef.Tell(onClientDisconnect)
                             return! loop false
                         with exn ->
                            return! loop false

                    | :? OnServerDisconnected -> 
                        connectionProxy <! Set({ IsConnected = false })
                        mailbox.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(10.), mailbox.Self, new ClientStartUp())
                        return! loop false
                    | _ -> mailbox.Unhandled message
                           return! loop state
                }
                loop false )

