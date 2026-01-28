
using System;

namespace ups_Entities
{
    public sealed class ScheduleVO
    {
        public bool Enabled { get; set; }
        public byte RecurrenceType { get; set; }
        public int? IntervalN { get; set; }
        public TimeSpan? TimeOfDay { get; set; }
        public string DaysOfWeek { get; set; }
        public byte? DayOfMonth { get; set; }
        public byte? MonthOfYear { get; set; }
        public DateTime? FixedDateTimeUtc { get; set; }
        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }
        public string TimeZoneId { get; set; }
    }
}
