namespace SmartTimeCVs.Web.Extensions
{
    public static class DateExtesions
    {
        public static string ToViewDate(this DateTime date)
        {
            return date.ToString("dddd dd MMMM yyyy - HH:mm");
        }

        public static string ToTableDate(this DateTime date)
        {
            return date.ToString("dd MMM yyyy - hh:mm:ss tt");
        }

        public static string ToDateOfBirth(this DateTime date)
        {
            return date.ToString("dd MMM yyyy");
        }

        public static string ToStringDate(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}
