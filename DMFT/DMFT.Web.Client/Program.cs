using DMFT.Shared.Services;
using DMFT.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IFolderPicker, FolderPicker>();

await builder.Build().RunAsync();
