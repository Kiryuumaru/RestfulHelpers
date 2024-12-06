using TransactionHelpers;

namespace RestfulHelpers.Test.UnitTest.Tests;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class IntegrationTest1
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RestfulHelpers_Test_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("apiservice");
        await resourceNotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromMinutes(10));

        var weatherforecastResponse = await httpClient.Execute<WeatherForecast[]>(HttpMethod.Get, "/weatherforecast");

        Assert.Equal(HttpStatusCode.OK, weatherforecastResponse.StatusCode);
        Assert.True(weatherforecastResponse.HasValue);
        Assert.True(weatherforecastResponse.IsSuccess);
        Assert.False(weatherforecastResponse.IsError);

        var resultResponse = await httpClient.Execute(HttpMethod.Get, "/result");

        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);
        Assert.True(resultResponse.IsSuccess);
        Assert.False(resultResponse.IsError);

        var resultErrorResponse = await httpClient.Execute(HttpMethod.Get, "/resulterror");

        Assert.Equal(HttpStatusCode.OK, resultErrorResponse.StatusCode);
        Assert.False(resultErrorResponse.IsSuccess);
        Assert.True(resultErrorResponse.IsError);
        Assert.Equal("ERROR_CODE_123", resultErrorResponse.Error.ErrorCode);
        Assert.Equal("THIS IS ERROR", resultErrorResponse.Error.Message);

        var httpResultResponse = await httpClient.Execute(HttpMethod.Get, "/httpresult");

        Assert.Equal(HttpStatusCode.OK, httpResultResponse.StatusCode);
        Assert.True(httpResultResponse.IsSuccess);
        Assert.False(httpResultResponse.IsError);

        var httpResultErrorResponse = await httpClient.Execute(HttpMethod.Get, "/httpresulterror");

        Assert.Equal(HttpStatusCode.OK, httpResultErrorResponse.StatusCode);
        Assert.False(httpResultErrorResponse.IsSuccess);
        Assert.True(httpResultErrorResponse.IsError);
        Assert.Equal("ERROR_CODE_123", httpResultErrorResponse.Error.ErrorCode);
        Assert.Equal("THIS IS ERROR", httpResultErrorResponse.Error.Message);

        var httpResultErrorInternalServerErrorResponse = await httpClient.Execute(HttpMethod.Get, "/httpresulterror_InternalServerError");

        Assert.Equal(HttpStatusCode.InternalServerError, httpResultErrorInternalServerErrorResponse.StatusCode);
        Assert.False(httpResultErrorInternalServerErrorResponse.IsSuccess);
        Assert.True(httpResultErrorInternalServerErrorResponse.IsError);

        var httpResultErrorUnauthorizedResponse = await httpClient.Execute(HttpMethod.Get, "/httpresulterror_Unauthorized");

        Assert.Equal(HttpStatusCode.Unauthorized, httpResultErrorUnauthorizedResponse.StatusCode);
        Assert.False(httpResultErrorUnauthorizedResponse.IsSuccess);
        Assert.True(httpResultErrorUnauthorizedResponse.IsError);
    }
}
