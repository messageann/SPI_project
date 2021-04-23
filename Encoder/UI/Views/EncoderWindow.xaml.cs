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
	}
}
