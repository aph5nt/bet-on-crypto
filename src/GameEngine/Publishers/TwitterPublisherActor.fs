namespace GameEngine.Publishers

open Akka.FSharp
open Tweetinvi
 
module TwitterPublisherActor = 
    open Shared
    open GameEngine.Games
    open Tweetinvi.Core.Events.EventArguments
 
    let hashTags = Settings.PublisherTwitter.HashTags

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
        | true  -> Logger.Information("TwitterPublisher - Published: {@Args}", args)
        | false -> Logger.Error("TwitterPublisher - Failed to Publish: {@Args}", args)
  
    let publish (message : string) =
        Auth.SetUserCredentials(Settings.PublisherTwitter.ConsumerKey, Settings.PublisherTwitter.ConsumerSecret, Settings.PublisherTwitter.AccessToken, Settings.PublisherTwitter.AccessTokenSecret) |> ignore
        Tweet.PublishTweet(message) |> ignore

    let buildScoreMessage network (data : GameDrawedData) = 
        let total = data.Win * (decimal data.Winners.Length)
        let perBet = data.Win
        if data.Winners.Length = 1 then
            sprintf "We got 1 winner for game %s! Total won: %f %s. %s" <| data.Name <| total <| Network.toString network <| hashTags
        else
            sprintf "We got %d winners for game %s! Total won: %f %s. Win per bet: %f %s %s" <| data.Winners.Length <| data.Name <| total <| Network.toString network <| perBet <| Network.toString network <| hashTags
    
    let buildCongratulationMessage network (data : GameDrawedData) twitterAccount =
        let win = data.Win
        sprintf "@%s You have won %f %s in game %s! %s " <| twitterAccount <| win <| Network.toString network <| data.Name <| hashTags
 
    let run system network =
        TweetinviEvents.QueryBeforeExecute.Add beforeExecuteHandler
        TweetinviEvents.QueryAfterExecute.Add afterExecuteHandler

        spawn system "twitter-publisher"
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()
                    match box message with
                    | :? GameUIEvents.OnGameDrawed as cmd ->
                        Logger.Debug("TwitterPublisher - Received @{Data}", cmd.GameDrawedData)
                        match cmd.GameDrawedData.Winners.Length with
                        | x when x > 0 -> 
                            publish <| buildScoreMessage network cmd.GameDrawedData
                            for id in TwitterPublisherStorage.GetTwitterAccounts() do
                                Async.Sleep(1000) |> Async.RunSynchronously
                                let name = Tweetinvi.User.GetUserFromId(id).ScreenName
                                publish <| buildCongratulationMessage network cmd.GameDrawedData name
                        | _ -> 
                            ()
                    | _ -> 
                        ()
                    return! loop()
                }

                loop())