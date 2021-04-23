using System.Windows;
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
				foreach(var f in files)
				{
					((EncoderWindowVM)this.DataContext).EncryptFileCommand.Execute(f);
				}
				MessageBox.Show("File crypt done!");
			}
		}
	}
}
