using Cashflow.Launch.Application.DTOs;
using Cashflow.Launch.Application.Ports;
using Cashflow.Launch.Application.UseCases;
using Cashflow.Launch.Infrastructure.Messaging;
using Cashflow.Launch.Infrastructure.Persistence;
using Cashflow.Launch.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddScoped<CreateTransactionUseCase>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddSingleton<IMessageBrokerPublisher, RabbitMqPublisher>();

    builder.Services.AddDbContext<LaunchDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=launch.db"));

    var app = builder.Build();
        
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LaunchDbContext>();
        dbContext.Database.Migrate();
    }

    //LIBERADO PARA TODOS (POC)
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.MapPost("/api/v1/transactions", async (
        [FromBody] CreateTransactionRequest request,
        [FromServices] CreateTransactionUseCase useCase,
        [FromServices] ILogger<Program> apiLogger) =>
    {
        apiLogger.LogInformation("Recebendo nova requisição de lançamento: {Amount} {Type}", request.Amount, request.Type);

        try
        {
            await useCase.ExecuteAsync(request);
            apiLogger.LogInformation("Lançamento processado com sucesso.");
            return Results.StatusCode(201); // Created
        }
        catch (ArgumentException ex)
        {
            apiLogger.LogWarning("Requisição inválida: {ErrorMessage}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            apiLogger.LogError(ex, "Erro inesperado ao processar o lançamento.");
            return Results.Problem("An internal server error has occurred.", statusCode: 500);
        }
    })
    .WithName("CreateTransaction")
    .WithSummary("Creates a new financial transaction.")
    .Produces(201)
    .Produces<object>(400)
    .Produces<object>(500);


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