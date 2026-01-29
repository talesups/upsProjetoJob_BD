
using System;
using System.Linq;
using ups_DAO;
using ups_Entities;

namespace ups_Business
{
    public class SchedulerServiceBO
    {
        #region <<<< MÉTODOS PRIVADOS >>>>
        private readonly JobSchedulesDao _dao = new JobSchedulesDao("SqlServer");
        #endregion

        #region <<<< MÉTODOS PÚBLICOS >>>>

        /// <summary>
        /// Métodos que validam regras de validação dos sheduler de serviço
        /// </summary>
        /// <param name="now"></param>
        /// <returns>ValueObject</returns>
        /// <remarks>
        /// Created By: Silva, Andre
        /// Created Date: 26 01 2026
        /// </remarks>
        public void EvaluateAndUpdateNextRuns(DateTime now)
        {
            foreach (var sch in _dao.GetDue(now))
            {
                var next = ComputeNextRun(sch, now);
                _dao.UpdateNextRunAndEval(sch.ScheduleId, next, now);
            }
        }

        /// <summary>
        /// Método de cáculo da próxima execução 
        /// </summary>
        /// <param name="sch"></param>
        /// <param name="utvNow"></param>
        /// <returns>DateTime</returns>
        /// <remarks>
        /// Created By: Silva, Andre
        /// Created Date: 26 01 2026
        /// </remarks>
        public DateTime? ComputeNextRun(JobScheduleVO sch, DateTime now)
        {
            if (sch.EndDateUtc.HasValue && now > sch.EndDateUtc.Value)
                return null;

            var timeOfDay = sch.TimeOfDay ?? TimeSpan.Zero;

            switch (sch.RecurrenceType)
            {

                //// Aqui UTC provavelmente 
                case RecurrenceType.Once:
                    return (sch.FixedDateTimeUtc.HasValue && sch.FixedDateTimeUtc.Value > now)
                        ? sch.FixedDateTimeUtc.Value
                        : (DateTime?)null;

                case RecurrenceType.Minute:
                    return now.AddMinutes(sch.IntervalN.GetValueOrDefault(1));

                case RecurrenceType.Hour:
                    return now.AddHours(sch.IntervalN.GetValueOrDefault(1));

                case RecurrenceType.Daily:
                    {
                        int n = sch.IntervalN.GetValueOrDefault(1);
                        var candidate = now.Date.AddDays(n).Add(timeOfDay);
                        if (sch.StartDateUtc.HasValue && candidate < sch.StartDateUtc.Value)
                            candidate = sch.StartDateUtc.Value.Date.Add(timeOfDay);
                        return candidate;
                    }

                case RecurrenceType.Weekly:
                    {
                        var days = (sch.DaysOfWeek ?? string.Empty)
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s, out var d) ? d : -1)
                            .Where(d => d >= 0 && d <= 6)
                            .ToArray();

                        if (days.Length == 0) return null;

                        int today = (int)now.DayOfWeek;
                        int nWeeks = sch.IntervalN.GetValueOrDefault(1);

                        for (int offset = 0; offset <= 7 * nWeeks; offset++)
                        {
                            var d = (today + offset) % 7;
                            if (days.Contains(d))
                            {
                                var candidate = now.Date.AddDays(offset).Add(timeOfDay);
                                if (candidate > now) return candidate;
                            }
                        }

                        return now.Date.AddDays(7 * nWeeks).Add(timeOfDay);
                    }

                case RecurrenceType.Monthly:
                    {
                        int n = sch.IntervalN.GetValueOrDefault(1);
                        byte day = sch.DayOfMonth.GetValueOrDefault(1);

                        var nextMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local)
                            .AddMonths(n);

                        int dim = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                        int d = Math.Min(day, (byte)dim);

                        return new DateTime(nextMonth.Year, nextMonth.Month, d, 0, 0, 0, DateTimeKind.Local)
                            .Add(timeOfDay);
                    }

                case RecurrenceType.Yearly:
                    {
                        int n = sch.IntervalN.GetValueOrDefault(1);
                        byte month = sch.MonthOfYear.GetValueOrDefault(1);
                        byte day = sch.DayOfMonth.GetValueOrDefault(1);

                        int targetYear = now.Year + n;
                        int dim = DateTime.DaysInMonth(targetYear, month);
                        int d = Math.Min(day, (byte)dim);

                        return new DateTime(targetYear, month, d, 0, 0, 0, DateTimeKind.Local)
                            .Add(timeOfDay);
                    }

                default:
                    return null;
            }
        }
    }
    #endregion
}
