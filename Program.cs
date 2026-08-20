using System;
using System.Threading;
using System.Windows.Forms;

namespace MinecraftLauncher
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = "MinecraftLauncher-8F2C1E7A-9B3D-4C6E-8A1F-2D5E7B9C0A3F";

        [STAThread]
        private static void Main()
        {
            using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);

            if (!isFirstInstance)
            {
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
