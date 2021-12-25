using System;
using WixSharp;

namespace Setup
{
    internal class Program
    {
        public static void Main()
        {
            var files = new Dir(@"%ProgramFiles%\yutokun\APK Installer", new File(@"..\APKInstaller\bin\Release\APKInstaller.exe"));
            var project = new Project("APK Installer", files);
            project.GUID = new Guid();
            Compiler.BuildMsi(project);
        }
    }
}
