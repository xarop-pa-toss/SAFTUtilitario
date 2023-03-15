using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using SAFTUtilitario.CustomControls;

namespace SAFTUtilitario;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("StdEntry", (handler, view) =>
        {
            if (view is StandardEntry)
            {
#if WINDOWS
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            }
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}