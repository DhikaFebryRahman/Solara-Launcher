using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MinecraftLauncher.Config;
using MinecraftLauncher.Core;
using MinecraftLauncher.Models;

namespace MinecraftLauncher
{
    public partial class MainWindow : Window
    {
        private LauncherConfig _config;
        private readonly InstallationStore _installStore = new();
        private long _totalRamMb = 16384;
        private bool _initializing = true;
        private bool _gameRunning;

        public MainWindow()
        {
            InitializeComponent();
            _config = LauncherConfig.Load();
            LoadVersions();
            LoadConfigToUI();
            LoadInstallations();
            _initializing = false;
        }

        private void LoadVersions()
        {
            var versions = new string[]
            {
                "26.2", "26.1.2", "26.1.1", "26.1",
                "1.21.11", "1.21.10", "1.21.9", "1.21.8", "1.21.7", "1.21.6", "1.21.5", "1.21.4", "1.21.3", "1.21.2", "1.21.1", "1.21",
                "1.20.6", "1.20.5", "1.20.4", "1.20.3", "1.20.2", "1.20.1", "1.20",
                "1.19.4", "1.19.3", "1.19.2", "1.19.1", "1.19",
                "1.18.2", "1.18.1", "1.18",
                "1.17.1", "1.17",
                "1.16.5", "1.16.4", "1.16.3", "1.16.2", "1.16.1", "1.16",
                "1.15.2", "1.15.1", "1.15",
                "1.14.4", "1.14.3", "1.14.2", "1.14.1", "1.14",
                "1.13.2", "1.13.1", "1.13",
                "1.12.2", "1.12.1", "1.12",
                "1.11.2", "1.11.1", "1.11",
                "1.10.2", "1.10.1", "1.10",
                "1.9.4", "1.9.3", "1.9.2", "1.9.1", "1.9",
                "1.8.9", "1.8.8", "1.8.7", "1.8.6", "1.8.5", "1.8.4", "1.8.3", "1.8.2", "1.8.1", "1.8",
                "1.7", "1.6", "1.5",
                "1.4", "1.3", "1.2", "1.1",
                "1.0"
            };
            CmbVersion.ItemsSource = versions;
        }

        private void LoadConfigToUI()
        {
            TxtPlayerName.Text = _config.PlayerName;
            CmbVersion.SelectedItem = _config.Version;

            ChkKeepLauncherOpen.IsChecked = _config.KeepLauncherOpen;

            int themeIndex = _config.Theme switch
            {
                "Light" => 1,
                "System" => 2,
                _ => 0
            };
            CmbTheme.SelectedIndex = themeIndex;

            TxtInstallDirectory.Text = _config.GetInstallDirectory();

            _totalRamMb = GetTotalRamMb();
            int maxRamMb = Math.Max(1024, (int)Math.Min(_totalRamMb, int.MaxValue));
            SldRamMb.Minimum = 1024;
            SldRamMb.Maximum = maxRamMb;
            SldRamMb.TickFrequency = 256;

            int ram = Math.Clamp(_config.RamMb, 1024, maxRamMb);
            SldRamMb.Value = ram;
            UpdateRamLabel(ram);
        }

        private void LoadInstallations()
        {
            ListInstallations.ItemsSource = new ObservableCollection<Installation>(_installStore.Load());
        }

        private void RefreshInstallations()
        {
            ListInstallations.ItemsSource = new ObservableCollection<Installation>(_installStore.Load());
        }

        private void SaveConfigFromUI()
        {
            _config.PlayerName = TxtPlayerName.Text.Trim();
            _config.Version = CmbVersion.Text.Trim();
            _config.RamMb = (int)SldRamMb.Value;
            _config.KeepLauncherOpen = ChkKeepLauncherOpen.IsChecked == true;
            _config.InstallDirectory = TxtInstallDirectory.Text.Trim();
            _config.Save();
        }

        private void SidebarMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlayView == null || InstallationsView == null || SettingsView == null) return;

            PlayView.Visibility = Visibility.Collapsed;
            InstallationsView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;

            var selectedIndex = SidebarMenu.SelectedIndex;
            switch (selectedIndex)
            {
                case 0: PlayView.Visibility = Visibility.Visible; break;
                case 1: InstallationsView.Visibility = Visibility.Visible; break;
                case 2: SettingsView.Visibility = Visibility.Visible; break;
            }
        }

        private async void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (_gameRunning) return;

            if (string.IsNullOrWhiteSpace(TxtPlayerName.Text) || string.IsNullOrWhiteSpace(CmbVersion.Text))
            {
                MessageBox.Show("Player Name and Version are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveConfigFromUI();
            await LaunchSelectedVersionAsync();
        }

        private async Task LaunchSelectedVersionAsync()
        {
            if (_gameRunning) return;

            SetBusy(true);
            LblStatus.Text = "Memulai instalasi...";

            try
            {
                string version = _config.Version;
                string gameDir = _config.GetInstallDirectory();

                IProgress<string> progress = new Progress<string>(msg =>
                {
                    Dispatcher.Invoke(() => LblStatus.Text = msg);
                });

                using var cts = new CancellationTokenSource();

                var installResult = await GameInstaller.InstallAsync(version, gameDir, progress, cts.Token);

                _installStore.Upsert(version, gameDir);
                RefreshInstallations();

                progress.Report("Memulai permainan...");
                Process gameProcess = GameLauncher.Launch(installResult, _config.PlayerName, _config.RamMb);
                _gameRunning = true;

                if (_config.KeepLauncherOpen)
                {
                    LblStatus.Text = "Game berjalan. Launcher tetap terbuka.";
                }
                else
                {
                    this.Hide();
                }

                await gameProcess.WaitForExitAsync();

                _gameRunning = false;
                LblStatus.Text = "Ready";
                SetBusy(false);

                if (!this.IsVisible)
                {
                    this.Show();
                    this.Activate();
                    this.Topmost = true;
                    this.Topmost = false;
                }
            }
            catch (Exception ex)
            {
                _gameRunning = false;
                MessageBox.Show($"Gagal memulai permainan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LblStatus.Text = "Error";
                SetBusy(false);

                if (!this.IsVisible)
                {
                    this.Show();
                }
            }
        }

        private void SetBusy(bool busy)
        {
            BtnLaunch.IsEnabled = !busy;
            TxtPlayerName.IsEnabled = !busy;
            CmbVersion.IsEnabled = !busy;
            ProgressBarStatus.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void InstallPlay_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Installation inst) return;

            CmbVersion.SelectedItem = inst.Version;
            _config.Version = inst.Version;
            _config.Save();

            SidebarMenu.SelectedIndex = 0;
            await LaunchSelectedVersionAsync();
        }

        private void InstallDelete_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Installation inst) return;

            _installStore.Remove(inst.Version, inst.Directory);
            RefreshInstallations();
        }

        private void KeepLauncherOpen_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            _config.KeepLauncherOpen = ChkKeepLauncherOpen.IsChecked == true;
            _config.Save();
        }

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || CmbTheme.SelectedIndex < 0) return;

            _config.Theme = CmbTheme.SelectedIndex switch
            {
                1 => "Light",
                2 => "System",
                _ => "Dark"
            };
            _config.Save();
            App.ApplyTheme(_config.Theme);
        }

        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Pilih folder instalasi Minecraft",
                InitialDirectory = _config.GetInstallDirectory()
            };

            if (dialog.ShowDialog(this) == true)
            {
                TxtInstallDirectory.Text = dialog.FolderName;
                _config.InstallDirectory = dialog.FolderName;
                _config.Save();
            }
        }

        private void TxtInstallDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing) return;

            _config.InstallDirectory = TxtInstallDirectory.Text.Trim();
            _config.Save();
        }

        private void SldRamMb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SldRamMb == null) return;

            int ram = (int)SldRamMb.Value;
            UpdateRamLabel(ram);

            if (_initializing) return;

            _config.RamMb = ram;
            _config.Save();
        }

        private void UpdateRamLabel(int ramMb)
        {
            if (LblRamValue == null || LblRamHint == null) return;

            double gb = ramMb / 1024.0;
            double totalGb = _totalRamMb / 1024.0;
            
            string gbText = gb == Math.Floor(gb) ? $"{gb:0} GB" : $"{gb:0.#} GB";
            string totalText = totalGb == Math.Floor(totalGb) ? $"{totalGb:0} GB" : $"{totalGb:0.#} GB";
            
            LblRamValue.Text = $"{gbText} / {totalText}";
            LblRamHint.Text = $"Alokasi saat ini: {ramMb} MB dari {_totalRamMb} MB";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        private static long GetTotalRamMb()
        {
            var buffer = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (GlobalMemoryStatusEx(ref buffer))
            {
                return (long)(buffer.ullTotalPhys / (1024 * 1024));
            }
            return 16384;
        }
    }
}