using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public static class Resource
    {
        public static string TempDirectory => Path.Combine(Directory.GetParent(Path.GetTempFileName()).FullName, "APKInstaller");

        public static async Task<string> Extract(string fileName, Action onPathLocked = null)
        {
            return await ExtractTo(fileName, TempDirectory, onPathLocked);
        }

        public static async Task<string> ExtractTo(string fileName, string destinationDirectory, Action onPathLocked = null)
        {
            var resourceUri = new Uri($"/Resources/{fileName}", UriKind.Relative);
            var stream = Application.GetResourceStream(resourceUri);
            Directory.CreateDirectory(destinationDirectory);
            var copiedPath = Path.Combine(destinationDirectory, fileName);

            var binary = new byte[stream.Stream.Length];
            await stream.Stream.ReadAsync(binary, 0, (int)stream.Stream.Length);

            if (File.Exists(copiedPath))
            {
                if (IsLocked(copiedPath))
                {
                    onPathLocked?.Invoke();
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

        static bool IsLocked(string path)
        {
            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);

            if (isDirectory)
            {
                var info = new DirectoryInfo(path);
                try
                {
                    info.GetDirectories();
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
            else
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
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

        public static void Cleanup(object sender, CancelEventArgs cancelEventArgs)
        {
            if (OtherInstanceExists()) return;

            Message.Add("クリーンアップ中...");

            while (IsLocked(TempDirectory))
            {
                Thread.Sleep(500);
            }

            var directoryIsEmpty = true;
            var files = Directory.GetFiles(TempDirectory);
            foreach (var file in files)
            {
                var tryout = 0;
                while (IsLocked(file) && tryout < 5)
                {
                    ++tryout;
                    Thread.Sleep(200);
                }

                if (tryout >= 5)
                {
                    directoryIsEmpty = false;
                    continue;
                }

                File.Delete(file);
            }

            if (directoryIsEmpty)
            {
                Directory.Delete(TempDirectory, true);
                Message.Add("完了");
                Thread.Sleep(1000);
            }
            else
            {
                Message.Add("他のアプリケーションが通信機能を使用しているため、クリーンアップをスキップしました。");
                Thread.Sleep(4000);
            }
        }

        public static bool OtherInstanceExists()
        {
            var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            return Process.GetProcessesByName(name).Length > 1;
        }
    }
}
