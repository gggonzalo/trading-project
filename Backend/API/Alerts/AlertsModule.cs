using Microsoft.EntityFrameworkCore;

public static class AlertsModule
{
    public static void AddAlertsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/alerts", async (Guid subscriptionId, AppDbContext dbContext) =>
        {
            var userAlerts = await dbContext.Alerts.Where(a => a.SubscriptionId == subscriptionId).ToListAsync();

            return userAlerts;
        });

        app.MapPost("/alerts", async (CreateAlertDto alert, AppDbContext dbContext, PriceService priceService) =>
        {
            var symbolPriceInfo = await priceService.GetPriceAsync(alert.Symbol);

            var newAlert = new Alert
            {
                Symbol = alert.Symbol,
                ValueOnCreation = symbolPriceInfo.Price,
                ValueTarget = alert.ValueTarget,
                SubscriptionId = alert.SubscriptionId,
                CreatedAt = symbolPriceInfo.Timestamp,
            };

            dbContext.Alerts.Add(newAlert);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
    }
}