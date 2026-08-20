using System;
using System.Linq;
using System.Threading;
using System.Windows;
using MaterialDesignThemes.Wpf;
using MinecraftLauncher.Config;

namespace MinecraftLauncher
{
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = "MinecraftLauncher-8F2C1E7A-9B3D-4C6E-8A1F-2D5E7B9C0A3F";
        private Mutex _mutex = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                _mutex = new Mutex(true, SingleInstanceMutexName, out bool isFirstInstance);

                if (!isFirstInstance)
                {
                    MessageBox.Show("Aplikasi sudah berjalan di latar belakang! Menutup instansi ini...", "Minecraft Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Current.Shutdown();
                    return;
                }

                var config = LauncherConfig.Load();
                ApplyTheme(config.Theme);

                base.OnStartup(e);

                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan fatal saat startup:\n\n{ex.Message}\n\n{ex.StackTrace}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private static ResourceDictionary? _themeDictionary;

        public static void ApplyTheme(string theme)
        {
            bool isDark = theme switch
            {
                "Light" => false,
                "Dark" => true,
                _ => SystemThemeIsDark()
            };

            var bundle = (BundledTheme)Current.Resources.MergedDictionaries.OfType<BundledTheme>().First();
            bundle.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;

            if (_themeDictionary != null)
            {
                Current.Resources.MergedDictionaries.Remove(_themeDictionary);
            }

            _themeDictionary = new ResourceDictionary
            {
                Source = new Uri(isDark ? "Resources/Themes/Dark.xaml" : "Resources/Themes/Light.xaml", UriKind.Relative)
            };
            Current.Resources.MergedDictionaries.Add(_themeDictionary);
        }

        private static bool SystemThemeIsDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}