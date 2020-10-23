using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace APKInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length >= 1) Properties.Add("apks", e.Args);
            CreateADB();
        }

        void CreateADB()
        {
            var resourceUri = new Uri("/adb.exe", UriKind.Relative);
            var adbStream = GetResourceStream(resourceUri);
            var adbPath = Path.GetTempFileName().Replace(".tmp", ".exe");
            var adbBinary = new byte[adbStream.Stream.Length];
            adbStream.Stream.Read(adbBinary, 0, (int)adbStream.Stream.Length);

            using (var fs = new FileStream(adbPath, FileMode.Create))
            {
                fs.Write(adbBinary, 0, adbBinary.Length);
                Debug.WriteLine(adbPath);
                Properties.Add("adb", adbPath);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            RemoveADB();
        }

        void RemoveADB()
        {
            File.Delete(Properties["adb"].ToString());
        }
    }
}
