using System;
using System.IO;
using System.Windows;

namespace APKInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static event Action OnExitAction;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length >= 1) Properties.Add("apks", e.Args);
            InitializeUpdater();
        }

        static async void InitializeUpdater()
        {
            Directory.SetCurrentDirectory(Resource.TempDirectory);
            await Resource.ExtractTo("WinSparkle.dll", Directory.GetCurrentDirectory());
            WinSparkle.win_sparkle_set_appcast_url("https://appcast.yutokun.com/apk-installer/appcast.xml");
            WinSparkle.win_sparkle_init();
            OnExitAction += WinSparkle.win_sparkle_cleanup;
            WinSparkle.win_sparkle_check_update_with_ui();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            OnExitAction?.Invoke();
        }
    }
}
