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

        app.MapPost("/alerts", async (CreateAlertDto createAlertDto, AppDbContext dbContext, IPushNotificationsService pushNotificationsService, IAlertsActivator alertsActivator, PriceService priceService) =>
        {
            var isSubscriptionValid = await pushNotificationsService.IsSubscriptionValidAsync(createAlertDto.SubscriptionId);

            if (!isSubscriptionValid)
            {
                return Results.BadRequest("Invalid subscription.");
            }

            var alertsForSymbol = await dbContext.Alerts
                .Where(a => a.SubscriptionId == createAlertDto.SubscriptionId
                    && a.Symbol == createAlertDto.Symbol
                    && a.Status == AlertStatus.Active)
                .CountAsync();

            if (alertsForSymbol >= 2)
            {
                return Results.BadRequest($"Maximum number of active alerts (2) reached for symbol {createAlertDto.Symbol}.");
            }

            var totalAlerts = await dbContext.Alerts
                .Where(a => a.SubscriptionId == createAlertDto.SubscriptionId
                    && a.Status == AlertStatus.Active)
                .CountAsync();

            if (totalAlerts >= 10)
            {
                return Results.BadRequest("Maximum number of total active alerts (10) reached.");
            }

            var symbolPriceInfo = await priceService.GetPriceAsync(createAlertDto.Symbol);

            var newAlert = new Alert
            {
                Symbol = createAlertDto.Symbol,
                ValueOnCreation = symbolPriceInfo.Price,
                ValueTarget = createAlertDto.ValueTarget,
                Status = AlertStatus.Active,
                SubscriptionId = createAlertDto.SubscriptionId,
                CreatedAt = symbolPriceInfo.Timestamp,
            };

            dbContext.Alerts.Add(newAlert);
            await dbContext.SaveChangesAsync();

            alertsActivator.Activate([newAlert]);

            return Results.Ok();
        });

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
        });
    }
}