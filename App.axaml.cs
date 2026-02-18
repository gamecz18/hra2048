using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins; // Dùležité pro BindingPlugins
using Avalonia.Markup.Xaml;
using System.Linq; // Dùležité pro .OfType<>()
using hra2048.ViewModels;
using hra2048.Views;

namespace hra2048
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Pokud vám tento øádek podtrhává chybu "DisableAvalonia...", 
                // mùžete ho i s metodou dole smazat. Slouží jen k odstranìní duplicitních validací.
                DisableAvaloniaDataAnnotationValidation();

                // Zde vytváøíme hlavní okno
                desktop.MainWindow = new MainWindow
                {
                    // Zde propojujeme okno s ViewModelem
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Pomocná metoda pro validace (pokud zlobí, lze celou metodu smazat)
        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Pokud vám IDE hlásí chybu u "BindingPlugins", zkontrolujte, 
            // zda máte nahoøe: using Avalonia.Data.Core.Plugins;
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}