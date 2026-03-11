using System;

namespace ExchangeRatesBot.Domain.Models.GetModels
{
    public class ValuteStatistics
    {
        public string CharCode { get; set; }
        public string Name { get; set; }

        /// <summary>Текущий курс (последнее значение в выборке).</summary>
        public double CurrentValue { get; set; }

        /// <summary>Максимальное значение за период.</summary>
        public double MaxValue { get; set; }

        /// <summary>Дата максимального значения.</summary>
        public DateTime MaxDate { get; set; }

        /// <summary>Минимальное значение за период.</summary>
        public double MinValue { get; set; }

        /// <summary>Дата минимального значения.</summary>
        public DateTime MinDate { get; set; }

        /// <summary>Абсолютное изменение (последний - первый).</summary>
        public double AbsoluteChange { get; set; }

        /// <summary>Процентное изменение.</summary>
        public double PercentChange { get; set; }

        /// <summary>Количество дней в выборке.</summary>
        public int DaysCount { get; set; }
    }
}
