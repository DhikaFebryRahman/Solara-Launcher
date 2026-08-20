using System.Threading;
using System.Windows;

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
    }
}
