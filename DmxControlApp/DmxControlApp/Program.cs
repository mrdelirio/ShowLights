using DmxControlApp.Client.Pages;
using DmxControlApp.Components;
using DmxControlApp.Services;
using Microsoft.AspNetCore.Mvc;
using DmxControlApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<IArtnetService, ArtnetService>();
builder.Services.AddSingleton<IAiPresetService, AiPresetService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(DmxControlApp.Client._Imports).Assembly);

// Minimal API endpoint for sending Art-Net DMX frames
app.MapPost("/api/artnet/send", async ([FromBody] DmxFrame frame, IArtnetService artnet, CancellationToken ct) =>
{
    if (frame.Values is null) return Results.BadRequest("Missing values");
    await artnet.SendDmxAsync(frame.Universe, frame.Values, frame.TargetIp, ct);
    return Results.Ok();
});

app.MapPost("/api/ai/preset", async ([FromBody] AiPresetRequest req, IAiPresetService ai, CancellationToken ct) =>
{
    var values = await ai.GeneratePresetAsync(req.Prompt, req.Channels, ct);
    return Results.Ok(new AiPresetResponse(values));
});

// Bluetooth stub endpoints
app.MapGet("/api/bluetooth/list", () => Results.Ok(Array.Empty<object>()));
app.MapPost("/api/bluetooth/connect", () => Results.Ok());

app.Run();
