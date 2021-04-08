using EncoderUI.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EncoderUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			var initres = ViewModels.ViewModelsController.Init();
			if (initres)
			{
				new EncoderWindow().Show();
			}
			else
			{
				Application.Current.Shutdown(-1);
			}
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			ViewModels.ViewModelsController.Unload();
		}
	}
}
