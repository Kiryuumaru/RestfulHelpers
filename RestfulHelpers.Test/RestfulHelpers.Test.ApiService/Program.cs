using RestfulHelpers.Common;
using System.Net;
using System.Text.Json;
using TransactionHelpers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

WeatherForecast[] randomWeatherForecast()
{
    return Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
}

app.MapGet("/weatherforecast", () =>
{
    var forecast = randomWeatherForecast();
    return forecast;
});

app.MapGet("/result", () =>
{
    Result result = new();
    return result;
});

app.MapGet("/resulterror", () =>
{
    Result result = new();
    result.WithError("THIS IS ERROR", "ERROR_CODE_123");
    return result;
});

app.MapGet("/resultweather", () =>
{
    Result<WeatherForecast[]> result = new();
    var forecast = randomWeatherForecast();
    result.WithValue(forecast);
    return result;
});

app.MapGet("/resultweathererror", () =>
{
    Result<WeatherForecast[]> result = new();
    var forecast = randomWeatherForecast();
    result.WithValue(forecast);
    result.WithError("THIS IS ERROR", "ERROR_CODE_123");
    return result;
});

app.MapGet("/httpresult", () =>
{
    HttpResult result = new();
    return result.GetResponse();
});

app.MapGet("/httpresulterror", () =>
{
    HttpResult result = new();
    result.WithError("THIS IS ERROR", "ERROR_CODE_123");
    return result.GetResponse();
});

app.MapGet("/httpresulterror_InternalServerError", () =>
{
    HttpResult result = new();
    result.WithStatusCode(HttpStatusCode.InternalServerError);
    return result.GetResponse();
});

app.MapGet("/httpresulterror_Unauthorized", () =>
{
    HttpResult result = new();
    result.WithHttpResponseHeader("WWW-Authenticate", "Bearer1", "Bearer2");
    result.WithStatusCode(HttpStatusCode.Unauthorized);
    return result.GetResponse();
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
