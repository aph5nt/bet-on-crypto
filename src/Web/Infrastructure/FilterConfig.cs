using System.Web;
using System.Web.Mvc;

// ReSharper disable once CheckNamespace
namespace Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}