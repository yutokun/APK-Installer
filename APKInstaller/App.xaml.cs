using System;
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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            OnExitAction?.Invoke();
        }
    }
}
