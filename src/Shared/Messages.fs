namespace Shared
open System

[<AutoOpen>]
module Messages = 
    open Shared

    /// Describes the failure message.
    type FailureMessage =
        | TransactionTooOldError
        | GameRefundedError
        | GameNotDrawnOnTime
        | BetMissedRefundedError
        | RateQueryError
        | SendError
        | RetriveError
 
    /// Returns the failure messages.
    let FailureMessages : Map<FailureMessage, string> =
        Map.empty
            .Add(GameRefundedError, "Due to the internal error, your bet has been refunded.")
            .Add(GameNotDrawnOnTime, "Game was not drawn on time, your bet has been refunded.")
            .Add(BetMissedRefundedError, "Due to the internal error, your bet has been refunded.")
            .Add(RateQueryError, "Rate query failed.")
            .Add(SendError, "Sending founds failed.")
            .Add(RetriveError, "Retriving transactions failed.")