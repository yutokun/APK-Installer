using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            AddMessage("ここに APK をドロップするとインストールできます。");
            AddEmptyLine();

            if (Application.Current.Properties.Contains("apks"))
            {
                AddMessage("起動時に渡された APK をインストールします。");
                var apks = Application.Current.Properties["apks"] as string[];
                BatchInstall(apks);
            }
        }

        async void BatchInstall(string[] files)
        {
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
                await Task.Run(() => Install(apk));
            }

            AddEmptyLine();
        }

        Task Install(string path)
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

            using (var process = new Process())
            {
                var tcs = new TaskCompletionSource<bool>();
                process.EnableRaisingEvents = true;
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) => AddMessage(args.Data);
                process.ErrorDataReceived += (sender, args) => AddMessage(args.Data);
                process.Exited += (sender, args) => tcs.SetResult(true);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return tcs.Task;
            }
        }

        void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            BatchInstall(files);
        }

        void MainWindow_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            var isPresent = e.Data.GetDataPresent(DataFormats.FileDrop, true);
            e.Effects = isPresent ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        void AddMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                Redirect.Text = $"{Redirect.Text}\n{message}";
                Redraw();
                Redirect.ScrollToEnd();
            });
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
