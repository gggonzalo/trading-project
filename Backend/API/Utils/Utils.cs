

public class Utils
{
    static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


    public static double ConvertDateToUnixExpoch(DateTime date)
    {
        TimeSpan duration = date.Subtract(UnixEpoch);
        return duration.TotalSeconds;
    }
}