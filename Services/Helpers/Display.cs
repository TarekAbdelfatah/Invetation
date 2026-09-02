namespace Ibtikar.Services.Helpers
{
    public static class Display
    {
        public const int TitleMax = 50;

        /// <summary>
        /// Returns a shortened version of a long title for compact grid cells.
        /// Keeps the full text available to callers for tooltips.
        /// </summary>
        public static string Title(string? title)
        {
            if (string.IsNullOrEmpty(title)) return string.Empty;
            return title.Length > TitleMax ? title.Substring(0, TitleMax) + "…" : title;
        }
    }
}
