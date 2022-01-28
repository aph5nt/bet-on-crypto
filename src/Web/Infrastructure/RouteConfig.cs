using System.Web.Mvc;
using System.Web.Routing;

// ReSharper disable once CheckNamespace
namespace Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("mu-5786a4ed-867ee6b8-99248209-f06b9984.txt");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new {controller = "Game", action = "Index", id = UrlParameter.Optional}
                );
        }
    }
}