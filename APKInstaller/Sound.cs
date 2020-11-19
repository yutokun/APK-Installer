using System.Media;
using System.Threading.Tasks;

namespace APKInstaller
{
    public static class Sound
    {
        static string soundPath;

        public static async Task Initialize()
        {
            soundPath = await Resource.Extract("microwave-tin1.wav");
        }

        public static void Play()
        {
            using (var player = new SoundPlayer(soundPath))
            {
                player.Play();
            }
        }
    }
}
