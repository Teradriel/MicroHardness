using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace MicroHardness.View
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Gif_Link(object sender, RoutedEventArgs e)
        {
            GifWindow gifWindow = new GifWindow();
            gifWindow.ShowDialog();
        }

        private void OnNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }
    }
}