using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Infrastructure.DB.Models
{
    public class ValuteModelDb
    {
        [Key]
        public int Id { get; set; }
        public string ValuteId { get; set; }
        public DateTime DateSave { get; set; }
        public string NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public double Previous { get; set; }
        /// <summary>
        /// Курс на эту дату.
        /// </summary>
        public DateTime DateValute { get; set; }
        /// <summary>
        /// Дата обновления на апи
        /// </summary>
        public DateTime TimeStampUpdateValute { get; set; }
    }
}
