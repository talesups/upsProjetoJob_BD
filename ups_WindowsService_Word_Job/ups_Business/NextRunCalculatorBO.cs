
using System;
using System.Linq;
using ups_DAO;
using ups_Entities;

namespace ups_Business
{
    public static class NextRunCalculatorBO
    {

        #region <<<< MÉTODOS PRIVADOS >>>>

        private static readonly JobSchedulesDao _dao = new JobSchedulesDao("");

        private static int[] ParseDays(string csv) =>
            string.IsNullOrWhiteSpace(csv) ? Array.Empty<int>()
                                           : csv.Split(',').Select(s => int.Parse(s.Trim())).ToArray();

        private static DateTime NextDaily(DateTime nowLocal, TimeSpan tod)
        {
            var candidate = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day).Add(tod);
            return candidate <= nowLocal ? candidate.AddDays(1) : candidate;
        }

        private static DateTime NextWeekly(DateTime nowLocal, TimeSpan tod, int[] days)
        {
            for (int i = 0; i <= 7; i++)
            {
                var d = nowLocal.AddDays(i);
                if (days.Contains((int)d.DayOfWeek))
                {
                    var candidate = new DateTime(d.Year, d.Month, d.Day).Add(tod);
                    if (candidate > nowLocal) return candidate;
                }
            }
            var nextWeek = nowLocal.AddDays(7);
            return new DateTime(nextWeek.Year, nextWeek.Month, nextWeek.Day).Add(tod);
        }

        private static DateTime NextMonthly(DateTime nowLocal, int day, TimeSpan tod)
        {
            int y = nowLocal.Year, m = nowLocal.Month;
            int dim = DateTime.DaysInMonth(y, m);
            int d = Math.Min(day, dim);
            var candidate = new DateTime(y, m, d).Add(tod);
            if (candidate <= nowLocal)
            {
                m = m == 12 ? 1 : m + 1;
                y = m == 1 ? y + 1 : y;
                dim = DateTime.DaysInMonth(y, m);
                d = Math.Min(day, dim);
                candidate = new DateTime(y, m, d).Add(tod);
            }
            return candidate;
        }

        private static DateTime NextYearly(DateTime nowLocal, int month, int day, TimeSpan tod)
        {
            int y = nowLocal.Year;
            int dim = DateTime.DaysInMonth(y, month);
            int d = Math.Min(day, dim);
            var candidate = new DateTime(y, month, d).Add(tod);
            if (candidate <= nowLocal)
            {
                y += 1;
                dim = DateTime.DaysInMonth(y, month);
                d = Math.Min(day, dim);
                candidate = new DateTime(y, month, d).Add(tod);
            }
            return candidate;
        }

        #endregion

        ///// <summary>
        ///// Calcula o próximo disparo em horário local (sem conversão de fuso/UTC).
        ///// </summary>
        //public static DateTime? ComputeNextRunUtc(int scheduleId, int jobId)
        //{
        //    // carrega sincrono do DAO
        //    ScheduleVO s = _dao.LoadScheduleSync(scheduleId);
        //    if (!s.Enabled) return null;

        //    DateTime nowLocal = DateTime.Now;

        //    DateTime? nextLocal = s.RecurrenceType switch
        //    {
        //        0 => s.FixedDateTimeUtc.HasValue
        //                ? (s.FixedDateTimeUtc.Value <= nowLocal ? (DateTime?)null : s.FixedDateTimeUtc.Value)
        //                : null,

        //        1 => s.IntervalN.HasValue ? nowLocal.AddMinutes(s.IntervalN.Value) : (DateTime?)null,
        //        2 => s.IntervalN.HasValue ? nowLocal.AddHours(s.IntervalN.Value) : (DateTime?)null,
        //        3 => s.TimeOfDay.HasValue ? NextDaily(nowLocal, s.TimeOfDay.Value) : (DateTime?)null,

        //        4 => (s.TimeOfDay.HasValue && !string.IsNullOrWhiteSpace(s.DaysOfWeek))
        //                ? NextWeekly(nowLocal, s.TimeOfDay.Value, ParseDays(s.DaysOfWeek))
        //                : (DateTime?)null,

        //        5 => (s.TimeOfDay.HasValue && s.DayOfMonth.HasValue)
        //                ? NextMonthly(nowLocal, s.DayOfMonth.Value, s.TimeOfDay.Value)
        //                : (DateTime?)null,

        //        6 => (s.TimeOfDay.HasValue && s.DayOfMonth.HasValue && s.MonthOfYear.HasValue)
        //                ? NextYearly(nowLocal, s.MonthOfYear.Value, s.DayOfMonth.Value, s.TimeOfDay.Value)
        //                : (DateTime?)null,

        //        7 => s.IntervalN.HasValue ? nowLocal.AddSeconds(s.IntervalN.Value) : (DateTime?)null,

        //        _ => null
        //    };

        //    if (!nextLocal.HasValue) return null;

        //    // Janela de agendamento em horário local
        //    if (s.StartDateUtc.HasValue && nextLocal.Value < s.StartDateUtc.Value)
        //        nextLocal = s.StartDateUtc.Value;

        //    if (s.EndDateUtc.HasValue && nextLocal.Value > s.EndDateUtc.Value)
        //        return null;

        //    return nextLocal;
        //}


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
        public static DateTime? ComputeNextRunUtc(int scheduleId, int jobId)
        {
            // carrega sincrono do DAO
            ScheduleVO s = _dao.LoadScheduleSync(scheduleId);
            if (!s.Enabled) return null;

            DateTime nowLocal = DateTime.Now;

            DateTime? nextLocal = null;

            switch (s.RecurrenceType)
            {
                case 0:
                    if (s.FixedDateTimeUtc.HasValue)
                    {
                        nextLocal = (s.FixedDateTimeUtc.Value <= nowLocal)
                            ? (DateTime?)null
                            : s.FixedDateTimeUtc.Value;
                    }
                    break;

                case 1:
                    if (s.IntervalN.HasValue)
                        nextLocal = nowLocal.AddMinutes(s.IntervalN.Value);
                    break;

                case 2:
                    if (s.IntervalN.HasValue)
                        nextLocal = nowLocal.AddHours(s.IntervalN.Value);
                    break;

                case 3:
                    if (s.TimeOfDay.HasValue)
                        nextLocal = NextDaily(nowLocal, s.TimeOfDay.Value);
                    break;

                case 4:
                    if (s.TimeOfDay.HasValue && !string.IsNullOrWhiteSpace(s.DaysOfWeek))
                        nextLocal = NextWeekly(nowLocal, s.TimeOfDay.Value, ParseDays(s.DaysOfWeek));
                    break;

                case 5:
                    if (s.TimeOfDay.HasValue && s.DayOfMonth.HasValue)
                        nextLocal = NextMonthly(nowLocal, s.DayOfMonth.Value, s.TimeOfDay.Value);
                    break;

                case 6:
                    if (s.TimeOfDay.HasValue && s.DayOfMonth.HasValue && s.MonthOfYear.HasValue)
                        nextLocal = NextYearly(nowLocal, s.MonthOfYear.Value, s.DayOfMonth.Value, s.TimeOfDay.Value);
                    break;

                case 7:
                    if (s.IntervalN.HasValue)
                        nextLocal = nowLocal.AddSeconds(s.IntervalN.Value);
                    break;

                default:
                    nextLocal = null;
                    break;
            }

            if (!nextLocal.HasValue) return null;

            // Janela de agendamento em horário local
            if (s.StartDateUtc.HasValue && nextLocal.Value < s.StartDateUtc.Value)
                nextLocal = s.StartDateUtc.Value;

            if (s.EndDateUtc.HasValue && nextLocal.Value > s.EndDateUtc.Value)
                return null;

            return nextLocal;
        }
        #endregion


    }
}
