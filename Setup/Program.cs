using System;
using System.Diagnostics;
using System.IO;
using WixSharp;
using File = WixSharp.File;

namespace Setup
{
    internal static class Program
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
                GUID = new Guid("f9b8f955-f62f-4765-a2e3-c94c198b3eb0"),
                Version = new Version("1.4.1"),
                LicenceFile = "../LICENSE.rtf",
                Language = "ja-JP"
            };
            project.BeforeInstall += KillApp;
            project.AfterInstall += RemoveTempFolder;
            project.MajorUpgrade = new MajorUpgrade
            {
                AllowSameVersionUpgrades = true,
                Schedule = UpgradeSchedule.afterInstallInitialize,
                DowngradeErrorMessage = "DGEM"
            };
            Compiler.BuildMsi(project);
        }

        static void KillApp(SetupEventArgs e)
        {
            var processes = Process.GetProcessesByName("APKInstaller");
            foreach (var process in processes)
            {
                process.CloseMainWindow();
                process.Kill();
                process.WaitForExit();
            }
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
