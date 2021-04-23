using Microsoft.Extensions.DependencyInjection;
using System;
using DataModule;
using System.IO;

namespace UI.ViewModels
{
	public class ViewModelsController
	{
		private static ServiceProvider _provider;
		public static bool IsInitialized => _provider is not null;

		public static void Init()
		{
			if (IsInitialized) throw new InvalidOperationException("reinit is not supported");
			_provider = new ServiceCollection()
				.AddScoped<EncoderWindowVM>()
				.AddScoped<DataService>().BuildServiceProvider(
					new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true }
				);
		}

		public static void Unload()
		{
			_provider?.Dispose();
		}

		//public static EncoderWindowVM EncoderWindowVM => _provider.GetRequiredService<EncoderWindowVM>();

		public static IServiceScope GetScope() => _provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
	}
}
