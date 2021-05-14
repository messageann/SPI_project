using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderCachedException : Exception
	{
		internal FolderCachedException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] already cached!") { }
	}
}
