using DMFT.Core.Services;
using DMFT.Core.Utilities;
using DMFT.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

TargetPlatform.SetCurrentPlatform(TargetPlatform.Platform.Web | TargetPlatform.Platform.WebAssembly);

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
