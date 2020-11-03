using System;
using System.Diagnostics;
using System.Reflection;
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
        public event Action OnContentRenderedAction;

        public MainWindow()
        {
            InitializeComponent();
            Message.Initialize();
            Installer.Initialize();
            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Window.Title += $" - v{ver.ProductVersion}";
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            OnContentRenderedAction?.Invoke();
        }

        public void AddDropEvent()
        {
            Redirect.PreviewDragOver += MainWindow_OnPreviewDragOver;
            Redirect.PreviewDrop += MainWindow_OnPreviewDrop;
        }

        public void RemoveDropEvent()
        {
            Redirect.PreviewDragOver -= MainWindow_OnPreviewDragOver;
            Redirect.PreviewDrop -= MainWindow_OnPreviewDrop;
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

        static void Redraw()
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

        void Associate(object sender, RoutedEventArgs e) => Association.Associate();

        void Dissociate(object sender, RoutedEventArgs e) => Association.Dissociate();
    }
}
