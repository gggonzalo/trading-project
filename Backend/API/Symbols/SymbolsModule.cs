using Binance.Net.Interfaces.Clients;
using Microsoft.EntityFrameworkCore;

public static class SymbolsModule
{
    public static void AddSymbolsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/symbols/{symbol}", async (string symbol, SymbolsService symbolsService) =>
        {
            var symbolInfo = await symbolsService.GetSymbolInfoAsync(symbol);

            if (symbolInfo == null) return Results.NotFound();

            return Results.Ok(symbolInfo);
        });
    }
}