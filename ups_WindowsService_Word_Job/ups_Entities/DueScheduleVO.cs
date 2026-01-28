
using System;

namespace ups_Entities
{
    #region <<<< MÉTODOS PÚBLICOS >>>>

    /// <summary>
    /// Método de prenchimento de agendamento a ser realizado
    /// </summary>
    /// <param></param>
    /// <returns></returns>
    /// <remarks>
    /// Created by: Silva, André
    /// Created Date: 26 01 2026
    /// </remarks>
    public sealed class DueScheduleVO
    {
        public int ScheduleId { get; set; }
        public int JobId { get; set; }
        public DateTime NextRunUtc { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public bool JobEnabled { get; set; }
        public int MaxRetries { get; set; }
        public int RetryDelaySec { get; set; }
        public string ConcurrencyKey { get; set; }
    }
    #endregion
}
