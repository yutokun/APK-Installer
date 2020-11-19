using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public static class Sound
    {
        static string soundPath;

        public static async Task Initialize()
        {
            soundPath = await CopyResourceToTempDirectory("microwave-tin1.wav");
        }

        public static void Play()
        {
            using (var player = new SoundPlayer(soundPath))
            {
                player.Play();
            }
        }

        static async Task<string> CopyResourceToTempDirectory(string path)
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
                File.Delete(copiedPath);
            }

            using (var fs = new FileStream(copiedPath, FileMode.Create))
            {
                fs.Write(binary, 0, binary.Length);
                return copiedPath;
            }
        }
    }
}
