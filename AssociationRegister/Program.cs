﻿using Microsoft.Win32;

namespace AssociationRegister
{
    internal static class Program
    {
        const string Extension = ".apk";
        const string FileType = "APKInstaller.apk";

        public static void Main(string[] args)
        {
            var command = args[0];
            if (command == "Associate")
            {
                var app = args[1];
                Associate(app);
            }
            else if (command == "Dissociate")
            {
                Dissociate();
            }
        }

        static void Associate(string path)
        {
            var command = $"\"{path}\" \"%1\"";
            var description = "Android アプリケーション";
            var verb = "open";
            var verbDescription = "インストール";
            var icon = $"\"{path}\",1";

            var rootKey = Registry.ClassesRoot;
            var regKey = rootKey.CreateSubKey(Extension);
            regKey.SetValue("", FileType);
            regKey.Close();

            var typeKey = rootKey.CreateSubKey(FileType);
            typeKey.SetValue("", description);
            typeKey.Close();

            var verbKey = rootKey.CreateSubKey($"{FileType}\\shell\\{verb}");
            verbKey.SetValue("", verbDescription);
            verbKey.Close();

            var cmdKey = rootKey.CreateSubKey($"{FileType}\\shell\\{verb}\\command");
            cmdKey.SetValue("", command);
            cmdKey.Close();

            var iconKey = rootKey.CreateSubKey($"{FileType}\\DefaultIcon");
            iconKey.SetValue("", icon);
            iconKey.Close();
        }

        static void Dissociate()
        {
            Registry.ClassesRoot.DeleteSubKeyTree(Extension);
            Registry.ClassesRoot.DeleteSubKeyTree(FileType);
        }
    }
}
