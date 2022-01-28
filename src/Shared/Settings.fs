namespace Shared

open System.Configuration
open System

[<AutoOpen>]
module Settings =
 
    type GameSetting = {
        Radius: float
        Bet: decimal
        IncomingAddress : string
        ProfitAddress : string
    }
    and PaymentGatewaySetting = {
        ApiUrl : string
        NotificationUrl : string
        ApiKey : string
        Pin : string
    }
    and Stackify = {
        GlobalApiKey : string;
        GlobalAppName : string;
        GlobalEnvironment : string;
    }
    and SystemsSettings = {
        Web : string;
        WebIp : string
        WebPort : int
        Server: string;
        ServerIp : string
        ServerPort : int
    }
    and SendGrid = {
        Key : string
    }
    and PublisherTwitter = {
        ConsumerKey : string; 
        ConsumerSecret : string;
        AccessToken :string;
        AccessTokenSecret : string
        HashTags : string
    }
    and AuthTwitter = {
        ConsumerKey : string; 
        ConsumerSecret : string;
    }
    and EnviromentConfiguration = DEBUG | STAGING | RELEASE
    and Enviroment = {
        Configuration : EnviromentConfiguration
    }
 
    type SettingsProvider() =
        let appSettings = System.Configuration.ConfigurationManager.AppSettings
        let  getEnviromentConfiguration (str : string) =
            match str.ToLower() with
            | "debug" -> DEBUG
            | "staging" -> STAGING
            | "release" -> RELEASE
            | _ -> DEBUG

        member x.Game = 
            {
                Radius = (float)   appSettings.["Game.Radius"]
                Bet = (decimal) appSettings.["Game.Bet"]
                IncomingAddress = appSettings.["Game.IncomingAddress"]
                ProfitAddress = appSettings.["Game.ProfitAddress"]
            }
        member x.PaymentGateway = 
            {
                ApiUrl = appSettings.["PaymentGateway.ApiUrl"]
                NotificationUrl = appSettings.["PaymentGateway.NotificationUrl"]
                ApiKey = appSettings.["PaymentGateway.ApiKey"]
                Pin = appSettings.["PaymentGateway.Pin"]
            }
        member x.Systems = 
            {
                  Web = "WebSystem"
                  WebIp = appSettings.["Systems.WebIp"]
                  WebPort = int appSettings.["Systems.WebPort"]
                  Server = "ServerSystem"
                  ServerIp = appSettings.["Systems.ServerIp"]
                  ServerPort = int appSettings.["Systems.ServerPort"]
            }
        member x.Storage =  ConfigurationManager.ConnectionStrings.["default"].ConnectionString
        member x.Stackify = 
            {
                GlobalApiKey =  appSettings.["Stackify.GlobalApiKey"];
                GlobalAppName =  appSettings.["Stackify.GlobalAppName"];
                GlobalEnvironment =  appSettings.["Stackify.GlobalEnvironment"];
            }
        member x.SendGrid = 
            {
                Key = appSettings.["SendGrid.Key"];
            }
        member x.PublisherTwitter = 
            {
                ConsumerKey = appSettings.["Publisher.Twitter.ConsumerKey"];
                ConsumerSecret = appSettings.["Publisher.Twitter.ConsumerSecret"];
                AccessToken = appSettings.["Publisher.Twitter.AccessToken"];
                AccessTokenSecret  = appSettings.["Publisher.Twitter.AccessTokenSecret"];
                HashTags = appSettings.["Publisher.Twitter.HashTags"];
               
            }
        member x.AuthTwitter = 
            {
                ConsumerKey = appSettings.["Auth.Twitter.ConsumerKey"];
                ConsumerSecret = appSettings.["Auth.Twitter.ConsumerSecret"];
            }
        member x.Enviroment = 
            {
                Configuration =  getEnviromentConfiguration appSettings.["Enviroment.Configuration"];
            }

    let Get() = 
        let cfg = new SettingsProvider()
        cfg 
    let Settings = Get()