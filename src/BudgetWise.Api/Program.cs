using BudgetWise.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Pipeline HTTP ──────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCorsPolicy();

app.Run();
