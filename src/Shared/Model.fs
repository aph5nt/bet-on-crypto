namespace Shared

open System.Globalization
open System

[<AutoOpen>]
module Model = 
    open Microsoft.FSharp.Reflection
    
    let nextHourDelta() = 
        let timeOfDay = DateTime.Now.TimeOfDay;
        let nextFullHour = TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));
        let delta = (nextFullHour - timeOfDay).TotalSeconds;
        delta
 
    (* MODEL *)
    
    type Network = BTCTEST | BTC | LTCTEST | LTC | DOGETEST | DOGE
        with static member toString (status : 'a)= 
                match FSharpValue.GetUnionFields(status, typeof<'a>) with
                case, _ -> case.Name

    let mapNetwork network = 
            match network with
            | Network.BTCTEST -> BTC
            | Network.DOGETEST -> DOGE
            | Network.LTCTEST -> LTC
            | _ -> network

    let roundAmount (amount : decimal) = Math.Round(amount, 8)

    type UserName = string
    type Vote = decimal
    type Address = string
    type VoteFor = decimal

    type GameName = string
    type Reason = string
    type TransactionId = string
    type Amount  = decimal
    type PlayedAt = System.DateTime
    and GameStatus = Pending | Opened | Closed | Drawed | Refunded
        with static member toString (status : 'a)= 
            match FSharpValue.GetUnionFields(status, typeof<'a>) with
            case, _ -> case.Name
    type ProfitAddress = string
    type WidthrawAddress = string
    type RefundAddress = string

    [<CLIMutable>]
    type Game = { // this will be for game archive; the view of current game will be a list of bets an prize.
        Network : Network
        Name: GameName;
        OpenedAt: DateTime;
        DrawingAt: DateTime;
        CompletedAt: DateTime;
        Status: GameStatus;
        WinningVote: Option<decimal>;
        Bets: list<Bet>;
        Winners: list<Bet>;
        Win: decimal;
        Profit: decimal; } with
        static member GetName (date : DateTime) =
            let date = date.ToString("yyyy-MM-dd HH:00")
            sprintf "%s" date
        static member Create network name (date : DateTime) = 
            let openedAt = new DateTime(date.Year, date.Month, date.Day, date.Hour,0,0,0, DateTimeKind.Utc)
            {   Network = network
                Name = name date;
                Status = Pending;
                Bets = list.Empty;
                WinningVote = None;
                Winners = list.Empty;
                Win = 0m;
                Profit = 0m;
                OpenedAt = openedAt
                CompletedAt = openedAt.AddHours(1.0)
                DrawingAt = openedAt.AddHours(2.0)
            }
    and [<CLIMutable>] 
        GameState = {
            Games : Map<string, Game>
            Accumulation: decimal
            Network : Network
        } 
    and [<CLIMutable>]
        Bet = {
        TransactionId: TransactionId
        UserName: string
        GameName: GameName
        Address: string
        Amount: Amount
        VoteFor: Vote
        PlacedAt: DateTime }