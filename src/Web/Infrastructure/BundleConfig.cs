using System.Web;
using System.Web.Optimization;

// ReSharper disable once CheckNamespace
namespace Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Vendor scripts
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jquery-2.2.1.min.js"));

            // jQuery Validation
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate.min.js",
                "~/Scripts/jquery.validate.unobtrusive.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/Scripts/bootstrap.min.js"));

            //// Inspinia script
            bundles.Add(new ScriptBundle("~/bundles/inspinia").Include(
                "~/Scripts/app/inspinia.js"));

            // SlimScroll
            bundles.Add(new ScriptBundle("~/plugins/slimScroll").Include(
                "~/Scripts/plugins/slimScroll/jquery.slimscroll.min.js"));

            // jQuery plugins
            bundles.Add(new ScriptBundle("~/plugins/metsiMenu").Include(
                "~/Scripts/plugins/metisMenu/metisMenu.min.js"));

            bundles.Add(new ScriptBundle("~/plugins/pace").Include(
                "~/Scripts/plugins/pace/pace.min.js"));

            // Custom libs
            bundles.Add(new ScriptBundle("~/bundles/libs").Include(
                "~/Scripts/knockout-3.4.0.js",
                "~/Scripts/knockout-mapping.js",
                "~/Scripts/moment.js",
                "~/Scripts/Chart.min.js",
                "~/Scripts/jquery.signalR-2.2.0.js",
                "~/Scripts/amplify.min.js",
                "~/Scripts/plugins/toastr/toastr.min.js"));
           
            // My Componens
            bundles.Add(new ScriptBundle("~/bundles/components").Include(
                "~/Scripts/app/common.js",
                "~/Scripts/app/current-games.js",
                "~/Scripts/app/previous-games.js",
                "~/Scripts/app/prize.js",
                "~/Scripts/app/rate-chart.js",
                "~/Scripts/app/shortstat.js",
                "~/Scripts/app/bet.js",
                "~/Scripts/app/balance.js",
                "~/Scripts/app/short-stat.js",
                "~/Scripts/app/busy.js",
                "~/Scripts/app/withdraw.js",
                "~/Scripts/app/deposit.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/ga")
               .Include(GoogleAnalitics()));

            //// CSS style (bootstrap/inspinia)
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/style.css"));

            //   // Font Awesome icons
            //var fonts = new StyleBundle("~/font-awesome/css").Include(
            //    "~/fonts/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform());
            //fonts.Transforms.Clear();
            //bundles.Add(fonts);

            var isRelease = Shared.Settings.Get().Enviroment.Configuration.IsRELEASE;
            if (isRelease)
            {
                BundleTable.EnableOptimizations = true;
            }
        }

        static string GoogleAnalitics()
        {
            var isRelease = Shared.Settings.Get().Enviroment.Configuration.IsRELEASE;
            if (isRelease)
            {
                return "~/Scripts/app/ga.js";
            }

            return "~/Scripts/app/noga.js";
        }
    }
}