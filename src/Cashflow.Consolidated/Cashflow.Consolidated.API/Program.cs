using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Application.UseCases;
using Cashflow.Consolidated.Domain;
using Cashflow.Consolidated.Infrastructure.Messaging;
using Cashflow.Consolidated.Infrastructure.Persistence;
using Cashflow.Consolidated.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddScoped<GetDailyBalanceUseCase>();
    builder.Services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();

    builder.Services.AddDbContext<ConsolidatedDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHostedService<TransactionConsumer>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidatedDbContext>();
        dbContext.Database.Migrate();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapGet("/api/v1/reports/consolidated/{date:datetime}", async (
        DateTime date,
        [FromServices] GetDailyBalanceUseCase useCase,
        [FromServices] ILogger<Program> apiLogger) =>
    {
        apiLogger.LogInformation("Relatório para a data {Date} solicitado.", date.ToShortDateString());
        var result = await useCase.ExecuteAsync(date);
        return Results.Ok(result);
    })
    .WithName("GetConsolidatedDailyReport")
    .WithSummary("Gets the consolidated daily financial report.")
    .Produces<DailyBalance>()
    .Produces(404);

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Programa interrompido por uma exceção.");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}