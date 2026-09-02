using System.Text.RegularExpressions;

namespace Ibtikar.Services.Helpers
{
    public static class SafeText
    {
        private static readonly Regex StripDangerous = new(
            @"[<>]|[\x00-\x08\x0B\x0C\x0E-\x1F]",
            RegexOptions.Compiled);

        private static readonly Regex StripSchemeTokens = new(
            @"javascript\s*:|vbscript\s*:",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Clean(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            var cleaned = StripDangerous.Replace(value, string.Empty);
            cleaned = StripSchemeTokens.Replace(cleaned, string.Empty);
            return cleaned;
        }
    }
}