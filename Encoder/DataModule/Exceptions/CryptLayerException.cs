using System;

namespace DataModule.Exceptions
{
	public class CryptLayerException : Exception
	{
		internal CryptLayerException(bool extra) : base($"Layer {(extra ? "already" : "not")}") { }
	}
}
