using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public static class Resource
    {
        public static async Task<string> Extract(string path, Action onPathLocked = null)
        {
            var resourceUri = new Uri($"/Resources/{path}", UriKind.Relative);
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

        public static bool IsLocked(string path)
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
