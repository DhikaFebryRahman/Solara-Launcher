using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MinecraftLauncher.Config;
using MinecraftLauncher.Core;

namespace MinecraftLauncher
{
    public partial class MainForm : Form
    {
        private LauncherConfig _config;
        private TextBox _txtName = null!;
        private TextBox _txtVersion = null!;
        private NumericUpDown _numRam = null!;
        private Button _btnLaunch = null!;
        private Label _lblStatus = null!;

        public MainForm()
        {
            InitializeComponent();
            _config = LauncherConfig.Load();
            LoadConfigToUI();
        }

        private void InitializeComponent()
        {
            this.Text = "Minecraft Launcher";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblName = new Label { Text = "Player Name:", Location = new Point(20, 20), AutoSize = true };
            _txtName = new TextBox { Location = new Point(120, 20), Width = 230 };

            var lblVersion = new Label { Text = "Version:", Location = new Point(20, 60), AutoSize = true };
            _txtVersion = new TextBox { Location = new Point(120, 60), Width = 230 };

            var lblRam = new Label { Text = "RAM (MB):", Location = new Point(20, 100), AutoSize = true };
            _numRam = new NumericUpDown { Location = new Point(120, 100), Width = 230, Minimum = 512, Maximum = 32768, Increment = 512 };

            _btnLaunch = new Button { Text = "Launch", Location = new Point(120, 140), Width = 100, Height = 30 };
            _btnLaunch.Click += async (s, e) => await OnLaunchClick();

            _lblStatus = new Label { Text = "Ready", Location = new Point(20, 200), Width = 340, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };

            this.Controls.Add(lblName);
            this.Controls.Add(_txtName);
            this.Controls.Add(lblVersion);
            this.Controls.Add(_txtVersion);
            this.Controls.Add(lblRam);
            this.Controls.Add(_numRam);
            this.Controls.Add(_btnLaunch);
            this.Controls.Add(_lblStatus);
        }

        private void LoadConfigToUI()
        {
            _txtName.Text = _config.PlayerName;
            _txtVersion.Text = _config.Version;
            _numRam.Value = _config.RamMb;
        }

        private void SaveConfigFromUI()
        {
            _config.PlayerName = _txtName.Text.Trim();
            _config.Version = _txtVersion.Text.Trim();
            _config.RamMb = (int)_numRam.Value;
            _config.Save();
        }

        private async Task OnLaunchClick()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtVersion.Text))
            {
                MessageBox.Show("Player Name and Version are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveConfigFromUI();
            
            _btnLaunch.Enabled = false;
            _txtName.Enabled = false;
            _txtVersion.Enabled = false;
            _numRam.Enabled = false;

            try
            {
                IProgress<string> progress = new Progress<string>(msg =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => _lblStatus.Text = msg));
                    }
                    else
                    {
                        _lblStatus.Text = msg;
                    }
                });

                using var cts = new CancellationTokenSource();
                
                var installResult = await GameInstaller.InstallAsync(_config.Version, progress, cts.Token);
                
                progress.Report("Memulai permainan...");
                GameLauncher.Launch(installResult, _config.PlayerName, _config.RamMb);
                
                this.Close(); // Tutup launcher setelah game berjalan
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memulai permainan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "Error";
            }
            finally
            {
                _btnLaunch.Enabled = true;
                _txtName.Enabled = true;
                _txtVersion.Enabled = true;
                _numRam.Enabled = true;
            }
        }
    }
}
