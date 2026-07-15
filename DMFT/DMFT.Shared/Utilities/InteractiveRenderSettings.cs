using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DMFT.Shared.Utilities;

public static class InteractiveRenderSettings
{
    public static IComponentRenderMode? InteractiveServer { get; private set; }
        = RenderMode.InteractiveServer;

    public static IComponentRenderMode? InteractiveWebAssembly { get; private set; }
        = RenderMode.InteractiveWebAssembly;

    public static IComponentRenderMode? InteractiveAuto { get; private set; }
        = RenderMode.InteractiveAuto;

    public static void ConfigureBlazorHybridRenderModes()
    {
        InteractiveServer = null;
        InteractiveWebAssembly = null;
        InteractiveAuto = null;
    }
}