using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DmxControlApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ILocalStorage, LocalStorage>();

await builder.Build().RunAsync();
