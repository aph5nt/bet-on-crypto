namespace GameEngine.Connectivity

open Akka.FSharp
open Akka.Actor
open Shared
 
module ServerActor =

   open ServerCommand
   open System
   open Shared.BalanceTypes.BalanceCommands

   let run (system : ActorSystem) (network : Network) =
        spawn system "server"
            (fun mailbox ->
                let mutable clientActorAddress : string = ""
                let rec loop = actor {
                    let! message = mailbox.Receive()
                    match box message with
                    | :? OnClientConnected as cmd ->
                        try
                            let gameProxyRef = system.ActorSelection(cmd.GameProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result
                            let rateProxyref = system.ActorSelection(cmd.RateProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result
                            let balanceProxyRef = system.ActorSelection(cmd.BalanceProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result

                            clientActorAddress <- sprintf "%suser/client" <| gameProxyRef.Path.Root.ToStringWithAddress()

                            system.EventStream.Subscribe(gameProxyRef, typeof<GameUIEvents.OnBetPlaced>)  |> ignore
                            system.EventStream.Subscribe(gameProxyRef, typeof<GameUIEvents.OnGameDrawed>) |> ignore
                            system.EventStream.Subscribe(gameProxyRef, typeof<GameUIEvents.OnShortStat>)  |> ignore
                            system.EventStream.Subscribe(gameProxyRef, typeof<GameUIEvents.Reload>)       |> ignore
                            system.EventStream.Subscribe(rateProxyref, typeof<Rate>)                      |> ignore
                            system.EventStream.Subscribe(balanceProxyRef, typeof<OnBalanceUpdated>)       |> ignore
                            
                            mailbox.Sender() <! { ConnectionState.IsConnected = true  }
                        with exn ->
                            Logger.Error("OnClientConnected error {@Exn}", exn)
                            mailbox.Sender() <! { ConnectionState.IsConnected = false }
                        return! loop

                    | :? OnClientDisconnect as cmd ->
                        try
                            let gameProxyRef = system.ActorSelection(cmd.GameProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result
                            let rateProxyref = system.ActorSelection(cmd.RateProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result
                            let balanceProxyRef = system.ActorSelection(cmd.BalanceProxyAddress).ResolveOne(TimeSpan.FromSeconds(10.)).Result

                            system.EventStream.Unsubscribe(gameProxyRef)    |> ignore
                            system.EventStream.Unsubscribe(gameProxyRef)    |> ignore
                            system.EventStream.Unsubscribe(gameProxyRef)    |> ignore
                            system.EventStream.Unsubscribe(gameProxyRef)    |> ignore
                            system.EventStream.Unsubscribe(rateProxyref)    |> ignore
                            system.EventStream.Unsubscribe(balanceProxyRef) |> ignore
                            
                            mailbox.Sender() <! { ConnectionState.IsConnected = true }
                        with _ ->
                            mailbox.Sender() <! { ConnectionState.IsConnected = false }
                        return! loop

                    | :? ServerShutdown -> 
                         system.ActorSelection(clientActorAddress).Tell(new Commands.ClientCommand.OnServerDisconnected())
                         return! loop
                    | message -> 
                        mailbox.Unhandled message
                        return! loop
                }
                loop)

 
       