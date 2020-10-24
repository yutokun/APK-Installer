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
            var path = Directory.GetParent(pathToADB).FullName;
            Directory.Delete(path, true);
        }

        void CreateADB()
        {
            var resourceUri = new Uri("/adb.exe", UriKind.Relative);
            var adbStream = Application.GetResourceStream(resourceUri);
            var adbDirectory = Path.Combine(Directory.GetParent(Path.GetTempFileName()).FullName, "APKInstaller");
            Directory.CreateDirectory(adbDirectory);
            var adbPath = Path.Combine(adbDirectory, "adb.exe");
            Debug.WriteLine(adbPath);

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
                process.OutputDataReceived += (sender, args) => HandleOutput(args.Data, process);
                process.ErrorDataReceived += (sender, args) => HandleOutput(args.Data, process);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return Task.CompletedTask;
            }
        }

        void HandleOutput(string message, Process process)
        {
            if (string.IsNullOrEmpty(message))
            {
                AddMessage(message);
            }
            else if (message.Contains("no devices/emulators found"))
            {
                AddMessage("デバイスが見つかりません。\n・デバイスが開発者モードであること\n・このコンピュータによる USB デバッグが許可されていること\n・正しく接続されていること\nを確認して下さい。");
                process.Kill();
            }
            else
            {
                AddMessage(message);
            }

            AddEmptyLine();
        }

        void AddMessage(string message) => mainWindow.AddMessage(message);
        void AddEmptyLine() => mainWindow.AddEmptyLine();
    }
}
