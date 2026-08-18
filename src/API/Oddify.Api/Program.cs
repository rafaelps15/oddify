using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Oddify.Api.Extensions;
using Oddify.Api.Middleware;
using Oddify.Common.Application;
using Oddify.Common.Infrastructure;
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

// Cada módulo registra seu próprio Outbox/Inbox job (AddOutboxProcessor/AddInboxProcessor, chamados
// de dentro do respectivo AddXModule mais abaixo) — nada é montado aqui, AddInfrastructure só cuida
// do bootstrapping compartilhado (Quartz, cleanup, etc).
builder.Services.AddInfrastructure(
    builder.Configuration,
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

// Assina os consumers genéricos de cada módulo consumidor no bus in-memory — precisa acontecer
// depois de builder.Build(), quando o IEventBus já existe no container (espelha
// EventsBusStartup.Initialize do projeto de referência, chamado depois do container montado).
ApostasModule.Initialize(app.Services);
UsersModule.Initialize(app.Services);

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
