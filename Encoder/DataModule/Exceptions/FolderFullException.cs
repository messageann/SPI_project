using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderFullException : Exception
	{
		internal FolderFullException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] is full!") { }
	}
}
