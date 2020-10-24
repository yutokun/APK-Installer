using System;
using System.Windows;
using System.Windows.Threading;

namespace APKInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public event Action<string[]> OnFileDropped;

        public MainWindow()
        {
            InitializeComponent();
            Installer.Initialize();
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
                OnFileDropped?.Invoke(apks);
            }
        }

        void MainWindow_OnPreviewDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            OnFileDropped?.Invoke(files);
        }

        void MainWindow_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            var isPresent = e.Data.GetDataPresent(DataFormats.FileDrop, true);
            e.Effects = isPresent ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        public void AddMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                Redirect.Text = $"{Redirect.Text}\n{message}";
                Redraw();
                Redirect.ScrollToEnd();
            });
        }

        public void AddEmptyLine()
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
