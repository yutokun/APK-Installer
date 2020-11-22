using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace APKInstaller
{
    public static class ADB
    {
        static string pathToADB;
        static string RunningOwnedADBMarkerPath => Path.Combine(Resource.TempDirectory, "UsingOwnADB");
        static bool RunningOwnedADB => File.Exists(RunningOwnedADBMarkerPath);

        public static async Task Initialize()
        {
            await EnsureADBExist();
            await EnsureDaemonRunning();
        }

        static async Task EnsureADBExist()
        {
            pathToADB = await Resource.Extract("adb.exe", () =>
            {
                Message.Add("既に実行中の通信プログラムがあるため、これを利用します。");
                Message.AddEmptyLine();
            });
            await Resource.Extract("AdbWinApi.dll");
            await Resource.Extract("AdbWinUsbApi.dll");
        }

        static async Task EnsureDaemonRunning()
        {
            await Task.Run(() =>
            {
                var noAdbRunning = Process.GetProcessesByName("adb").Length == 0;
                if (noAdbRunning)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = pathToADB,
                        Arguments = "start-server",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Message.Add("通信の準備をしています...");
                    var process = new Process { StartInfo = startInfo };
                    process.Start();
                    process.WaitForExit();

                    using (var fs = File.Create(RunningOwnedADBMarkerPath)) fs.Close();

                    Message.Add("完了");
                    Message.AddEmptyLine();
                }
            });
        }

        public static async Task<string> Run(string arguments, Action<string, Process> action = null)
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
            await EnsureDaemonRunning();

            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) => OnDataReceived(args.Data, process, ref output, action);
                process.ErrorDataReceived += (sender, args) => OnDataReceived(args.Data, process, ref output, action);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return output;
            }
        }

        static void OnDataReceived(string message, Process process, ref string output, Action<string, Process> onReceived)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.StartsWith("*")) return;
            output += $"{message}\n";

            onReceived?.Invoke(message, process);
        }

        public static void Terminate(object sender, CancelEventArgs cancelEventArgs)
        {
            if (Resource.OtherInstanceExists()) return;
            if (!RunningOwnedADB) return;

            var adbRunning = Process.GetProcessesByName("adb").Length > 0;
            if (adbRunning)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pathToADB,
                    Arguments = "kill-server",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Message.Add("通信機能を終了しています...");
                var process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
