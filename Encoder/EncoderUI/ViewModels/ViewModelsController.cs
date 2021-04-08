using Microsoft.Extensions.DependencyInjection;
using System;

namespace EncoderUI.ViewModels
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
