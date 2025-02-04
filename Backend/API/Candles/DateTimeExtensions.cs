
public static class DateTimeExtensions
{
    static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static double ToUnixEpoch(this DateTime date)
    {
        TimeSpan duration = date.Subtract(UnixEpoch);
        return duration.TotalSeconds;
    }
}
