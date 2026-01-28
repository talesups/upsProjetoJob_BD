
using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;

namespace ups_Work_Job_Service
{
    public sealed class UpsWindowsServiceWorkJob : ServiceBase
    {
        #region <<<< MÉTODOS PRIVADOS >>>>

        private Scheduler _scheduler;

        /// <summary>
        /// Método de inicio do processamento por agendamento
        /// </summary>
        /// <param name="args"></param>
        /// <returns name="Job"></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        protected override void OnStart(string[] args)
        {
            try
            {
                Trace.TraceInformation("UpsWorkJobService iniciando...");

                int poll = int.TryParse(ConfigurationManager.AppSettings["PollIntervalSec"], out var p) ? p : 15;
                int par = int.TryParse(ConfigurationManager.AppSettings["MaxDegreeOfParallelism"], out var m) ? m : 1;
                string connName = ConfigurationManager.AppSettings["DbConnName"] ?? "SqlServer";

                _scheduler = new Scheduler(poll, par, connName);
                _scheduler.Start();

                Trace.TraceInformation("UpsWorkJobService iniciado.");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Falha ao iniciar serviço: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Método de parada do processamento por agendamento
        /// </summary>
        /// <param></param>
        /// <returns name="Job"></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        protected override void OnStop()
        {
            Trace.TraceInformation("UpsWorkJobService parando...");
            _scheduler?.Stop();
            Trace.TraceInformation("UpsWorkJobService parado.");
        }
        #endregion

        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Método de consulta de jobStep por ID
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public UpsWindowsServiceWorkJob()
        {
            ServiceName = "UpsWorkJobService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true; // usa EventLog padrão do serviço
        }
        #endregion
    }

}
