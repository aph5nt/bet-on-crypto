namespace Shared

open System
open System.Globalization
open Microsoft.FSharp.Reflection
open Chessie.ErrorHandling
 
/// Module responsible for handling logging
[<AutoOpen>]
module Logging =
    open Shared
 
    let Logger = Serilog.Log.Logger
        
    /// Logs the messages on success or failure Tee
    let logResult (result : Result<'a, FailureMessage * string>) = 
         let logger = Serilog.Log.Logger
         result 
            |> successTee (fun (_, msgs) -> msgs |> List.iter (fun (message) -> logger.Information("{@Message}", message)))
            |> ignore
         result 
            |> failureTee (fun (msgs) -> msgs |> List.rev |> List.iter (fun (_, message) -> logger.Warning("{@Error}", message)))  
            |> ignore
         result

[<AutoOpen>]
module SystemTime = 
    
    let mutable UtcNow = fun() -> DateTime.UtcNow
    let SetDateTime (date : DateTime) = UtcNow <- fun() -> date
    let ResetDateTime() = UtcNow <- fun() -> DateTime.UtcNow

[<AutoOpen>]
module Helpers = 
 
    let toString (x:'a) = 
            match FSharpValue.GetUnionFields(x, typeof<'a>) with
                case, _ -> case.Name

    let AsDecimal (str :string) =
        try
            Decimal.Parse(str.Replace(',','.'), NumberStyles.Any, CultureInfo.InvariantCulture)
        with 
        | exn -> 0m