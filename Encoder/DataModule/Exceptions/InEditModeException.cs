using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class InEditModeException : Exception
	{
		internal InEditModeException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] is in edit mode!") { }
		internal InEditModeException(LogInfo li) : base($"Loginfo[id:{li.ID}] is in edit mode!") { }
	}
}
