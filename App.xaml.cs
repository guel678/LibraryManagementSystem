using System.Globalization;
using System.IO;
using System.Windows;
using LibraryManagementSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LibraryManagementSystem",
                "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {args.Exception}\n\n");
            MessageBox.Show(args.Exception.Message, "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        Services = new ServiceCollection()
            .AddSingleton<DataStore>()
            .BuildServiceProvider();

        var culture = new CultureInfo("en-PH");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        base.OnStartup(e);
    }
}
