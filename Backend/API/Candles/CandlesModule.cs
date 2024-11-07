using Binance.Net.Enums;

public static class CandlesModule
{
    public static void AddCandlesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/candles", async (string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int? limit, CandlesService candlesService) =>
        {
            return await candlesService.GetCandlesAsync(symbol, interval, startTime, endTime, limit);
        }).RequireRateLimiting("fixed-medium");

        app.MapHub<CandlesHub>("/candles-hub").RequireRateLimiting("fixed-medium");
    }
}