using System.Windows;

namespace APKInstaller
{
    public class Message
    {
        static Message instance;
        static MainWindow mainWindow;

        public static void Initialize()
        {
            if (instance != null) return;
            instance = new Message();
        }

        Message()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        public static void Add(string message) => mainWindow.AddMessage(message);
        public static void AddEmptyLine() => mainWindow.AddEmptyLine();
    }
}
