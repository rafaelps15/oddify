using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Oddify.Api.Extensions;
using Oddify.Api.Middleware;
using Oddify.Common.Application;
using Oddify.Common.Infrastructure;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Serialization;
using Oddify.Modules.Analise.Infrastructure;
using Oddify.Modules.Apostas.Infrastructure;
using Oddify.Modules.Fixtures.Infrastructure;
using Oddify.Modules.Users.Infrastructure;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
});

builder.Services.AddApplication([
    Oddify.Modules.Fixtures.Application.AssemblyReference.Assembly,
    Oddify.Modules.Analise.Application.AssemblyReference.Assembly,
    Oddify.Modules.Apostas.Application.AssemblyReference.Assembly,
    Oddify.Modules.Users.Application.AssemblyReference.Assembly]);

string databaseConnectionString = builder.Configuration.GetConnectionString("Database")!;
string redisConnectionString = builder.Configuration.GetConnectionString("Cache")!;

// Só entram aqui os módulos que publicam integration event pra fora de si mesmos (cross-module
// ou entrega garantida) — Apostas hoje só consome, não publica, então não precisa de outbox.
OutboxModule[] outboxModules =
[
    new OutboxModule("users", Oddify.Modules.Users.IntegrationEvents.AssemblyReference.Assembly),
    new OutboxModule("fixtures", Oddify.Modules.Fixtures.IntegrationEvents.AssemblyReference.Assembly),
    new OutboxModule("analise", Oddify.Modules.Analise.IntegrationEvents.AssemblyReference.Assembly)
];

builder.Services.AddInfrastructure(
    builder.Configuration,
    [ApostasModule.ConfigureConsumers, UsersModule.ConfigureConsumers],
    outboxModules,
    databaseConnectionString,
    redisConnectionString);

builder.Configuration.AddModuleConfiguration(["fixtures", "analise", "apostas", "users"]);

builder.Services.AddHealthChecks()
    .AddNpgSql(databaseConnectionString)
    .AddRedis(redisConnectionString);

builder.Services.AddFixturesModule(builder.Configuration);
builder.Services.AddAnaliseModule(builder.Configuration);
builder.Services.AddApostasModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.Run();

// Público e parcial para que Oddify.IntegrationTests use WebApplicationFactory<Program>.
#pragma warning disable CA1515
public partial class Program;
#pragma warning restore CA1515
