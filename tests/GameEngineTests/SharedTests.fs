namespace GameEngineTests

open Xunit
open FsUnit.Xunit
open Foq
open System
open Akka.Actor
open DigitallyCreated.FSharp.Azure.TableStorage
open Shared
open Microsoft.WindowsAzure.Storage
open GameEngine.Payments

module SharedTests =
 
    [<Fact>]
    let ``Should log httpstatus code``() = 
        Storage.log "200 op" { OperationResult.HttpStatusCode = 200; Etag = "tag" }
        Storage.log "299 op" { OperationResult.HttpStatusCode = 299; Etag = "tag" }
        Storage.log "300 op" { OperationResult.HttpStatusCode = 300; Etag = "tag" }
        Storage.log "400 op" { OperationResult.HttpStatusCode = 400; Etag = "tag" }
        Storage.log "499 op" { OperationResult.HttpStatusCode = 499; Etag = "tag" }
        Storage.log "500 op" { OperationResult.HttpStatusCode = 500; Etag = "tag" }
        Storage.log "599 op" { OperationResult.HttpStatusCode = 599; Etag = "tag" }
        Storage.log "199 op" { OperationResult.HttpStatusCode = 199; Etag = "tag" }
        Storage.log "600 op" { OperationResult.HttpStatusCode = 600; Etag = "tag" }