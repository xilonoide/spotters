namespace Spotters;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();

        var context = new TrayIconApplicationContext();

        EventHandler? idleHandler = null;
        idleHandler = async (_, _) =>
        {
            Application.Idle -= idleHandler;
            await context.InitializeAsync();
        };

        Application.Idle += idleHandler;

        Application.Run(context);
    }
}
