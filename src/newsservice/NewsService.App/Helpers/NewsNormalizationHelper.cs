using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsService.App.Helpers
{
    public static class NewsNormalizationHelper
    {
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
