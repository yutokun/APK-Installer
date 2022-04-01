using System.ComponentModel;
using System.IO;

namespace APKInstaller
{
    public class Updater
    {
        public static async void Initialize()
        {
            Directory.SetCurrentDirectory(Resource.TempDirectory);
            await Resource.ExtractTo("WinSparkle.dll", Directory.GetCurrentDirectory());
            WinSparkle.win_sparkle_set_appcast_url("https://appcast.yutokun.com/apk-installer/appcast.xml");
            WinSparkle.win_sparkle_init();
            WinSparkle.win_sparkle_check_update_without_ui();
        }

        public static void Cleanup(object sender, CancelEventArgs cancelEventArgs)
        {
            WinSparkle.win_sparkle_cleanup();
        }
    }
}
