using UI.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DataModule;
using UI.ViewModels;
using System.Windows.Controls;

namespace UI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private bool _isShutdowned = false;
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			ViewModelsController.Init();
			while (!_isShutdowned)
			{
				using (var scope = ViewModelsController.GetScope())
				{
					var scopeProvider = scope.ServiceProvider;
					var auth = new Authorization();
					auth.ShowDialog();
					DataService ds = scopeProvider.GetRequiredService<DataService>();
					if (auth.file == null)
					{
						this.Shutdown(3);
						return;
					}
					else
					{
						var res = ds.Init(auth.file, auth.password);
						if (!res)
						{
							MessageBox.Show("Bad password!");
							continue;
						}
						else
						{
							var vm = scopeProvider.GetRequiredService<EncoderWindowVM>();
							vm.AccountName = new string(auth.file.Name.Take(auth.file.Name.LastIndexOf(auth.file.Extension)).ToArray());
							new EncoderWindow(vm).ShowDialog();
						}
					}
				}
			}
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			_isShutdowned = true;
			ViewModelsController.Unload();
		}

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			var pb = (PasswordBox)sender;
			var htb = (TextBlock)pb.Template.FindName("HINT_Host", pb);
			htb.Visibility = pb.SecurePassword.Length == 0 ? Visibility.Visible : Visibility.Hidden;
		}
	}
}
