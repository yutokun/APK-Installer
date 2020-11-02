using System;
using System.Diagnostics;
using System.IO;
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
            App.OnExitAction += Terminate;
        }

        static async Task EnsureADBExist()
        {
            var resourceUri = new Uri("/adb.exe", UriKind.Relative);
            var adbStream = Application.GetResourceStream(resourceUri);
            var adbDirectory = Path.Combine(Directory.GetParent(Path.GetTempFileName()).FullName, "APKInstaller");
            Directory.CreateDirectory(adbDirectory);
            var adbPath = Path.Combine(adbDirectory, "adb.exe");
            Debug.WriteLine(adbPath);

            var adbBinary = new byte[adbStream.Stream.Length];
            await adbStream.Stream.ReadAsync(adbBinary, 0, (int)adbStream.Stream.Length);

            if (File.Exists(adbPath))
            {
                if (IsLocked(adbPath))
                {
                    Message.Add("既存の通信プログラムを利用します。前バージョンのものが存在している可能性があるので注意してください。");
                    Message.AddEmptyLine();
                    pathToADB = adbPath;
                    return;
                }

                File.Delete(adbPath);
            }

            using (var fs = new FileStream(adbPath, FileMode.Create))
            {
                fs.Write(adbBinary, 0, adbBinary.Length);
                pathToADB = adbPath;
            }

            bool IsLocked(string path)
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

        static async void Terminate()
        {
            if (usingOwnedServer)
            {
                await Task.Run(() =>
                {
                    var noExistingADB = Process.GetProcessesByName("adb").Length == 0;
                    if (noExistingADB)
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

                        Message.Add("完了");
                        Message.AddEmptyLine();
                        usingOwnedServer = true;
                    }

                    var path = Directory.GetParent(pathToADB).FullName;
                    Directory.Delete(path, true);
                });
            }
        }
    }
}
