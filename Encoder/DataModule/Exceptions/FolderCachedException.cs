using DataModule.Models;
using System;

namespace DataModule.Exceptions
{

	[Serializable]
	public class FolderCachedException : Exception
	{
		public FolderCachedException() { }
		public FolderCachedException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] already cached!") { }
		protected FolderCachedException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
