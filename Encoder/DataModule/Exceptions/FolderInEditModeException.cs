using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderInEditModeException : Exception
	{
		internal FolderInEditModeException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] is in edit mode!") { }
	}
}
