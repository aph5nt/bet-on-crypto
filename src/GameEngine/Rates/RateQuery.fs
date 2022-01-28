namespace GameEngine.Rates

open System
open FSharp.Data
 
module Query =
    open Shared
 
    let Get : Get =
        fun network ->
            try
                let markup = Coinmarketcap.Load("http://coinmarketcap.com/currencies/views/all/")
                let row = markup.Tables.``All Currencies``.Rows |> Seq.find(fun(row) -> row.Symbol = Network.toString ( Model.mapNetwork network ) )
                let date = SystemTime.UtcNow()
                let day7 = row.``% 7d``.Replace("%","").Trim() |> Helpers.AsDecimal
                let rate = { Date = date; Day7 = day7; Network = Model.mapNetwork network }
                Some(rate)
            with _ -> 
                Logger.Warning(FailureMessages.[FailureMessage.RateQueryError])
                None

    let RateQueryService() : RateQueryService = 
        { Get = Get }

