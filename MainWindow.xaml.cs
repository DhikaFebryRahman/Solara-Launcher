using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MinecraftLauncher.Config;
using MinecraftLauncher.Core;

namespace MinecraftLauncher
{
    public partial class MainWindow : Window
    {
        private LauncherConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            _config = LauncherConfig.Load();
            LoadConfigToUI();
        }

        private void LoadConfigToUI()
        {
            TxtPlayerName.Text = _config.PlayerName;
            TxtVersion.Text = _config.Version;
            TxtRamMb.Text = _config.RamMb.ToString();
        }

        private void SaveConfigFromUI()
        {
            _config.PlayerName = TxtPlayerName.Text.Trim();
            _config.Version = TxtVersion.Text.Trim();
            if (int.TryParse(TxtRamMb.Text.Trim(), out int ram))
            {
                _config.RamMb = ram;
            }
            _config.Save();
        }

        private void SidebarMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlayView == null || InstallationsView == null || SkinsView == null || SettingsView == null) return;
            
            PlayView.Visibility = Visibility.Collapsed;
            InstallationsView.Visibility = Visibility.Collapsed;
            SkinsView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;

            var selectedIndex = SidebarMenu.SelectedIndex;
            switch (selectedIndex)
            {
                case 0: PlayView.Visibility = Visibility.Visible; break;
                case 1: InstallationsView.Visibility = Visibility.Visible; break;
                case 2: SkinsView.Visibility = Visibility.Visible; break;
                case 3: SettingsView.Visibility = Visibility.Visible; break;
            }
        }

        private async void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPlayerName.Text) || string.IsNullOrWhiteSpace(TxtVersion.Text))
            {
                MessageBox.Show("Player Name and Version are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveConfigFromUI();
            
            BtnLaunch.IsEnabled = false;
            TxtPlayerName.IsEnabled = false;
            TxtVersion.IsEnabled = false;
            TxtRamMb.IsEnabled = false;
            ProgressBarStatus.Visibility = Visibility.Visible;

            try
            {
                IProgress<string> progress = new Progress<string>(msg =>
                {
                    Dispatcher.Invoke(() => LblStatus.Text = msg);
                });

                using var cts = new CancellationTokenSource();
                
                var installResult = await GameInstaller.InstallAsync(_config.Version, progress, cts.Token);
                
                progress.Report("Memulai permainan...");
                GameLauncher.Launch(installResult, _config.PlayerName, _config.RamMb);
                
                this.Close(); // Tutup launcher setelah game berjalan
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memulai permainan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LblStatus.Text = "Error";
            }
            finally
            {
                BtnLaunch.IsEnabled = true;
                TxtPlayerName.IsEnabled = true;
                TxtVersion.IsEnabled = true;
                TxtRamMb.IsEnabled = true;
                ProgressBarStatus.Visibility = Visibility.Collapsed;
            }
        }
    }
}
