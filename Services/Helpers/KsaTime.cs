using System.Globalization;

namespace Ibtikar.Services.Helpers
{
    public static class KsaTime
    {
        private static readonly TimeZoneInfo RiyadhTz = ResolveSaudiArabiaTimeZone();
        private static readonly CultureInfo ArGregorian = BuildArabicGregorianCulture();

        public static DateTime ToKsa(this DateTime utc)
        {
            var safeUtc = EnsureUtc(utc);
            return TimeZoneInfo.ConvertTimeFromUtc(safeUtc, RiyadhTz);
        }

        public static DateTime? ToKsa(this DateTime? utc)
            => utc.HasValue ? utc.Value.ToKsa() : (DateTime?)null;

        public static string FormatDateTime(this DateTime utc)
            => utc.ToKsa().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public static string FormatDateTime(this DateTime? utc)
            => utc.HasValue ? utc.Value.FormatDateTime() : "—";

        public static string FormatDate(this DateTime utc)
            => utc.ToKsa().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static string FormatDate(this DateTime? utc)
            => utc.HasValue ? utc.Value.FormatDate() : "—";

        public static string FormatArabicDateTime(this DateTime utc)
            => utc.ToKsa().ToString("dddd d MMMM yyyy - HH:mm", ArGregorian);

        public static string FormatArabicDateTime(this DateTime? utc)
            => utc.HasValue ? utc.Value.FormatArabicDateTime() : "—";

        private static DateTime EnsureUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

        private static TimeZoneInfo ResolveSaudiArabiaTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        private static CultureInfo BuildArabicGregorianCulture()
        {
            var c = new CultureInfo("ar-SA");
            var gr = new GregorianCalendar();
            var info = c.DateTimeFormat.Clone() as DateTimeFormatInfo;
            info!.Calendar = gr;
            c.DateTimeFormat = info;
            c.NumberFormat.DigitSubstitution = DigitShapes.NativeNational;
            return c;
        }
    }
}
