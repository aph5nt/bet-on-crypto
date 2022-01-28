using System;
using GameClient.Balances;
using GameClient.Connectivity;
using GameClient.Games;
using GameClient.Rates;
using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Web;
using Web.Infrastructure;

[assembly: OwinStartup(typeof(Startup))]

namespace Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AppDomain.CurrentDomain.Load(typeof(GameHub).Assembly.FullName);

            GlobalHost.DependencyResolver.Register(typeof(GameHub), () => new GameHub());
            GlobalHost.DependencyResolver.Register(typeof(ConnectivityProxyHub), () => new ConnectivityProxyHub());
            GlobalHost.DependencyResolver.Register(typeof(RateChartHub), () => new RateChartHub());
            GlobalHost.DependencyResolver.Register(typeof(BalanceHub), () => new BalanceHub());

            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(10);

            var settings = new JsonSerializerSettings { ContractResolver = new SignalRContractResolver() };
            var serializer = JsonSerializer.Create(settings);
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);

            app.MapSignalR("/rapi", new HubConfiguration
            {
                EnableJavaScriptProxies = true,
                EnableDetailedErrors = true,
                EnableJSONP = true
            });

            ConfigureAuth(app);
        }
    }
}