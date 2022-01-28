using System;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;
using Shared;

namespace Web
{
    public class WebRole : RoleEntryPoint
    {
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

        public override void Run()
        {
            SetupLogging();

            foreach (var endpoint in RoleEnvironment.CurrentRoleInstance.InstanceEndpoints)
            {
                Log.Information("{@Key} : {@Value}", endpoint.Key, endpoint.Value);
            }

            using (var serverManager = new ServerManager())
            {
                try
                {
                    var mainSite = serverManager.Sites[RoleEnvironment.CurrentRoleInstance.Id + "_Web"];
                    if (mainSite != null)
                    {
                        var mainApplication = mainSite.Applications["/"];
                        mainApplication["preloadEnabled"] = true;

                        var mainApplicationPool = serverManager.ApplicationPools[mainApplication.ApplicationPoolName];
                        mainApplicationPool.AutoStart = true;
                        mainApplicationPool.Recycling.PeriodicRestart.Time = TimeSpan.FromSeconds(0);
                        mainApplicationPool.ProcessModel.IdleTimeout = TimeSpan.FromSeconds(0);
                        mainApplicationPool.Recycling.DisallowOverlappingRotation = true;
                        mainApplicationPool.Recycling.DisallowRotationOnConfigChange = false;

                        serverManager.CommitChanges();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error occured.");
                }
            }

            base.Run();
        }
    }
}
