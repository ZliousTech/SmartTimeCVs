using System.Globalization;

namespace SmartTimeCVs.Web.Extensions
{
    public static class StringExtestions
    {
        public static DateTime ToSqlDateTime(this string str)
        {
            return DateTime.ParseExact(str, "dddd dd MMMM yyyy - HH:mm", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseToDate(this string dateString)
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-dd", null);
        }
    }
}
