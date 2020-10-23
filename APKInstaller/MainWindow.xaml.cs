using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace APKInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            AddMessage("ここに APK をドロップするとインストールできます。");
            AddEmptyLine();
        }

        void Install(string path)
        {
            AddMessage($"インストールしています：{path}");

            var startInfo = new ProcessStartInfo
            {
                FileName = Application.Current.Properties["adb"].ToString(),
                Arguments = $"install -r \"{path}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = new Process { StartInfo = startInfo };
            process.Start();

            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            AddMessage(result == "Success\r\n" ? "インストール完了" : result);
            Debug.WriteLine(result);
        }

        void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var apks = files.Where(f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

            if (apks.Length == 0)
            {
                AddMessage("APK がドロップされませんでした。\n");
                return;
            }

            var text = "インストールする APK：";
            foreach (var apk in apks)
            {
                text = $"{text}\n{apk}";
            }

            AddMessage($"{text}");
            AddEmptyLine();

            foreach (var apk in apks)
            {
                Install(apk);
            }

            AddEmptyLine();
        }

        void MainWindow_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            var isPresent = e.Data.GetDataPresent(DataFormats.FileDrop, true);
            e.Effects = isPresent ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        void AddMessage(string message)
        {
            Redirect.Text = $"{Redirect.Text}\n{message}";
            Redraw();
            Redirect.ScrollToEnd();
        }

        void AddEmptyLine()
        {
            AddMessage(string.Empty);
        }

        void Redraw()
        {
            var frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
