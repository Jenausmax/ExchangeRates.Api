using ExchangeRatesBot.Domain.Interfaces;
using ExchangeRatesBot.Domain.Models.GetModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.App.Services
{
    public class MessageValuteService : IMessageValute
    {
        private readonly IProcessingService _processingService;
        private readonly ILogger _logger;

        // Русская культура для форматирования дат
        private static readonly CultureInfo RuCulture = new CultureInfo("ru-RU");

        public MessageValuteService(IProcessingService processingService, ILogger logger)
        {
            _processingService = processingService;
            _logger = logger;
        }

        /// <summary>
        /// Форматирует дату в вид "10 мар"
        /// </summary>
        private string FormatDate(DateTime date)
        {
            return date.ToString("dd MMM", RuCulture).TrimEnd('.');
        }

        /// <summary>
        /// Форматирует одну строку курса с трендовым индикатором и изменением
        /// </summary>
        private string FormatValuteLine(Valute valute)
        {
            var date = FormatDate(valute.DateValute);
            var value = valute.Value.ToString("F2", CultureInfo.InvariantCulture);

            // Если нет данных за предыдущий день — просто дата и курс
            if (valute.AbsoluteDiff == null)
                return $"{date}  {value}";

            var diff = valute.AbsoluteDiff.Value;
            var percent = valute.PercentDiff.Value;

            // Определяем трендовый индикатор с порогом 0.005 для фильтрации шума
            var trend = Math.Abs(diff) <= 0.005 ? "→" : (diff > 0 ? "↑" : "↓");
            var sign = diff >= 0 ? "+" : "";

            return $"{date}  {value} {trend} {sign}{diff:F2} ({sign}{percent:F2}%)";
        }

        public async Task<string> GetValuteMessage(int day, string charCode, CancellationToken cancel)
        {
            var valutesRoot = await _processingService.RequestProcessing(day, charCode, cancel);

            if (valutesRoot == null || day == 0)
            {
                _logger.Error($"Type error: {typeof(MessageValuteService)}.: Collection null");
                return " ";
            }

            var getValutesModels = valutesRoot.GetValuteModels;
            var valutes = new List<Valute>();
            foreach (var item in getValutesModels)
            {
                valutes.Add(new Valute()
                {
                    CharCode = item.CharCode,
                    DateValute = item.DateValute,
                    Name = item.Name,
                    Value = item.Value
                });
            }

            // Дедупликация по дате и сортировка от новых к старым
            valutes = valutes
                .GroupBy(e => e.DateValute.Date)
                .Select(g => g.First())
                .OrderByDescending(v => v.DateValute)
                .ToList();

            // Вычисляем разницу: текущий[i] - предыдущий[i+1]
            // Исправляет баг старой логики: разница теперь записывается в актуальную запись [i], а не в [i+1]
            for (int i = 0; i < valutes.Count - 1; i++)
            {
                var diff = valutes[i].Value - valutes[i + 1].Value;
                valutes[i].AbsoluteDiff = diff;
                valutes[i].PercentDiff = diff / valutes[i + 1].Value * 100;
            }
            // Последний элемент (самая старая дата) остаётся без разницы (null)

            // Защита: после дедупликации список может оказаться пустым
            if (valutes == null || valutes.Count == 0)
            {
                _logger.Warning($"Type warning: {typeof(MessageValuteService)}: No valute data for charCode={charCode}, day={day}");
                return string.Empty;
            }

            // Сборка итоговой строки
            var sb = new StringBuilder();
            sb.AppendLine($"{valutes[0].CharCode}/RUB -- {valutes[0].Name}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━");

            foreach (var valute in valutes)
            {
                sb.AppendLine(FormatValuteLine(valute));
            }

            return sb.ToString();
        }

        public async Task<string> GetValuteMessage(int day, string[] charCodesCollection, CancellationToken cancel)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < charCodesCollection.Length; i++)
            {
                var valuteString = await GetValuteMessage(day, charCodesCollection[i], cancel);

                // Если данных нет — пропускаем блок и не добавляем разделитель
                if (string.IsNullOrEmpty(valuteString))
                    continue;

                sb.Append(valuteString);

                // Пустая строка-разделитель между блоками валют (кроме последней)
                if (i < charCodesCollection.Length - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
