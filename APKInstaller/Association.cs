using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace APKInstaller
{
    public static class Association
    {
        static string path;

        public static async Task Initialize()
        {
            await EnsureAssociationRegisterExist();
        }

        static async Task EnsureAssociationRegisterExist()
        {
            var resourceUri = new Uri("/AssociationRegister.exe", UriKind.Relative);
            var stream = Application.GetResourceStream(resourceUri);
            var directory = Path.Combine(Directory.GetParent(Path.GetTempFileName()).FullName, "APKInstaller");
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "AssociationRegister.exe");
            Debug.WriteLine(path);

            var adbBinary = new byte[stream.Stream.Length];
            await stream.Stream.ReadAsync(adbBinary, 0, (int)stream.Stream.Length);

            if (File.Exists(path))
            {
                if (IsLocked(path))
                {
                    Message.Add("既存の関連付けプログラムを利用します。前バージョンのものが存在している可能性があるので注意してください。");
                    Message.AddEmptyLine();
                    return;
                }

                File.Delete(path);
            }

            using (var fs = new FileStream(path, FileMode.Create))
            {
                fs.Write(adbBinary, 0, adbBinary.Length);
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

        public static void Associate() => Run("Associate");
        public static void Dissociate() => Run("Dissociate");

        static void Run(string argument)
        {
            var isAssociate = argument == "Associate";

            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = argument
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
                return;
            }

            var successMessage = isAssociate ? "関連付けに成功しました。今後は APK を直接ダブルクリックすることでインストールを行えます。" : "関連付けを解除しました。";
            Message.Add(successMessage);
        }
    }
}
