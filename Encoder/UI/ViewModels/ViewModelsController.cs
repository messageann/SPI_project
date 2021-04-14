using Microsoft.Extensions.DependencyInjection;
using System;
using DataModule;

namespace UI.ViewModels
{
	public class ViewModelsController
	{
		private static ServiceProvider _provider;
		public static bool IsInitialized => _provider is not null;

		public static bool Init()
		{
			if (IsInitialized) throw new InvalidOperationException("reinit is not supported");
			var services = new ServiceCollection();

			services.AddSingleton<EncoderWindowVM>();
			services.AddSingleton<DataService>((sp)=>new DataService("encdb"));

			_provider = services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true });
			return true;
		}

		public static void Unload()
		{
			_provider?.Dispose();
		}

		public static EncoderWindowVM EncoderWindowVM => _provider.GetRequiredService<EncoderWindowVM>();
	}
}
