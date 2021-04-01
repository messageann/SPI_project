using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace DataModule
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

		private readonly List<FolderInfoCore> _folders;
		#endregion fields


		public DataService(string dataPath)
		{
			_dataPath = dataPath;
			_emptyFolderPoses = new Queue<long>(DEFAULT_FOLDERS_COUNT);
			_folders = new List<FolderInfoCore>(DEFAULT_FOLDERS_COUNT);
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
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.None, FolderInfoCore.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryReader br = new BinaryReader(fs))
			{
				_lastLoginfoId = br.ReadUInt16();
				_lastFolderinfoId = br.ReadUInt16();
				_foldersCount = br.ReadUInt16();
				_emptyFoldersCount = br.ReadUInt16();
				fs.Seek(_bytesPerExtradata, SeekOrigin.Begin);
				long preFolderPos;
				UInt32 status;
				byte[] buffer = new byte[FolderInfoCore.BYTES_BODY];
				_folders.Capacity = _foldersCount.ToTheUpperPowerOf2();
				while (_emptyFolderPoses.Count < _emptyFoldersCount)
				{
					preFolderPos = fs.Position;
					status = br.ReadUInt32();
					if ((status & (uint)StatusEnum.NULL) == 1)//null folder
					{
						_emptyFolderPoses.Enqueue(preFolderPos);
						fs.Seek(preFolderPos + FolderInfoCore.BYTES_BODY + FolderInfoCore.DEFAULT_QUANTITY * LogInfo.EmptyLogInfo.Length, SeekOrigin.Begin);
					}
					else//non null folder
					{
						fs.Seek(preFolderPos, SeekOrigin.Begin);
						_folders.Add(FolderInfoCore.ReadBodyFromFile(br));
					}
				}
				while (_folders.Count < _foldersCount)
				{
					preFolderPos = fs.Position;
					_folders.Add(FolderInfoCore.ReadBodyFromFile(br));
				}
			}
		}

		public void AddNewFolder(StatusEnum status, string name, string descr)
		{
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Write, FileShare.None, FolderInfoCore.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryWriter bw = new BinaryWriter(fs))
			{
				fs.Seek(_bytesPerLastLoginfoId, SeekOrigin.Begin);
				bw.Write(++_lastFolderinfoId);
				bw.Write(++_foldersCount);
				FolderInfoCore res;
				if (_emptyFolderPoses.Count > 0 && status == StatusEnum.Normal)//if open space exists
				{
					fs.Seek(_emptyFolderPoses.Dequeue(), SeekOrigin.Begin);
					res = new FolderInfoCore(fs.Position, status, 0, _lastFolderinfoId, name, descr);
					res.WriteBodyToFile(bw);
					for (int i = 0; i < FolderInfoCore.DEFAULT_QUANTITY; i++)
					{
						fs.Write(LogInfo.EmptyLogInfo);
					}
				}
				else//to EOF
				{
					fs.Seek(fs.Length, SeekOrigin.Begin);
					res = new FolderInfoCore(fs.Position, status, 0, _lastFolderinfoId, name, descr);
					res.WriteBodyToFile(bw);
				}
				_folders.Add(res);
			}
		}

		public bool RemoveFolder(UInt16 id)
		{
			for (int i = 0; i < _folders.Count; i++)
			{
				if (_folders[i].Id == id)
				{
					var fi = _folders[i];
					using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Write, FileShare.None, FolderInfoCore.BYTES_BODY, FileOptions.SequentialScan))
					using (BinaryWriter bw = new BinaryWriter(fs))
					{
						fs.Seek(_bytesPerLastLoginfoId + _bytesPerLastFolderinfoId, SeekOrigin.Begin);
						bw.Write(--_foldersCount);
						_emptyFoldersCount += (ushort)fi.StatusToMulti();
						bw.Write(_emptyFoldersCount);
						fs.Seek(fi.FilePos, SeekOrigin.Begin);
						if (fi.Status == StatusEnum.Normal)
						{
							bw.Write((UInt32)StatusEnum.NULL);
						}
						else
						{
							FolderInfoCore.WriteNullFoldersToFile(bw, fi.StatusToMulti());
						}
					}
					return true;
				}
			}
			return false;
		}

		public bool AddNewLogInfo(FolderInfoCore fi, string name, string descr, string clogin, string cpass)
		{
			if (fi._logInfos.Count == fi._logInfos.Capacity) return false;
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, FolderInfoCore.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryWriter bw = new BinaryWriter(fs))
			using (BinaryReader br = new BinaryReader(fs))
			{
				bw.Write(++_lastLoginfoId);
				fs.Seek(fi.FilePos + FolderInfoCore.BYTES_STATUS, SeekOrigin.Begin);
				bw.Write(++fi.Quantity);
				fs.Seek(fi.FilePos + FolderInfoCore.BYTES_BODY, SeekOrigin.Begin);
				while (br.ReadUInt16() != 0)
				{
					fs.Seek(fs.Position + LogInfo.BYTES_LOGINFO - LogInfo.BYTES_ID, SeekOrigin.Begin);
				}
				fs.Seek(fs.Position - LogInfo.BYTES_ID, SeekOrigin.Begin);
				LogInfo res = new LogInfo(fs.Position, _lastLoginfoId, name, descr, DateTime.UtcNow, clogin, cpass);
				res.WriteToFile(bw);
				fi._logInfos.Add(res);
			}
			return true;
		}

		public bool RemoveLogInfo(FolderInfoCore fi, UInt16 id)
		{
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Write, FileShare.None, FolderInfoCore.BYTES_BODY, FileOptions.SequentialScan))
			using (BinaryWriter bw = new BinaryWriter(fs))
			{
				var li = fi[id];
				if (li == null) return false;
				fs.Seek(fi.FilePos + FolderInfoCore.BYTES_STATUS, SeekOrigin.Begin);
				bw.Write(--fi.Quantity);
				fs.Seek(li.FilePos, SeekOrigin.Begin);
				bw.Write((UInt16)0);
				return true;
			}
		}

		public void ReadFolderContent(FolderInfoCore fi)
		{
			using (var fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.None, LogInfo.BYTES_LOGINFO, FileOptions.SequentialScan))
			using (BinaryReader br = new BinaryReader(fs))
			{
				fs.Seek(fi.FilePos + FolderInfoCore.BYTES_BODY, SeekOrigin.Begin);
				fi.ReadLoginfosFromFile(br);
			}
		}

		public ReadOnlyCollection<FolderInfoCore> Folders => _folders.AsReadOnly();
	}
}
