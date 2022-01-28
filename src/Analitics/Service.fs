namespace Analitics 

open System
open System.IO
open System.Windows.Forms
open FSharp.Charting
open FSharp.Charting.ChartTypes
open FSharp.Data

module Service = 
    open System.Globalization
    
    type Rates = CsvProvider<"bxnprd_Rates.csv">
    type Rate = { Date : DateTime; Day7 : decimal }

    module Validate =

        let rec CheckForDuplicates (prevDate : DateTime) (input : CsvProvider<"bxnprd_Rates.csv">.Row list) =
            match input with
            | head :: tail -> 
                let refDate  = DateTime.ParseExact( head.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture )
                let date = new DateTime(refDate.Year, refDate.Month, refDate.Day, refDate.Hour, refDate.Minute, 0, 0)

                if prevDate.Hour = date.Hour && prevDate.Minute = date.Minute then
                    printf "\nduplicated data found\n"
                    printf "%A" head

                CheckForDuplicates date tail 
            | [] -> ()

        let rec CheckForGaps (prevDate : DateTime) (input : CsvProvider<"bxnprd_Rates.csv">.Row list) =
            match input with
            | head :: tail -> 
                let refDate  = DateTime.ParseExact( head.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture )
                let date = new DateTime(refDate.Year, refDate.Month, refDate.Day, refDate.Hour, refDate.Minute, 0, 0)
                if Math.Abs( (prevDate - date).TotalMinutes ) <> 30. then
                    printf "\ndata is missing\n"
                    printf "%A" head
                CheckForGaps date tail
            | [] -> ()

    module Calculate =

        let rec MapToRateDiff (input : CsvProvider<"bxnprd_Rates.csv">.Row list) (output : Rate list) =
            match input with
            | head :: tail when tail.Length > 0 -> 
                let refDate  = DateTime.ParseExact( tail.Head.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture )
                let date = new DateTime(refDate.Year, refDate.Month, refDate.Day, refDate.Hour, refDate.Minute, 0, 0)
                let diff = tail.Head.Day7 - head.Day7 
                let rate = { Date = date; Day7 = diff }
                MapToRateDiff tail [rate]@output
            | head :: _ -> output
            | [] -> output

        let rec RemoveNonSelectableRates (input : Rate list) =
            let data = input |> List.filter (fun i -> i.Date.Minute <> 0)
            data

        let rec RemoveExtremums (input : Rate list) min max =
           let data = input |> List.filter (fun r -> r.Day7 >= min) |> List.filter (fun r -> r.Day7 <= max)
           data

        let Group (input : Rate list) =
            input |> List.sortBy (fun o -> o.Day7) |> List.groupBy(fun i -> i.Day7) |> List.map (fun (k, v) -> (k, v.Length) )
            
        let Propability (input : (decimal * int) list) =
            let total = float input.Length
            let data = input |> List.map (fun i -> (fst i, float(snd i) / total ))
            data

    let Run() =
        let rates = Rates.Load("bxnprd_Rates.csv").Rows |> Seq.toList |> List.rev
        
        let date = DateTime.ParseExact( rates.Head.Date, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture )
        let prevDate = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, 0)
        
        Validate.CheckForDuplicates
        <| prevDate
        <| rates.Tail

        Validate.CheckForGaps 
        <| prevDate
        <| rates.Tail

        let diff =
            Calculate.MapToRateDiff
            <| rates
            <| []

        let data =  
            Calculate.RemoveExtremums diff -1m 1m
            |> Calculate.RemoveNonSelectableRates
            |> Calculate.Group
            |> Calculate.Propability

        for d in data do
            printf "%A\n" d


        // calculate the probabiliy of win if I bet from -1 do 1
        // with diff margin: how many bets must I place to win?

        let prize = 1m
        let houseEdge = 0.1m
        let cost = 0.01m // cost of 1 bet
        let bets = 100m

        // xbets * cost * edge*100 = prize
        
        // 1000 * 0.01 -> 10 = 
        // 10 * 0.1 == 1BTC prize
        // 1000 failed bets go accumulate one prize

        // 500 * 0.01 -> 5 -> 0.5 btc prize
        // 250 * 0.01 -> 2.5 -> 0.25 btc prize

        // how many bets should I place to win? (with probability and win margins)

        // lets assume that 100 gives mi chance to win > 50%
        // 100 * 0.01 -> 10btc to win -- failed
        // 25   * 0.01 -> 0.25 to win 25btc.  ... more fair for the user

        // users are laizy: 25 bets should be max

        // so 25-50 bets to win the prize
        // i need 100 - 200 users per game

        // todo: record user bets, for analitics in the future

        // 100 users * avg 10 bets -> 1000 * 0.01 -> 10btc -> 0.5btc prize ; 0.5btc profit
        // 50 users * avg  20 bets

        // 10 * avg 10b -> 100* 0.01 -> 1btc -> prize = 0.1btc; profit = 0 -----> action needed!! use bank roll to fillup the pize to 0.5btc

        // good to have 1000bets daily


        // MY GOAL: 10 bets daily
        //          100 bets daily
        //          1000 bets daily

        // solution: run game on test net, set params to that you thing will be ok, play for 1 month with other ppl
        // prize: 0.5
        // bet: 0.01
        // diffmargin: 0.01 (prop x 3)
        // --- play for 1 week

        // run analitics agains btc
        // todo: add bet history records (1hour)
        // place bet in nice place
        // faq - do in in the evening or on monday at work
        // setup allrequired things (new account for everything)
        // and play for 1 week!

        // if faq will be ready, announce at reddit, bitcointalk, nxtforum, twitter












        



        let losingBetsToGetNewPrize = bets 

 
        let form = new Form(Width=400, Height=300, Visible=true, Text="Hello charting")
        //let chart = FSharp.Charting.Chart.Line([for d in data -> d.Date, d.Day7]).ShowChart()

        let chart2 = FSharp.Charting.Chart.Line([for d in data -> fst d, snd d ]).ShowChart()

        //let chart = FSharp.Charting.Chart.Line([for x in 0 ..10 -> x, x + x]).ShowChart()
        System.Windows.Forms.Application.Run(form)

        ()
 