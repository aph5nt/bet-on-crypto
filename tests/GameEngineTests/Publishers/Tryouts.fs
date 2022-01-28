namespace GameEngineTests.Publishers

open Xunit
open FsUnit.Xunit
open System
open GameEngine.Rates
open Shared
open Tweetinvi
 
module Tryouts = 
    open Tweetinvi.Core.Events.EventArguments
 
    [<Fact>]
    let ``Publish tryout``() = 
        GameEngineTests.Logging.Init()
 
        let consumerKey = "tZ1b2cN4RPxyZTGqr0aejGkgS"
        let consumerSecret = "10ITG3gJUCnfDDBw3xssZ5wreoSTMeGTYBdHYIjqagY6u526Qo"
        let accessToken = "708643865402720257-KwMS860AlChrbEwBV7roAQFFejlZBhc" 
        let accessTokenSecret = "XAVstgmEZ9NzszIqXPiN6kBH2OP3Pyia3Ex6IyU8DNugN"

        Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret) |> ignore

        let beforeExecuteHandler ( args : QueryBeforeExecuteEventArgs ) =
            let queryRateLimits = RateLimit.GetQueryRateLimit(args.QueryURL)
            match queryRateLimits with
            | null -> ()
            | x when x.Remaining > 0 -> ()
            | x when x.Remaining = 0 -> 
                 Logger.Information("TwitterPublisher - Waiting for RateLimits until : {@QueryRateLimits}", queryRateLimits);
                 Async.Sleep((int queryRateLimits.ResetDateTimeInMilliseconds)) |> Async.RunSynchronously
            | _ -> args.Cancel <- true
        
        let afterExecuteHandler ( args : QueryAfterExecuteEventArgs ) = 
            match args.Success with
            | true  -> Logger.Information("TwitterPublisher - Published: {@CompletedDateTime}", args.CompletedDateTime)
            | false -> Logger.Error("TwitterPublisher - Failed to Publish: {@CompletedDateTime}", args.CompletedDateTime)
  
        TweetinviEvents.QueryBeforeExecute.Add beforeExecuteHandler
        TweetinviEvents.QueryAfterExecute.Add afterExecuteHandler
        let gameName = DateTime.UtcNow
        Tweet.PublishTweet(sprintf "We got 1 winner for game %A! Total won: 100.0000 BTCTEST. #btctest #crypto" gameName) |> ignore
