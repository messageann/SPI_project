using System.Windows;
using System.Windows.Controls;
using UI.ViewModels;

namespace UI.Views
{
    /// <summary>
    /// Interaction logic for EncoderWindow.xaml
    /// </summary>
    public partial class EncoderWindow : Window
    {
        public EncoderWindow(EncoderWindowVM vm)
        {
            DataContext = vm;
            InitializeComponent();
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var f in files)
                {
                    ((EncoderWindowVM)this.DataContext).EncryptFileCommand.Execute(f);
                }
                MessageBox.Show("File crypt done!");
            }
        }

        private void CancelItem_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var s = ((Button)sender);
            if (!s.IsEnabled) s.Visibility = Visibility.Hidden;
            else s.Visibility = Visibility.Visible;
        }
    }
}
