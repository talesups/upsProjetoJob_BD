using System;
using System.Web;
using System.Web.Http;

namespace ups_Work_Job_API
{
    public class Global : HttpApplication
    {
        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Método de inicio da aplicação
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        void Application_Start(object sender, EventArgs e)
        {
            // Código que é executado na inicialização do aplicativo
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
        #endregion
    }
}