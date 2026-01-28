using System;
using System.Reflection;
using System.ServiceProcess;

namespace ups_Work_Job_Service
{
    static class Program
    {
        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Método de start da aplicação no momento da instalação do serviço
        /// </summary>
        /// <param></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        static void Main()
        {
            // Se rodar pelo Visual Studio (F5), ficará em modo console
            if (Environment.UserInteractive)
            {
                var svc = new UpsWindowsServiceWorkJob();
                // invoca OnStart/OnStop via reflexão para reaproveitar a lógica
                svc.GetType().GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic)
                   ?.Invoke(svc, new object[] { new string[0] });

                Console.WriteLine("Serviço em modo console. Pressione ENTER para encerrar...");
                Console.ReadLine();

                //                svc.GetType().GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic)
                //                   ?.Invoke(svc, null);
            }
            else
            {
                // Execução como Serviço Windows
                ServiceBase.Run(new ServiceBase[] { new UpsWindowsServiceWorkJob() });
            }
        }
        #endregion
    }
}

