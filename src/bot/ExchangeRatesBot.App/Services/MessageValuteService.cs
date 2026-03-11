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

            // Добавляем блок статистики для периодов от 3 дней и более
            if (day >= 3 && valutes.Count >= 2)
            {
                // Создаем копию списка, отсортированную от старых к новым для CalculateStatistics
                var valutesSortedAscending = valutes.OrderBy(v => v.DateValute).ToList();
                var stats = CalculateStatistics(valutesSortedAscending);

                if (stats != null)
                {
                    var maxDateStr = stats.MaxDate.ToString("dd MMM", RuCulture);
                    var minDateStr = stats.MinDate.ToString("dd MMM", RuCulture);

                    var changeSign = stats.AbsoluteChange >= 0 ? "+" : "";
                    var changeStr = $"{changeSign}{stats.AbsoluteChange:0.00}";
                    var percentStr = $"{changeSign}{stats.PercentChange:0.00}%";

                    sb.AppendLine();
                    sb.AppendLine($" Макс: {stats.MaxValue:0.00} ({maxDateStr})");
                    sb.AppendLine($" Мин:  {stats.MinValue:0.00} ({minDateStr})");
                    sb.AppendLine($" Изм:  {changeStr} ({percentStr}) за {stats.DaysCount} дн.");
                }
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

        /// <summary>
        /// Рассчитывает статистику по списку курсов валюты за период.
        /// Принимает уже дедуплицированный список Valute (отсортированный от старых к новым).
        /// </summary>
        private ValuteStatistics CalculateStatistics(List<Valute> valutes)
        {
            if (valutes == null || valutes.Count == 0)
                return null;

            var stats = new ValuteStatistics
            {
                CharCode = valutes[0].CharCode,
                Name = valutes[0].Name
            };

            // Находим элемент с максимальным значением
            var maxItem = valutes.OrderByDescending(v => v.Value).First();
            stats.MaxValue = maxItem.Value;
            stats.MaxDate = maxItem.DateValute;

            // Находим элемент с минимальным значением
            var minItem = valutes.OrderBy(v => v.Value).First();
            stats.MinValue = minItem.Value;
            stats.MinDate = minItem.DateValute;

            // Абсолютное изменение за период (последний - первый)
            var firstValue = valutes.First().Value;  // самый старый курс
            var lastValue = valutes.Last().Value;    // самый свежий курс
            stats.AbsoluteChange = lastValue - firstValue;

            // Процентное изменение
            stats.PercentChange = firstValue != 0
                ? (lastValue - firstValue) / firstValue * 100.0
                : 0;

            // Текущий курс (последнее значение)
            stats.CurrentValue = valutes.Last().Value;

            // Количество дней в выборке
            stats.DaysCount = valutes.Count;

            return stats;
        }

        /// <summary>
        /// Формирует компактную сводку курсов с недельной динамикой для рассылки.
        /// </summary>
        public async Task<string> GetValuteSummaryMessage(
            string[] charCodesCollection, CancellationToken cancel)
        {
            var dateHeader = DateTime.Now.ToString("d MMMM", RuCulture);
            var result = $"*Курсы ЦБ РФ* {dateHeader}\n\r\n\r";

            foreach (var charCode in charCodesCollection)
            {
                var valutesRoot = await _processingService.RequestProcessing(8, charCode, cancel);
                if (valutesRoot == null) continue;

                var v = valutesRoot.GetValuteModels
                    .Select(item => new Valute
                    {
                        CharCode = item.CharCode,
                        DateValute = item.DateValute,
                        Name = item.Name,
                        Value = item.Value
                    })
                    .GroupBy(e => e.DateValute)
                    .Select(g => g.First())
                    .OrderBy(e => e.DateValute)
                    .ToList();

                if (v.Count < 2)
                {
                    result += $"{charCode}  {v.FirstOrDefault()?.Value:0.00}  нет данных\n\r";
                    continue;
                }

                var stats = CalculateStatistics(v);
                string changeStr;
                if (Math.Abs(stats.AbsoluteChange) < 0.005)
                {
                    changeStr = "без изм.";
                }
                else
                {
                    var sign = stats.AbsoluteChange >= 0 ? "+" : "";
                    changeStr = $"{sign}{stats.AbsoluteChange:0.00} за неделю";
                }

                result += $"{stats.CharCode}  {stats.CurrentValue:0.00}  {changeStr}\n\r";
            }

            return result;
        }
    }
}
