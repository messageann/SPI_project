using System;

namespace DataModule.Exceptions
{
	public class InitModeException : Exception
	{
		internal InitModeException() : base("Editing object in init state!") { }
	}
}
