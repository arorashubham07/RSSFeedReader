using System.Text.Json;
using Microsoft.Net.Http.Headers;
using RSSFeedReader.Api.Models;
using RSSFeedReader.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SubscriptionStore>();

builder.Services.AddCors(options => options.AddPolicy("LocalFrontend", policy => policy
    .WithOrigins("http://localhost:5213")
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type")));

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    return Task.CompletedTask;
}));
app.UseCors("LocalFrontend");

app.MapGet("/api/subscriptions", (HttpContext context, SubscriptionStore store) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Json(store.Snapshot());
});

app.MapPost("/api/subscriptions", async (HttpRequest request, SubscriptionStore store) =>
{
    if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
        || !string.Equals(contentType.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase))
    {
        return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }

    AddSubscriptionRequest? submission;
    try
    {
        submission = await request.ReadFromJsonAsync<AddSubscriptionRequest>(request.HttpContext.RequestAborted);
    }
    catch (JsonException)
    {
        return Results.BadRequest();
    }
    catch (BadHttpRequestException)
    {
        return Results.BadRequest();
    }

    return store.Add(submission?.Url) ? Results.NoContent() : Results.BadRequest();
});

app.Run();
