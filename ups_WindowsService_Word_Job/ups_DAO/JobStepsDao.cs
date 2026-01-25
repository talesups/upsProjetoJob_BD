
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ups_Common;
using ups_Entities;

namespace ups_DAO
{
    public class JobStepsDao
    {
        #region <<<< MÉTODOS PRIVADOS >>>>

        /// <summary>
        /// Método de mapeamento de dados do job
        /// </summary>
        /// <param name="r"></param>
        /// <returns name="Job"></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private JobStepVO Map(SqlDataReader r) =>
            new JobStepVO
            {
                StepId = r.GetInt32(r.GetOrdinal("StepId")),
                JobId = r.GetInt32(r.GetOrdinal("JobId")),
                StepNo = r.GetInt32(r.GetOrdinal("StepNo")),
                Script = r.GetString(r.GetOrdinal("Script")),
                TimeoutSec = r.IsDBNull(r.GetOrdinal("TimeoutSec"))
                    ? (int?)null
                    : r.GetInt32(r.GetOrdinal("TimeoutSec"))
            };
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
        public IEnumerable<JobStepVO> GetByJobId(int jobId)
        {
            var list = new List<JobStepVO>();

            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(
                "SELECT StepId, JobId, StepNo, Script, TimeoutSec " +
                "FROM dbo.JobSteps WHERE JobId = @id ORDER BY StepNo",
                conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = jobId;

                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(Map(r));
            }

            return list;
        }
        #endregion
    }
}
