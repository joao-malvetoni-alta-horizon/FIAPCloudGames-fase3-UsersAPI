using System.Text.Json.Serialization;
using FCG.API.Configuration;
using FCG.API.Endpoints;
using FCG.API.Middlewares;
using FCG.Application.DependencyInjection;
using FCG.Infrastructure.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogConfig();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSwaggerConfig();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseSwaggerConfig();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapApiEndpoints();

await app.MigrateAndSeedAsync();

app.Run();

// Exposto para testes de integração (WebApplicationFactory<Program>).
public partial class Program;
