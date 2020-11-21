using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace APKInstaller
{
    public static class Association
    {
        static string path;

        public static async Task Initialize()
        {
            path = await Resource.Extract("AssociationRegister.exe", () =>
            {
                Message.Add("既存の関連付けプログラムを利用します。前バージョンのものが存在している可能性があるので注意してください。");
                Message.AddEmptyLine();
            });
        }

        public static void Associate() => Run($"Associate \"{Assembly.GetEntryAssembly().Location}\"");
        public static void Dissociate() => Run("Dissociate");
        static async void Run(string argument) => await RunAsync(argument);

        static async Task RunAsync(string argument)
        {
            await Task.Run(() =>
            {
                var isAssociate = argument.StartsWith("Associate");

                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = argument,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas",
                };

                try
                {
                    var process = Process.Start(startInfo);
                    process.WaitForExit();
                }
                catch (Win32Exception)
                {
                    var failureMessage = isAssociate ? "APK を本アプリに関連付けられませんでした。" : "関連付けの解除に失敗しました。";
                    Message.Add(failureMessage);
                    Message.AddEmptyLine();
                    return;
                }

                var successMessage = isAssociate ? "関連付けに成功しました。今後は APK を直接ダブルクリックすることでインストールを行えます。" : "関連付けを解除しました。";
                Message.Add(successMessage);
                Message.AddEmptyLine();
            });
        }
    }
}
