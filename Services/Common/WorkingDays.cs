namespace Ibtikar.Services.Common
{
    public static class WorkingDays
    {
        private static readonly TimeZoneInfo RiyadhTz = ResolveSaudiArabiaTimeZone();

        public static bool Within(DateTime sentAtUtc, int workingDays, DateTime nowUtc)
        {
            if (workingDays <= 0) return false;

            var sentLocal = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(sentAtUtc), RiyadhTz);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(nowUtc), RiyadhTz);

            var count = 0;
            var cursor = sentLocal.Date;
            while (cursor < nowLocal.Date)
            {
                cursor = cursor.AddDays(1);
                if (IsWorkingDay(cursor)) count++;
                if (count >= workingDays) return true;
            }

            if (cursor == nowLocal.Date && IsWorkingDay(cursor))
            {
                count++;
            }

            return count <= workingDays && count > 0;
        }

        public static bool IsWithinWindow(DateTime sentAtUtc, int workingDays, DateTime nowUtc)
            => Within(sentAtUtc, workingDays, nowUtc);

        public static bool IsWorkingDay(DateTime date)
            => date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Saturday;

        public static int CountFrom(DateTime sentAtUtc, DateTime nowUtc)
        {
            var sentLocal = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(sentAtUtc), RiyadhTz);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(nowUtc), RiyadhTz);

            var count = 0;
            var cursor = sentLocal.Date;
            while (cursor < nowLocal.Date)
            {
                cursor = cursor.AddDays(1);
                if (IsWorkingDay(cursor)) count++;
            }

            if (cursor == nowLocal.Date && IsWorkingDay(cursor))
            {
                count++;
            }

            return count;
        }

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
    }
}