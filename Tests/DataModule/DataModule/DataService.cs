using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Core3Tests.DataModule
{
	public class DataService
	{
		#region app extra data consts
		private const byte _bytesPerExtradata = 100;


		private const byte _bytesPerLastLoginfoId = 2;//uint16
		private const byte _bytesPerLastFolderinfoId = 2;//uint16
		private const byte _bytesPerFoldersCount = 2;//uint16
		private const byte _bytesPerEmptyFoldersCount = 2;//uint16

		private const int DEFAULT_FOLDERS_COUNT = 16;
		#endregion app extra data consts

		#region fields
		private UInt16 _lastLoginfoId;
		private UInt16 _lastFolderinfoId;
		private UInt16 _foldersCount;
		private UInt16 _emptyFoldersCount;
		private readonly string _dataPath;
		private readonly Queue<long> _emptyFolderPoses;

		private readonly List<FolderInfo> _folders;
		#endregion fields


		public DataService(string dataPath)
		{
			_dataPath = dataPath;
			_emptyFolderPoses = new Queue<long>(DEFAULT_FOLDERS_COUNT);
			_folders = new List<FolderInfo>(DEFAULT_FOLDERS_COUNT);
		}
		public void Init()
		{
			if (!File.Exists(_dataPath))
			{
				using (var fs = new FileStream(_dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.SequentialScan))
				{
					fs.SetLength(_bytesPerExtradata);
				}
				return;
			}
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.None, FolderInfo.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryReader br = new BinaryReader(fs))
			{
				_lastLoginfoId = br.ReadUInt16();
				_lastFolderinfoId = br.ReadUInt16();
				_foldersCount = br.ReadUInt16();
				_emptyFoldersCount = br.ReadUInt16();
				fs.Seek(_bytesPerExtradata, SeekOrigin.Begin);
				long preFolderPos;
				UInt32 status;
				byte[] buffer = new byte[FolderInfo.BYTES_BODY];
				_folders.Capacity = _foldersCount.ToTheUpperPowerOf2();
				while (_emptyFolderPoses.Count < _emptyFoldersCount)
				{
					preFolderPos = fs.Position;
					status = br.ReadUInt32();
					if ((status & (uint)StatusEnum.NULL) == 1)//null folder
					{
						_emptyFolderPoses.Enqueue(preFolderPos);
						fs.Seek(preFolderPos + FolderInfo.BYTES_BODY + FolderInfo.DEFAULT_QUANTITY * LogInfo.EmptyLogInfo.Length, SeekOrigin.Begin);
					}
					else//non null folder
					{
						fs.Seek(preFolderPos, SeekOrigin.Begin);
						_folders.Add(FolderInfo.ReadBodyFromFile(br));
					}
				}
				while (_folders.Count < _foldersCount)
				{
					preFolderPos = fs.Position;
					_folders.Add(FolderInfo.ReadBodyFromFile(br));
				}
			}
		}

		public void AddNewFolder(StatusEnum status, string name, string descr)
		{
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Write, FileShare.None, FolderInfo.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryWriter bw = new BinaryWriter(fs))
			{
				fs.Seek(_bytesPerLastLoginfoId, SeekOrigin.Begin);
				bw.Write(++_lastFolderinfoId);
				bw.Write(++_foldersCount);
				FolderInfo res;
				if (_emptyFolderPoses.Count > 0 && status == StatusEnum.Normal)//if open space exists
				{
					fs.Seek(_emptyFolderPoses.Dequeue(), SeekOrigin.Begin);
					res = new FolderInfo(fs.Position, status, 0, _lastFolderinfoId, name, descr);
					res.WriteBodyToFile(bw);
					for (int i = 0; i < FolderInfo.DEFAULT_QUANTITY; i++)
					{
						fs.Write(LogInfo.EmptyLogInfo);
					}
				}
				else//to EOF
				{
					fs.Seek(fs.Length, SeekOrigin.Begin);
					res = new FolderInfo(fs.Position, status, 0, _lastFolderinfoId, name, descr);
					res.WriteBodyToFile(bw);
				}
				_folders.Add(res);
			}
		}

		//public bool RemoveFolder(UInt16 id)
		//{
		//	for (int i = 0; i < _folders.Count; i++)
		//	{
		//		if (_folders[i].Id == id)
		//		{
		//			return true;
		//		}
		//	}
		//	return false;
		//}

		public ReadOnlyCollection<FolderInfo> Folders => _folders.AsReadOnly();
	}
}
