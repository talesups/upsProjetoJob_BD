using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ups_Common
{
    public static class Locking
    {
        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Tenta obter um lock lógico para uma chave de concorrência (sp_getapplock).
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tx"></param>
        /// <param name="concurrencyKey"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public static bool TryAcquireJobLock(SqlConnection conn, SqlTransaction tx, string concurrencyKey, int jobId)
        {
            // Se não houver chave definida no job, use o próprio JobId como chave
            string resource = string.IsNullOrWhiteSpace(concurrencyKey)
                ? $"JOB:{jobId}"
                : $"JOBKEY:{concurrencyKey}";

            using (var cmd = new SqlCommand("sp_getapplock", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Resource", resource);
                cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
                cmd.Parameters.AddWithValue("@LockOwner", "Transaction");
                cmd.Parameters.AddWithValue("@DbPrincipal", "public");
                cmd.Parameters.AddWithValue("@Timeout", 0);
                var ret = cmd.Parameters.Add("@return_value", SqlDbType.Int);
                ret.Direction = ParameterDirection.ReturnValue;
                
                try
                {
                    // await ProcessOneScheduleAsync(s).ConfigureAwait(false);
                    cmd.ExecuteNonQuery();
                    int code = (int)ret.Value; // 0 ou 1 => sucesso, -1 timeout, -2 cancelado, -3 morto, -999 erro
                    return (code >= 0);
                }
                catch (Exception ex)
                {
                    return (true);
                    //Trace.TraceError($"Erro no processamento do schedule {jobId}: {ex}");
                }
                // int code = (int)ret.Value; // 0 ou 1 => sucesso, -1 timeout, -2 cancelado, -3 morto, -999 erro
                ///return (code >= 0);
            }
        }
        #endregion
    }
}
