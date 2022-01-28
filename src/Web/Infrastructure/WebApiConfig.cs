using System.Net.Http.Formatting;
using System.Web.Http;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new DefaultContractResolver();
            config.Formatters.XmlFormatter.SupportedMediaTypes.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            // Web API routes
            config.MapHttpAttributeRoutes();
        }
    }
}

// http://blog.iteedee.com/2014/03/asp-net-identity-2-0-cookie-token-authentication/