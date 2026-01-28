
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ups_Common;
using ups_Entities;

namespace ups_DAO
{
    public class JobSchedulesDao
    {
        #region <<<< MÉTODOS PRIVADOS >>>>

        private readonly string _connName;

        /// <summary>
        /// Método de cálculo de período de tempo para novos processamento job
        /// </summary>
        /// <param name="r"></param>
        /// <returns name="JobSchedule"></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        private JobScheduleVO Map(SqlDataReader r) =>
            new JobScheduleVO
            {
                ScheduleId = r.GetInt32(r.GetOrdinal("ScheduleId")),
                JobId = r.GetInt32(r.GetOrdinal("JobId")),
                Enabled = r.GetBoolean(r.GetOrdinal("Enabled")),
                RecurrenceType = (ups_Entities.RecurrenceType)r.GetByte(r.GetOrdinal("RecurrenceType")),
                IntervalN = r.IsDBNull(r.GetOrdinal("IntervalN"))
                    ? (int?)null
                    : r.GetInt32(r.GetOrdinal("IntervalN")),
                TimeOfDay = r.IsDBNull(r.GetOrdinal("TimeOfDay"))
                    ? (TimeSpan?)null
                    : r.GetTimeSpan(r.GetOrdinal("TimeOfDay")),
                DaysOfWeek = r.IsDBNull(r.GetOrdinal("DaysOfWeek"))
                    ? null
                    : r.GetString(r.GetOrdinal("DaysOfWeek")),
                DayOfMonth = r.IsDBNull(r.GetOrdinal("DayOfMonth"))
                    ? (byte?)null
                    : r.GetByte(r.GetOrdinal("DayOfMonth")),
                MonthOfYear = r.IsDBNull(r.GetOrdinal("MonthOfYear"))
                    ? (byte?)null
                    : r.GetByte(r.GetOrdinal("MonthOfYear")),
                FixedDateTimeUtc = r.IsDBNull(r.GetOrdinal("FixedDateTimeUtc"))
                    ? (DateTime?)null
                    : r.GetDateTime(r.GetOrdinal("FixedDateTimeUtc")),
                StartDateUtc = r.IsDBNull(r.GetOrdinal("StartDateUtc"))
                    ? (DateTime?)null
                    : r.GetDateTime(r.GetOrdinal("StartDateUtc")),
                EndDateUtc = r.IsDBNull(r.GetOrdinal("EndDateUtc"))
                    ? (DateTime?)null
                    : r.GetDateTime(r.GetOrdinal("EndDateUtc")),
                TimeZoneId = r.GetString(r.GetOrdinal("TimeZoneId")),
                NextRunUtc = r.IsDBNull(r.GetOrdinal("NextRunUtc"))
                    ? (DateTime?)null
                    : r.GetDateTime(r.GetOrdinal("NextRunUtc")),
                LastEvaluatedUtc = r.IsDBNull(r.GetOrdinal("LastEvaluatedUtc"))
                    ? (DateTime?)null
                    : r.GetDateTime(r.GetOrdinal("LastEvaluatedUtc"))
            };
        #endregion


        #region <<<< MÉTODOS PÚBLICOS >>>>

        public JobSchedulesDao(string connName) => _connName = connName;

        /// <summary>
        /// Métodos de consulta de agenda de jobs a serem executados
        /// </summary>
        /// <param name="utcNow"></param>
        /// <returns nam="JobSchedule"></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public IEnumerable<JobScheduleVO> GetDue(DateTime utcNow)
        {
            var list = new List<JobScheduleVO>();

            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(
                "SELECT * FROM dbo.JobSchedules " +
                "WHERE Enabled = 1 AND NextRunUtc IS NOT NULL AND NextRunUtc <= @now",
                conn))
            {
                cmd.Parameters.Add("@now", SqlDbType.DateTime2).Value = utcNow;

                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(Map(r));
            }

            return list;
        }

        /// <summary>
        /// Método de consulta de job pelo id
        /// </summary>
        /// <param name="jobId"></param>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public IEnumerable<JobScheduleVO> GetByJobId(int jobId)
        {
            var list = new List<JobScheduleVO>();

            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(
                "SELECT * FROM dbo.JobSchedules WHERE JobId = @id",
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

        /// <summary>
        /// Método de alteração de próxima execução da agenda de jobs
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="nextRunUtc"></param>
        /// <param name="evalUtc"></param>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public void UpdateNextRunAndEval(int scheduleId, DateTime? nextRunUtc, DateTime evalUtc)
        {
            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(
                "UPDATE dbo.JobSchedules " +
                "SET NextRunUtc = @next, LastEvaluatedUtc = @eval " +
                "WHERE ScheduleId = @id",
                conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = scheduleId;
                cmd.Parameters.Add("@next", SqlDbType.DateTime2).Value =
                    (object)nextRunUtc ?? DBNull.Value;
                cmd.Parameters.Add("@eval", SqlDbType.DateTime2).Value = evalUtc;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Método de lista de próxima execução da agenda de jobs
        /// </summary>
        /// <param name="take"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public List<DueScheduleVO> GetDueSchedules(int take)
        {
            var list = new List<DueScheduleVO>();
            using (var conn = Db.GetConnection(_connName))
            using (var cmd = new SqlCommand(@"
                SELECT 
                     s.ScheduleId
                    ,s.JobId
                    ,COALESCE(s.NextRunUtc, SYSUTCDATETIME()) AS NextRunUtc
                    ,s.Enabled
                    ,j.Name
                    ,j.Enabled AS JobEnabled
                    ,j.MaxRetries
                    ,j.RetryDelaySec
                    ,j.ConcurrencyKey
                FROM dbo.JobSchedules s
                JOIN dbo.Jobs j ON j.JobId = s.JobId
                WHERE s.Enabled = 1
                  AND j.Enabled = 1
                  AND COALESCE(s.NextRunUtc, DATEADD(day,-1,SYSUTCDATETIME())) <= SYSUTCDATETIME()
                ORDER BY s.NextRunUtc ASC, s.JobId ASC;", conn))
            {
                // Se quiser limitar quantidade:
                // cmd.CommandText = "SELECT TOP (@take) ...";
                // cmd.Parameters.Add("@take", SqlDbType.Int).Value = take;

                cmd.CommandTimeout = Db.DefaultCommandTimeoutSec;
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DueScheduleVO
                        {
                            ScheduleId = rd.GetInt32(0),
                            JobId = rd.GetInt32(1),
                            NextRunUtc = rd.GetDateTime(2),
                            Enabled = rd.GetBoolean(3),
                            Name = rd.GetString(4),
                            JobEnabled = rd.GetBoolean(5),
                            MaxRetries = rd.GetInt32(6),
                            RetryDelaySec = rd.GetInt32(7),
                            ConcurrencyKey = rd.IsDBNull(8) ? null : rd.GetString(8)
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Método que marca o momento da próxima agenda de jobs
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="conn"></param>
        /// <param name="tx"></param>
        /// <param name="timeoutSec"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public bool MarkLastEvaluated(int scheduleId, SqlConnection conn, SqlTransaction tx, int timeoutSec = 10)
        {
            using (var cmd = new SqlCommand(
                "UPDATE dbo.JobSchedules SET LastEvaluatedUtc = SYSUTCDATETIME() WHERE ScheduleId = @id", conn, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = scheduleId;
                cmd.CommandTimeout = timeoutSec;
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        /// <summary>
        /// Método que marca o momento da próxima agenda de jobs
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <param name="nextUtc"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public int UpdateNextRunUtc(int scheduleId, DateTime? nextUtc)
        {
            using (var conn = Db.GetConnection(_connName))
            using (var cmd = new SqlCommand(
                "UPDATE dbo.JobSchedules SET NextRunUtc = @next, LastEvaluatedUtc = SYSUTCDATETIME() WHERE ScheduleId = @sid", conn))
            {
                cmd.Parameters.Add("@next", SqlDbType.DateTime2).Value = (object)nextUtc ?? DBNull.Value;
                cmd.Parameters.Add("@sid", SqlDbType.Int).Value = scheduleId;
                cmd.CommandTimeout = Db.DefaultCommandTimeoutSec;

                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Método que busca dados das agendas
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Created by: Silva, André
        /// Created Date: 26 01 2026
        /// </remarks>
        public ScheduleVO LoadScheduleSync(int scheduleId)
        {
            using (var conn = Db.GetConnection(_connName))
            using (var cmd = new SqlCommand(@"
                SELECT Enabled, RecurrenceType, (case when IntervalN is null then 0 else IntervalN end) IntervalN, 
                       TimeOfDay, DaysOfWeek, DayOfMonth, MonthOfYear,
                       FixedDateTimeUtc, StartDateUtc, EndDateUtc, TimeZoneId
                  FROM dbo.JobSchedules
                 WHERE ScheduleId = @sid;", conn))
            {
                cmd.Parameters.Add("@sid", SqlDbType.Int).Value = scheduleId;

                conn.Open();
                using (var rd = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!rd.Read())
                        throw new InvalidOperationException("Schedule não encontrado");

                    return new ScheduleVO
                    {
                        Enabled = rd.GetBoolean(0),
                        RecurrenceType = rd.GetByte(1),
                        IntervalN = rd.IsDBNull(2) ? (int?)null : rd.GetInt32(2),
                        TimeOfDay = rd.IsDBNull(3) ? (TimeSpan?)null : rd.GetTimeSpan(3),
                        DaysOfWeek = rd.IsDBNull(4) ? null : rd.GetString(4),
                        DayOfMonth = rd.IsDBNull(5) ? (byte?)null : rd.GetByte(5),
                        MonthOfYear = rd.IsDBNull(6) ? (byte?)null : rd.GetByte(6),
                        FixedDateTimeUtc = rd.IsDBNull(7) ? (DateTime?)null : rd.GetDateTime(7),
                        StartDateUtc = rd.IsDBNull(8) ? (DateTime?)null : rd.GetDateTime(8),
                        EndDateUtc = rd.IsDBNull(9) ? (DateTime?)null : rd.GetDateTime(9),
                        TimeZoneId = rd.IsDBNull(10) ? "UTC" : rd.GetString(10)
                    };
                }
            }
        }

        #endregion
    }
}
