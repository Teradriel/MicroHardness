using System.Windows;

namespace Microhardness.View
{
    /// <summary>
    /// Lógica de interacción para KnownIssues.xaml
    /// </summary>
    public partial class KnownIssues : Window
    {
        public KnownIssues()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}