using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class NotUnlockedException : Exception
	{
		internal NotUnlockedException(FolderInfo fi) : base($"FolderInfo[id:{fi.Id}] not unlocked!") { }
		internal NotUnlockedException(LogInfo li) : base($"FolderInfo[id:{li.ID}] not unlocked!") { }
	}
}
