using DataModule.Models;
using System;

namespace DataModule.Exceptions
{
	[Serializable]
	public class FolderFullException : Exception
	{
		internal FolderFullException(FolderInfo fi) : base($"Folderinfo[id:{fi.Id}] is full!") { }
		protected FolderFullException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
