using System.Web.Http;
using Swashbuckle.Application;

namespace ups_Work_Job_API
{
    public static class WebApiConfig
    {

        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Método de registro da aplicação
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public static void Register(HttpConfiguration config)
        {
            // Rotas Web API
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
        #endregion
    }
}
