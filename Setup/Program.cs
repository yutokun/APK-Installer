using System;
using System.IO;
using WixSharp;
using File = WixSharp.File;

namespace Setup
{
    internal class Program
    {
        public static void Main()
        {
            var exe = new File(@"..\APKInstaller\bin\Release\APKInstaller.exe");
            exe.Shortcuts = new[] { new FileShortcut("APK Installer", "%ProgramMenu%") };
            var files = new Dir(@"%ProgramFiles%\yutokun\APK Installer", exe);
            var project = new ManagedProject("APK Installer", files)
            {
                ProductId = new Guid("f9b8f955-f62f-4765-a2e3-c94c198b3eb0"),
                UpgradeCode = new Guid("f9b8f955-f62f-4765-a2e3-c94c198b3eb1"),
                Version = new Version("1.4.1"),
                LicenceFile = "../LICENSE.rtf",
                Language = "ja-JP"
            };
            project.AfterInstall += RemoveTempFolder;
            Compiler.BuildMsi(project);
        }

        static void RemoveTempFolder(SetupEventArgs e)
        {
            if (e.IsUninstalling)
            {
                var temp = Path.Combine(Path.GetTempPath(), "APKInstaller");
                Directory.Delete(temp, true);
            }
        }
    }
}
