using System.Globalization;

namespace Supla
{
    public static class Helpers
    {
        private static CultureInfo culture = CultureInfo.CreateSpecificCulture("pl-PL");

        public static DateTime UnixTimeToDateTime(this long unixtime)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            date = date.AddSeconds(unixtime).ToLocalTime();
            return date;
        }

        public static DateTime UnixTimeToDateTime(this int unixtime)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            date = date.AddSeconds(unixtime).ToLocalTime();
            return date;
        }

        public static double ToDouble(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (double.TryParse(text, NumberStyles.Number, culture, out double number))
                return number;

            return 0.0;
        }
    }
}
