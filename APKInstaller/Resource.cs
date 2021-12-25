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
        public static string TempDirectory
        {
            get
            {
                var path = Path.Combine(Path.GetTempPath(), "APKInstaller");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static async Task<string> Extract(string fileName, Action onPathLocked = null)
        {
            return await ExtractTo(fileName, TempDirectory, onPathLocked);
        }

        public static async Task<string> ExtractTo(string fileName, string destinationDirectory, Action onPathLocked = null)
        {
            var resourceUri = new Uri($"/Resources/{fileName}", UriKind.Relative);
            var stream = Application.GetResourceStream(resourceUri);
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

        public static bool OtherInstanceExists()
        {
            var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            return Process.GetProcessesByName(name).Length > 1;
        }
    }
}
