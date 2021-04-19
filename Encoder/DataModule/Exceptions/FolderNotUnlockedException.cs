using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderNotUnlockedException : Exception
	{
		internal FolderNotUnlockedException(FolderInfo fi) : base($"FolderInfo[id:{fi.Id}] not unlocked!") { }
	}
}
