using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	public class FolderNotCryptedException : Exception
	{
		internal FolderNotCryptedException(FolderInfo fi) : base($"FolderInfo[id:{fi.Id}] not crypted!") { }
	}
}
