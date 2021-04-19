using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderNotCachedException : Exception
	{
		internal FolderNotCachedException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] not cached!") { }
	}
}
