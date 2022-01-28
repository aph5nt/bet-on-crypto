using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Events;
using Shared;
using GameEngine;
using Akka.Actor;
using GameEngine.Payments;

namespace ServerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static ActorSystem system;
        private static Module.ModuleActors _moduleActors;
        void SetupLogging()
        {
            var settings = Shared.Settings.Get();
            StackifyLib.Logger.GlobalApiKey = settings.Stackify.GlobalApiKey;
            StackifyLib.Logger.GlobalAppName = settings.Stackify.GlobalAppName;
            StackifyLib.Logger.GlobalEnvironment = settings.Stackify.GlobalEnvironment;
            var loggerCfg = new Serilog.LoggerConfiguration();
            Log.Logger = loggerCfg.MinimumLevel.Verbose()
               .WriteTo.Trace(LogEventLevel.Debug)
               .WriteTo.Stackify()
               .CreateLogger();
        }

        private void SetupServices()
        {
            var settings = Shared.Settings.Get();

            system = ActorSystem.Create(settings.Systems.Server);

            if (settings.Enviroment.Configuration.IsDEBUG || settings.Enviroment.Configuration.IsSTAGING ||
                settings.Enviroment.Configuration.IsRELEASE)
            {
                var sendActorService = new SendActorTypes.SendActorService(
                    GameEngine.Payments.Storage.StorageService(), BlockApi.BlockApi.Api());
                var appServices = new Module.AppServices(GameEngine.Rates.Query.RateQueryService(),
                    GameEngine.Rates.Storage.StorageService(), sendActorService);

                _moduleActors = Module.Create(Model.Network.BTCTEST, system, appServices);
            }
        }

        private void Shutdown()
        {
            _moduleActors.ServerActor.Tell(new Commands.ServerCommand.ServerShutdown());
            Thread.Sleep(10000);
            Module.Kill(_moduleActors);
            Thread.Sleep(1000);
            Log.Logger.Information("Service stopped.");
        }

        public override void Run()
        {
            SetupLogging();

            foreach(var endpoint in RoleEnvironment.CurrentRoleInstance.InstanceEndpoints)
            {
                Log.Information("{@Key}: {@Value}", endpoint.Key, endpoint.Value);
            }
            
            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;

            bool result = base.OnStart();

            Trace.TraceInformation("ServerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("ServerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            Shutdown();

            base.OnStop();

            Trace.TraceInformation("ServerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            SetupServices();

            var settings = Shared.Settings.Get();

            Module.Start(_moduleActors);
            Log.Logger.Information("Service started in {@Mode}", settings.Enviroment.Configuration);
            Log.Logger.Information("Current settings : {@Settings}", settings);

            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
