using System.Text.Json.Serialization;
using BudgetWise.Api.Converters;
using BudgetWise.Api.Endpoints;
using BudgetWise.Api.Extensions;
using BudgetWise.Application;
using BudgetWise.Infrastructure;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// ── JSON ───────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new NullableDateOnlyConverter());
});

// ── Logging ────────────────────────────────────────────────────────────────
builder.AddSerilogConfiguration();

// ── Infraestrutura ─────────────────────────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddIdentityServices();
builder.Services.AddRepositories();

// ── Autenticação e Autorização ─────────────────────────────────────────────
builder.Services.AddApiAuthentication(builder.Configuration);

// ── Application ────────────────────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddUseCases();
builder.Services.AddValidators();

// ── Mensageria (Domain Events) ──────────────────────────────────────────────
// ExtensionDiscovery.ManualOnly disables Wolverine from scanning every DLL in
// the bin folder looking for IWolverineExtension implementations.
// DisableConventionalDiscovery() disables automatic handler scanning — we only
// include the assemblies that actually contain handlers.
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeAssembly(typeof(ApplicationAssemblyMarker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(InfrastructureAssemblyMarker).Assembly);
}, ExtensionDiscovery.ManualOnly);

// ── API ────────────────────────────────────────────────────────────────────
builder.Services.AddCorsPolicy(builder.Environment);
builder.Services.AddDocumentation();
builder.Services.AddGlobalErrorHandler();
builder.Services.AddHealthMonitoring(builder.Configuration);

// ── Pipeline HTTP ──────────────────────────────────────────────────────────
var app = builder.Build();

app.UseGlobalErrorHandler();
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthMonitoring();
app.UseStartupLog();

// ── Endpoints ──────────────────────────────────────────────────────────────
app.MapAllEndpoints();
app.UseDocumentation();

// ── Inicialização do banco ──────────────────────────────────────────────────
await app.InitialiseDatabaseAsync();

app.Run();

