using System;
using System.Web;
using Serilog;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Akka.Actor;
using GameClient.Balances;
using GameClient.Games;
using GameClient.Rates;
using Shared;
using GameClient.Connectivity;
using GameClient.Payments;


namespace Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static ActorSystem WebSystem;
        public static IActorRef ClientActor;
        public static IActorRef ConnectionProxy;
        public static IActorRef GameProxy;
        public static IActorRef BalanceProxy;
        public static ActorSelection GameView;
        public static ActorSelection ShortStatView;
        public static ActorSelection RateActor;
        public static IActorRef RateProxy;
        public static ActorSelection ServerActor;
        public static ActorSelection BalanceActor;
        public static ActorSelection GameActor;

        public Shared.Settings.SettingsProvider Settings => Shared.Settings.Get();

        public void SetupLogging()
        {
            StackifyLib.Logger.GlobalApiKey = Settings.Stackify.GlobalApiKey;
            StackifyLib.Logger.GlobalAppName = Settings.Stackify.GlobalAppName;
            StackifyLib.Logger.GlobalEnvironment = Settings.Stackify.GlobalEnvironment;

            var loggerCfg = new LoggerConfiguration();
            Log.Logger = loggerCfg.MinimumLevel.Verbose()
                .WriteTo.RollingFile("logs.txt")
                .WriteTo.Stackify().CreateLogger();
        }

        protected void Application_Start()
        {
            try
            {
                SetupLogging();

                AreaRegistration.RegisterAllAreas();
                GlobalConfiguration.Configure(WebApiConfig.Register);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);

                WithdrawStorage.Init();

                WebSystem = ActorSystem.Create(Settings.Systems.Web);

                ServerActor = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/server");
                GameView = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/readonly-game");
                GameActor = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/game");
                RateActor = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/rate");
                ShortStatView = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/short-stats");
                BalanceActor = WebSystem.ActorSelection($"akka.tcp://{Settings.Systems.Server}@{Settings.Systems.ServerIp}:{Settings.Systems.ServerPort}/user/balance");

                ConnectionProxy = ClientProxy.run(WebSystem);
                GameProxy = GameProxyActor.run(WebSystem);
                RateProxy = RateProxyActor.run(WebSystem);

                BalanceProxy = BalanceProxyActor.run(WebSystem,
                    Settings.Enviroment.Configuration.IsDEBUG ||
                    Settings.Enviroment.Configuration.IsSTAGING ? 
                    Model.Network.BTCTEST : Model.Network.BTC);

                var gameProxyRemoteAddress = $"akka.tcp://{Settings.Systems.Web}@{Settings.Systems.WebIp}:{Settings.Systems.WebPort}/user/gameProxy";
                var rateProxyRemoteAdddress = $"akka.tcp://{Settings.Systems.Web}@{Settings.Systems.WebIp}:{Settings.Systems.WebPort}/user/rateProxy";
                var balanceProxyRemoteAdddress = $"akka.tcp://{Settings.Systems.Web}@{Settings.Systems.WebIp}:{Settings.Systems.WebPort}/user/balanceProxy";

                ClientActor = GameClient.Connectivity.ClientActor.run(WebSystem, ServerActor, ConnectionProxy,
                    gameProxyRemoteAddress, balanceProxyRemoteAdddress, rateProxyRemoteAdddress);
                System.Threading.Thread.Sleep(1000);

                ClientActor.Tell(new Commands.ClientCommand.ClientStartUp(), ClientActor);

                Log.Logger.Debug("WebApp started.");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Failed to start WebApp");
            }
        }

        public static bool IsConnected()
        {
            return ConnectionProxy.Ask<Commands.ConnectionState>(Commands.ClientState.Get, TimeSpan.FromSeconds(3)).Result.IsConnected;
        }

        protected void Application_End()
        {
            ClientActor.Tell(new Commands.ClientCommand.ClientShutdown(), ClientActor);
            System.Threading.Thread.Sleep(1000);
            Log.Information("WebApp has been stopped.");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError().GetBaseException();

            var httpException = exception as HttpException;
            if (httpException != null)
                return;
 
            Log.Logger.Fatal(exception, "Request: {@Request},\nMessage: {@Message}", Request.Url, exception.Message);
        }
    }
}
