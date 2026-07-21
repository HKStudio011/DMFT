using DMFT.Core.Utilities;
using DMFT.Shared.Services;
using DMFT.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

TargetPlatform.SetCurrentPlatform(TargetPlatform.Platform.Web | TargetPlatform.Platform.WebAssembly);

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IFolderPicker, FolderPicker>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
