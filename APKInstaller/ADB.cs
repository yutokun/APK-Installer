using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public static class ADB
    {
        static string pathToADB;
        static bool usingOwnedServer;

        public static async Task Initialize()
        {
            await EnsureADBExist();
            await EnsureDaemonRunning();
        }

        static async Task EnsureADBExist()
        {
            pathToADB = await CopyResourceToTempDirectory("adb.exe");
            await CopyResourceToTempDirectory("AdbWinApi.dll");
            await CopyResourceToTempDirectory("AdbWinUsbApi.dll");

            async Task<string> CopyResourceToTempDirectory(string path)
            {
                var resourceUri = new Uri($"/{path}", UriKind.Relative);
                var stream = Application.GetResourceStream(resourceUri);
                var directory = Path.Combine(Directory.GetParent(Path.GetTempFileName()).FullName, "APKInstaller");
                Directory.CreateDirectory(directory);
                var copiedPath = Path.Combine(directory, path);

                var binary = new byte[stream.Stream.Length];
                await stream.Stream.ReadAsync(binary, 0, (int)stream.Stream.Length);

                if (File.Exists(copiedPath))
                {
                    if (IsLocked(copiedPath))
                    {
                        return copiedPath;
                    }

                    File.Delete(copiedPath);
                }

                using (var fs = new FileStream(copiedPath, FileMode.Create))
                {
                    fs.Write(binary, 0, binary.Length);
                    return copiedPath;
                }
            }
        }

        static async Task EnsureDaemonRunning()
        {
            await Task.Run(() =>
            {
                var noRunningADB = Process.GetProcessesByName("adb").Length == 0;
                if (noRunningADB)
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

                    Message.Add("完了");
                    Message.AddEmptyLine();
                    usingOwnedServer = true;
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
            if (usingOwnedServer)
            {
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
                    usingOwnedServer = false;
                }

                var path = Directory.GetParent(pathToADB).FullName;
                while (IsLocked(pathToADB))
                {
                    Thread.Sleep(500);
                }

                Directory.Delete(path, true);
                Message.Add("完了");
                Thread.Sleep(1000);
            }
        }

        static bool IsLocked(string path)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                fs?.Close();
            }

            return false;
        }
    }
}
