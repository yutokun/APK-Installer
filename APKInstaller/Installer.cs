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
        static Installer instance;
        static MainWindow mainWindow;

        public static void Initialize()
        {
            if (instance != null) return;
            instance = new Installer();
        }

        Installer()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                Installer.mainWindow = mainWindow;
                mainWindow.OnContentRenderedAction += OnWindowAppeared;
                mainWindow.Closing += ADB.Terminate;
            }
        }

        ~Installer()
        {
            instance = null;
        }

        async void OnWindowAppeared()
        {
            await ADB.Initialize();

            if (Application.Current.Properties.Contains("apks"))
            {
                Message.Add("起動時に渡された APK をインストールします。");
                var apks = Application.Current.Properties["apks"] as string[];
                BatchInstall(apks);
            }
            else
            {
                Message.Add("ここに APK をドロップするとインストールできます。");
            }

            Message.AddEmptyLine();

            mainWindow.OnFileDropped += BatchInstall;
            mainWindow.AddDropEvent();
        }

        async void BatchInstall(string[] files)
        {
            var apks = files.Where(f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

            if (apks.Length == 0)
            {
                Message.Add("APK がドロップされませんでした。");
                Message.AddEmptyLine();
                return;
            }

            var devices = await GetDevices();
            if (devices.Count == 0)
            {
                Message.Add("❌ デバイスが見つかりません。\n・デバイスが開発者モードであること\n・このコンピュータによる USB デバッグが許可されていること\n・正しく接続されていること\nを確認して下さい。");
                Message.AddEmptyLine();
                return;
            }

            var unauthorizedDevices = devices.Where(d => d.IsUnauthorized).ToArray();
            if (unauthorizedDevices.Length >= 1)
            {
                var deviceText = "次のデバイスは、このコンピュータによる USB デバッグを許可する必要があります：";
                foreach (var device in unauthorizedDevices)
                {
                    deviceText = $"{deviceText}\nシリアル：{device.Serial}";
                }

                Message.Add($"{deviceText}");
                Message.AddEmptyLine();
            }

            var validDevices = devices.Where(d => d.IsValidDevice).ToArray();
            if (validDevices.Length == 0)
            {
                Message.Add("インストール可能なデバイスがありません。");
                Message.AddEmptyLine();
                return;
            }

            if (validDevices.Length >= 2)
            {
                var deviceText = "複数のデバイスにインストールします：";
                foreach (var device in validDevices)
                {
                    deviceText = $"{deviceText}\n{device.Model}（シリアル：{device.Serial}）";
                }

                Message.Add($"{deviceText}");
                Message.AddEmptyLine();
            }

            if (apks.Length >= 2)
            {
                var text = "複数の APK をインストールします：";
                foreach (var apk in apks)
                {
                    text = $"{text}\n{apk}";
                }

                Message.Add($"{text}");
                Message.AddEmptyLine();
            }

            mainWindow.RemoveDropEvent();

            foreach (var device in validDevices)
            {
                Message.Add($"{device.Model} へのインストールを開始します。（シリアル：{device.Serial}）");
                foreach (var apk in apks)
                {
                    await Task.Run(() => Install(apk, device));
                }
            }

            Message.Add("全てのインストールが完了しました。");
            Message.AddEmptyLine();

            mainWindow.AddDropEvent();
        }

        static async Task Install(string path, ADBDevice target)
        {
            Message.Add($"インストール中...：{path}");
            await ADB.Run($"-s {target.Serial} install -r \"{path}\"", HandleOutput);
        }

        static async Task<List<ADBDevice>> GetDevices()
        {
            var devices = new List<ADBDevice>();
            var result = await ADB.Run("devices");
            var sr = new StringReader(result);
            var line = "";
            await sr.ReadLineAsync();
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parameters = line.Split('\t');
                var device = new ADBDevice { Serial = parameters[0], State = parameters[1] };
                device.Model = await ADB.Run($"-s {device.Serial} shell getprop ro.product.model");
                device.Model = device.Model.Replace("\n", "");
                devices.Add(device);
            }

            return devices;
        }

        static void HandleOutput(string message, Process process)
        {
            if (message.Contains("Success"))
            {
                Message.Add("✔ インストール完了");
            }
            else if (message.Contains("no devices/emulators found"))
            {
                Message.Add("❌ デバイスが見つかりません。\n・デバイスが開発者モードであること\n・このコンピュータによる USB デバッグが許可されていること\n・正しく接続されていること\nを確認して下さい。");
                process.Kill();
            }
            else if (message.Contains("signatures do not match previously installed version"))
            {
                Message.Add("❌ 同じアプリが既にインストールされており、かつ署名が異なるためアップデートできません。APK の作成者に確認を取って下さい。");
            }
            else
            {
                Message.Add(message);
                Message.Add("❌ 未知のメッセージを受け取ったため、処理を中止しました。");
            }

            Message.AddEmptyLine();
        }
    }
}
