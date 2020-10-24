using System;
using System.Collections.Generic;
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
                AddMessage("APK がドロップされませんでした。");
                AddEmptyLine();
                return;
            }

            var devices = await GetDevices();
            if (devices.Count == 0)
            {
                AddMessage("デバイスが接続されていません。");
                AddEmptyLine();
                return;
            }

            var unauthorizedDevices = devices.Where(d => d.IsUnauthorized).ToArray();
            if (unauthorizedDevices.Length >= 1)
            {
                var deviceText = "次のデバイスは、このコンピュータで USB デバッグを許可する必要があります：";
                foreach (var device in unauthorizedDevices)
                {
                    deviceText = $"{deviceText}\n{device.Model}（シリアル：{device.Serial}）";
                }

                AddMessage($"{deviceText}");
                AddEmptyLine();
            }

            var validDevices = devices.Where(d => d.IsValidDevice).ToArray();
            if (validDevices.Length == 0)
            {
                AddMessage("インストール可能なデバイスがありません。");
                AddEmptyLine();
            }

            if (validDevices.Length >= 2)
            {
                var deviceText = "複数のデバイスにインストールします：";
                foreach (var device in validDevices)
                {
                    deviceText = $"{deviceText}\n{device.Model}（シリアル：{device.Serial}）";
                }

                AddMessage($"{deviceText}");
                AddEmptyLine();
            }

            if (apks.Length >= 2)
            {
                var text = "複数の APK をインストールします：";
                foreach (var apk in apks)
                {
                    text = $"{text}\n{apk}";
                }

                AddMessage($"{text}");
                AddEmptyLine();
            }

            foreach (var device in validDevices)
            {
                AddMessage($"{device.Model} へのインストールを開始します。（シリアル：{device.Serial}）");
                foreach (var apk in apks)
                {
                    await Task.Run(() => Install(apk, device));
                }

                AddEmptyLine();
            }

            AddMessage("全てのインストールが完了しました。");
            AddEmptyLine();
        }

        async Task Install(string path, ADBDevice target)
        {
            AddMessage($"インストール中：{path}");
            await RunADB($"-s {target.Serial} install -r \"{path}\"");
        }

        async Task<List<ADBDevice>> GetDevices()
        {
            var devices = new List<ADBDevice>();
            var result = await RunADB("devices", false);
            var sr = new StringReader(result);
            var line = "";
            await sr.ReadLineAsync();
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parameters = line.Split('\t');
                var device = new ADBDevice { Serial = parameters[0], State = parameters[1] };
                device.Model = await RunADB($"-s {device.Serial} shell getprop ro.product.model", false);
                device.Model = device.Model.Replace("\n", "");
                devices.Add(device);
            }

            return devices;
        }

        Task<string> RunADB(string arguments, bool autoHandle = true)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pathToADB,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var output = "";

            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args.Data)) return;
                    output += $"{args.Data}\n";
                    if (autoHandle) HandleOutput(args.Data, process);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrWhiteSpace(args.Data)) return;
                    output += $"{args.Data}\n";
                    if (autoHandle) HandleOutput(args.Data, process);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return Task.FromResult(output);
            }
        }


        void HandleOutput(string message, Process process)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (message.Contains("Success"))
            {
                AddMessage("インストール完了");
            }
            else if (message.Contains("no devices/emulators found"))
            {
                AddMessage("デバイスが見つかりません。\n・デバイスが開発者モードであること\n・このコンピュータによる USB デバッグが許可されていること\n・正しく接続されていること\nを確認して下さい。");
                AddEmptyLine();
                process.Kill();
            }
            else
            {
                AddMessage(message);
                AddMessage("未知のメッセージを受け取ったため、処理を中止しました。");
                AddEmptyLine();
            }
        }

        void AddMessage(string message) => mainWindow.AddMessage(message);
        void AddEmptyLine() => mainWindow.AddEmptyLine();
    }
}
