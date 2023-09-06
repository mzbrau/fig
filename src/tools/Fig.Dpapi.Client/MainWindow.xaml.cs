using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
