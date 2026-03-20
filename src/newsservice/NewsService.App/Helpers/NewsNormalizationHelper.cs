using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsService.App.Helpers
{
    public static class NewsNormalizationHelper
    {
        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "в", "на", "по", "от", "и", "с", "что", "как", "для", "не", "это", "до",
            "за", "из", "к", "о", "а", "но", "да", "же", "ли", "бы", "был", "быть",
            "его", "её", "их", "мы", "вы", "он", "она", "они", "то", "все", "при"
        };

        /// <summary>
        /// Нормализует заголовок для вычисления схожести: приводит к нижнему регистру,
        /// убирает пунктуацию и стоп-слова.
        /// </summary>
        public static string NormalizeTitleForSimilarity(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            var lower = title.ToLowerInvariant();
            var noPunct = Regex.Replace(lower, @"[^\p{L}\p{N}\s]", " ");
            var words = noPunct.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !StopWords.Contains(w));
            return string.Join(" ", words);
        }

        /// <summary>
        /// Возвращает множество символьных n-грамм из текста.
        /// </summary>
        public static HashSet<string> GetCharNGrams(string text, int n = 3)
        {
            var grams = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(text) || text.Length < n)
                return grams;

            for (int i = 0; i <= text.Length - n; i++)
            {
                grams.Add(text.Substring(i, n));
            }
            return grams;
        }

        /// <summary>
        /// Вычисляет коэффициент Жаккара между двумя множествами n-грамм.
        /// </summary>
        public static double JaccardSimilarity(HashSet<string> set1, HashSet<string> set2)
        {
            if (set1.Count == 0 || set2.Count == 0)
                return 0.0;

            var intersection = set1.Count(g => set2.Contains(g));
            var union = set1.Count + set2.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        public static string NormalizeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            title = title.Trim();
            title = Regex.Replace(title, @"\s+", " ");
            return title;
        }

        public static string ComputeHash(string title, string url)
        {
            var input = $"{NormalizeTitle(title)}|{url?.Trim() ?? ""}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var text = Regex.Replace(html, @"<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        public static string TruncateText(string text, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text.Substring(0, maxLength) + "...";
        }
    }
}
