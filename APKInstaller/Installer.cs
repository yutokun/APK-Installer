using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public class Installer
    {
        string pathToADB;
        readonly MainWindow mainWindow;

        public static void Initialize()
        {
            new Installer();
        }

        Installer()
        {
            CreateADB();
            mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.OnFileDropped += BatchInstall;
        }

        ~Installer()
        {
            File.Delete(pathToADB);
        }

        void CreateADB()
        {
            var resourceUri = new Uri("/adb.exe", UriKind.Relative);
            var adbStream = Application.GetResourceStream(resourceUri);
            var adbPath = Path.GetTempFileName().Replace(".tmp", ".exe");
            var adbBinary = new byte[adbStream.Stream.Length];
            adbStream.Stream.Read(adbBinary, 0, (int)adbStream.Stream.Length);

            using (var fs = new FileStream(adbPath, FileMode.Create))
            {
                fs.Write(adbBinary, 0, adbBinary.Length);
                Debug.WriteLine(adbPath);
                pathToADB = adbPath;
            }
        }

        async void BatchInstall(string[] files)
        {
            var apks = files.Where(f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

            if (apks.Length == 0)
            {
                AddMessage("APK がドロップされませんでした。\n");
                return;
            }

            var text = "インストールする APK：";
            foreach (var apk in apks)
            {
                text = $"{text}\n{apk}";
            }

            AddMessage($"{text}");
            AddEmptyLine();

            foreach (var apk in apks)
            {
                await Task.Run(() => Install(apk));
            }

            AddEmptyLine();
        }

        Task Install(string path)
        {
            AddMessage($"インストールしています：{path}");

            var startInfo = new ProcessStartInfo
            {
                FileName = pathToADB,
                Arguments = $"install -r \"{path}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) => AddMessage(args.Data);
                process.ErrorDataReceived += (sender, args) => AddMessage(args.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return Task.CompletedTask;
            }
        }

        void AddMessage(string message) => mainWindow.AddMessage(message);
        void AddEmptyLine() => mainWindow.AddEmptyLine();
    }
}
