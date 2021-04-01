using System;

namespace DataModule.Exceptions
{
	[Serializable]
	public class FolderNotCachedException : Exception
	{
		public FolderNotCachedException() { }
		internal FolderNotCachedException(FolderInfoCore fi) : base($"Folderinfo[id:{fi.Id}] not cached!") { }
		protected FolderNotCachedException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
