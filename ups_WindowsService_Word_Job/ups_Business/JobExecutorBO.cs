
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ups_Entities;  // DueSchedule/JobStep VO se for usado


namespace ups_Business
{
    public static class JobExecutor
    {
        #region <<<< MÉTODOS PRIVADOS >>>>

        /// <summary>
        /// Método que realiza a busca de dados dos steps
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static List<JobStepVO> LoadSteps(int jobId)
        {
            var list = new List<JobStepVO>();
            using (var conn = new SqlConnection(GetConn()))
            using (var cmd = new SqlCommand(
                "SELECT StepId, JobId, StepNo, Script, TimeoutSec FROM dbo.JobSteps WHERE JobId=@jid ORDER BY StepNo ASC", conn))
            {
                cmd.Parameters.Add("@jid", SqlDbType.Int).Value = jobId;
                cmd.CommandTimeout = 30;

                conn.Open();
                using (var rd = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    while (rd.Read())
                    {
                        list.Add(new JobStepVO
                        {
                            StepId = rd.GetInt32(0),
                            JobId = rd.GetInt32(1),
                            StepNo = rd.GetInt32(2),
                            Script = rd.GetString(3),
                            TimeoutSec = rd.IsDBNull(4) ? (int?)null : rd.GetInt32(4)
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Método que realiza retry nos steps
        /// </summary>
        /// <param name="st"></param>
        /// <param name="maxRetries"></param>
        /// <param name="retryDelaySec"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static bool ExecuteStepWithRetry(JobStepVO st, int maxRetries, int retryDelaySec, out string errorMessage)
        {
            int attempts = 0;
            var baseDelay = TimeSpan.FromSeconds(Math.Max(0, retryDelaySec));
            Exception lastException = null;
            errorMessage = string.Empty;

            while (true)
            {
                attempts++;
                try
                {
                    using (var conn = new SqlConnection(GetConn()))
                    using (var cmd = new SqlCommand(st.Script, conn))
                    {
                        cmd.CommandTimeout = st.TimeoutSec ?? DefaultTimeout();
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    // Sucesso: limpar mensagem e sair
                    errorMessage = string.Empty;
                    return true;
                }
                catch (SqlException ex) when (IsTransient(ex) && attempts < maxRetries)
                {
                    lastException = ex;

                    var delayMs = Math.Min(
                        baseDelay.TotalMilliseconds * Math.Pow(2, attempts - 1),
                        30000 // teto de 30s
                    );
                    var delay = TimeSpan.FromMilliseconds(delayMs);

                    Trace.TraceWarning(
                        $"Step {st.StepNo} (Job {st.JobId}) erro transitório: {ex.Message}. " +
                        $"Tentativa {attempts}/{maxRetries}. Aguardando {delay.TotalSeconds:F1}s...");

                    System.Threading.Thread.Sleep(delay);
                    // Continua o loop para nova tentativa
                }
                catch (Exception ex)
                {
                    // Exceção não-transiente ou qualquer outra falha no meio
                    lastException = ex;

                    var msg =
                        $"Step {st.StepNo} (Job {st.JobId}) falhou na tentativa {attempts} de {maxRetries}. " +
                        $"Erro: {ex.Message}";

                    Trace.TraceError($"{msg}{Environment.NewLine}{ex}");

                    errorMessage = msg;
                    return false;
                }

                // Se chegou aqui, foi um erro transitório mas ainda há tentativas.
                // Quando o loop recomeçar, tenta novamente.
                // Caso acabe as tentativas, o while não dá return aqui e cai no bloco abaixo.
                if (attempts < maxRetries)
                {
                    var finalMsg =
                        $"Step {st.StepNo} (Job {st.JobId}) falhou após {attempts} tentativas. " +
                        $"Último erro: {lastException?.Message}";

                    Trace.TraceError($"{finalMsg}{Environment.NewLine}{lastException}");
                    errorMessage = finalMsg;
                    return false;
                }
            }
        }

        //private static bool ExecuteStepWithRetry(JobStepVO st, int maxRetries, int retryDelaySec, out string errorMessag)
        //{
        //    int attempts = 0;
        //    var baseDelay = TimeSpan.FromSeconds(Math.Max(0, retryDelaySec));

        //    while (true)
        //    {
        //        attempts++;
        //        try
        //        {
        //            using (var conn = new SqlConnection(GetConn()))
        //            using (var cmd = new SqlCommand(st.Script, conn))
        //            {
        //                cmd.CommandTimeout = st.TimeoutSec ?? DefaultTimeout();
        //                conn.Open();
        //                cmd.ExecuteNonQuery();
        //            }
        //            errorMessag = "";
        //            return true;
        //        }
        //        catch (SqlException ex) when (IsTransient(ex) && attempts < maxRetries)
        //        {
        //            var delayMs = Math.Min(
        //                baseDelay.TotalMilliseconds * Math.Pow(2, attempts - 1),
        //                30000 // teto de 30s
        //            );
        //            var delay = TimeSpan.FromMilliseconds(delayMs);

        //            Trace.TraceWarning(
        //                $"Step {st.StepNo} (Job {st.JobId}) erro transitório: {ex.Message}. " +
        //                $"Tentativa {attempts}/{maxRetries}. Aguardando {delay.TotalSeconds:F1}s...");

        //            System.Threading.Thread.Sleep(delay);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Exceção não-transiente ou qualquer outra falha no meio
        //            lastException = ex;

        //            var msg =
        //                $"Step {st.StepNo} (Job {st.JobId}) falhou na tentativa {attempts} de {maxRetries}. " +
        //                $"Erro: {ex.Message}";

        //            Trace.TraceError($"{msg}{Environment.NewLine}{ex}");

        //            errorMessage = msg;
        //            return false;
        //        }
        //    }
        //}

        /// <summary>
        /// Método que realiza insert de histórico dos steps a serem processados
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static long InsertRunHistory(int jobId, string status, string message)
        {
            using (var conn = new SqlConnection(GetConn()))
            using (var cmd = new SqlCommand(@"
                INSERT INTO dbo.JobRunHistory (JobId, Status, Message, HostName)
                VALUES (@jid, @st, @msg, @host);
                SELECT CONVERT(BIGINT, SCOPE_IDENTITY());", conn))
            {
                cmd.Parameters.Add("@jid", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@st", SqlDbType.NVarChar, 50).Value = (object)status ?? DBNull.Value;

                var pMsg = cmd.Parameters.Add("@msg", SqlDbType.NVarChar, -1);
                pMsg.Value = (object)message ?? DBNull.Value;

                cmd.Parameters.Add("@host", SqlDbType.NVarChar, 200).Value = Environment.MachineName;
                cmd.CommandTimeout = 30;

                conn.Open();
                object scalar = cmd.ExecuteScalar();
                return Convert.ToInt64(scalar);
            }
        }

        /// <summary>
        /// Método que marca a finalização do histórico
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static void FinishRunHistory(long runId, string status, string message)
        {
            using (var conn = new SqlConnection(GetConn()))
            using (var cmd = new SqlCommand(
                "UPDATE dbo.JobRunHistory SET FinishedUtc=SYSDATETIME(), Status=@st, Message=@msg WHERE RunId=@rid;", conn))
            {
                cmd.Parameters.Add("@rid", SqlDbType.BigInt).Value = runId;
                cmd.Parameters.Add("@st", SqlDbType.NVarChar, 50).Value = status;
                cmd.Parameters.Add("@msg", SqlDbType.NVarChar, -1).Value = (object)message ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Método que realiza a alteração da última execução
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="status"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static async Task UpdateJobLastRunAsync(int jobId, string status)
        {
            using (var conn = new SqlConnection(GetConn()))
            using (var cmd = new SqlCommand(
                "UPDATE dbo.Jobs SET LastRunUtc=SYSDATETIME(), LastRunStatus=@st WHERE JobId=@jid;", conn))
            {
                cmd.Parameters.Add("@jid", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@st", SqlDbType.NVarChar, 50).Value = status;
                await conn.OpenAsync().ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Método que cria a conexão com o banco de dados
        /// </summary>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static string GetConn()
        {
            // Se você já tem ups_Common.Db.GetConn(), pode chamar ele diretamente:
            // return Db.GetConn();

            // Caso prefira usar App.config local:
            return ConfigurationManager.ConnectionStrings[ConfigurationManager.AppSettings["DbConnName"] ?? "SqlServer"].ConnectionString;
        }

        private static int DefaultTimeout() =>
            int.TryParse(ConfigurationManager.AppSettings["DefaultCommandTimeoutSec"], out var s) ? s : 60;

        /// <summary>
        /// Método realiza a geração de processo Transient
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private static bool IsTransient(SqlException ex)
        {
            // exemplos comuns: -2 (timeout), 4060 (db indisponível), 10928/10929 (throttling), 40197/40501 (Azure)
            var codes = new[] { -2, 4060, 10928, 10929, 40197, 40501 };
            return codes.Contains(ex.Number);
        }

        #endregion

        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Executa sincronicamente a sequência de steps do Job, com retry/backoff por step, registra histórico e atualiza status final.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="maxRetries"></param>
        /// <param name="retryDelaySec"></param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public static async Task<bool> RunJobSync(int jobId, int maxRetries, int retryDelaySec)
        {
            long runId = InsertRunHistory(jobId, "RUNNING", null);
            string err;
            bool ok = false;
            string finalMsg = null;

            try
            {
                var steps = LoadSteps(jobId);
                foreach (var st in steps)
                {
                    ok = ExecuteStepWithRetry(st, maxRetries: 3, retryDelaySec: 2, out err);
                    if (!ok)
                    {
                        finalMsg = $"Falha no Step {st.StepNo} (StepId={st.StepId}) (erro: {err})";
                        break;
                    }
                }

                finalMsg = ok ? "Executado com sucesso" : finalMsg ?? "Falha desconhecida";
                FinishRunHistory(runId, ok ? "SUCCESS" : "FAILED", finalMsg);
            }
            catch (Exception ex)
            {
                ok = false;
                finalMsg = $"Exceção no Job {jobId}: {ex.Message}";
                FinishRunHistory(runId, "FAILED", finalMsg);
            }

            await UpdateJobLastRunAsync(jobId, ok ? "SUCCESS" : "FAILED").ConfigureAwait(false);
            return ok;
        }
        #endregion

    }
}
