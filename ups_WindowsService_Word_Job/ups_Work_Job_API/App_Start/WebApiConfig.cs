using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ups_Work_Job_API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Serviços e configuração da API da Web

            // Rotas da API da Web
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

        }
    }
}
