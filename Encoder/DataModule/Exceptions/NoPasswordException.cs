using System;

namespace DataModule.Exceptions
{
	public class NoPasswordException : Exception
	{
		internal NoPasswordException() : base("You must provide password!") { }
	}
}
