using Microsoft.EntityFrameworkCore;

public static class AlertsModule
{
    public static void AddAlertsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/alerts", async (Guid subscriptionId, AppDbContext dbContext) =>
        {
            var userAlerts = await dbContext.Alerts.Where(a => a.SubscriptionId == subscriptionId).ToListAsync();

            return userAlerts;
        }).RequireRateLimiting("fixed-hard");

        app.MapPost("/alerts", async (CreateAlertDto alert, AppDbContext dbContext, IAlertsActivator alertsActivator, PriceService priceService) =>
        {
            var alertsForSymbol = await dbContext.Alerts
                .Where(a => a.SubscriptionId == alert.SubscriptionId
                    && a.Symbol == alert.Symbol
                    && a.Status == AlertStatus.Active)
                .CountAsync();

            if (alertsForSymbol >= 2)
            {
                return Results.BadRequest($"Maximum number of active alerts (2) reached for symbol {alert.Symbol}.");
            }

            var totalAlerts = await dbContext.Alerts
                .Where(a => a.SubscriptionId == alert.SubscriptionId
                    && a.Status == AlertStatus.Active)
                .CountAsync();

            if (totalAlerts >= 10)
            {
                return Results.BadRequest("Maximum number of total active alerts (10) reached.");
            }

            var symbolPriceInfo = await priceService.GetPriceAsync(alert.Symbol);

            var newAlert = new Alert
            {
                Symbol = alert.Symbol,
                ValueOnCreation = symbolPriceInfo.Price,
                ValueTarget = alert.ValueTarget,
                Trigger = alert.Trigger,
                Status = AlertStatus.Active,
                SubscriptionId = alert.SubscriptionId,
                CreatedAt = symbolPriceInfo.Timestamp,
            };

            dbContext.Alerts.Add(newAlert);
            await dbContext.SaveChangesAsync();

            alertsActivator.Activate([newAlert]);

            return Results.Ok();
        }).RequireRateLimiting("fixed-hard");

        app.MapDelete("/alerts/{id}", async (Guid id, AppDbContext dbContext, IAlertsActivator alertsActivator) =>
        {
            var alert = await dbContext.Alerts.FindAsync(id);

            if (alert == null)
            {
                return Results.NotFound();
            }

            alertsActivator.Deactivate(alert);

            dbContext.Alerts.Remove(alert);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        }).RequireRateLimiting("fixed-soft");
    }
}