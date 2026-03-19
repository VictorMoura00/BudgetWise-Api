using BudgetWise.Api.Endpoints;
using BudgetWise.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────
builder.AddSerilogConfiguration();

// ── Infraestrutura ─────────────────────────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddRepositories();

// ── Autenticação e Autorização ─────────────────────────────────────────────
builder.Services.AddApiAuthentication(builder.Configuration);

// ── Application ────────────────────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddUseCases();

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
app.UseDocumentation();
app.UseHealthMonitoring();
app.UseStartupLog();

// ── Endpoints ──────────────────────────────────────────────────────────────
app.MapAuthEndpoints();

// ── Inicialização do banco ──────────────────────────────────────────────────
await app.InitialiseDatabaseAsync();

app.Run();

public partial class Program { }