using System.Windows;
using System.Windows.Input;

namespace Balatron
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private void LivePeekButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Views.LivePeekWindow.Instance;
            window.Show();
            window.Activate();
        }

        private void SavegameEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Views.SavegameEditorWindow.Instance;
            window.Show();
            window.Activate();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                return;

            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
