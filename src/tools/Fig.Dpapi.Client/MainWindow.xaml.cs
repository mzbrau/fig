using System.Windows;

namespace Fig.Dpapi.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel model;
        
        public MainWindow()
        {
            model = new ViewModel();
            DataContext = model;
            InitializeComponent();
        }

        private void OnCopyEncryptedText(object sender, RoutedEventArgs e)
        {
            model.CopyEncryptedText();
        }

        private void OnGenerateSecret(object sender, RoutedEventArgs e)
        {
            model.GenerateSecret();
        }
    }
}
