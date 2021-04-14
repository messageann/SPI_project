using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using DataModule.Exceptions;
using DataModule.Models;

namespace DataModule
{
	public class DataService : IDisposable
	{
		#region CONSTS
		internal const int DEFAULT_FOLDERS_COUNT = 16;

		internal const byte BYTES_LASTLOGINFOID = 2;
		internal const byte BYTES_LASTFOLDERINFOID = 2;
		internal const byte BYTES_FOLDERSCOUNT = 2;
		internal const byte BYTES_EMPTYFOLDERSCOUNT = 2;

		internal const byte BYTES_BODY = 100;
		#endregion //CONSTS

		#region BODY FIELDS
		private UInt16 _lastLoginfoId;
		private UInt16 _lastFolderinfoId;
		private UInt16 FoldersCount => (ushort)_folders.Count;
		private UInt16 EmptyFoldersCount => (ushort)_emptyFolderPoses.Count;

		#endregion //BODY FIELDS

		#region APP FIELDS
		private readonly FileInfo _dataFile;

		//private readonly List<FolderInfo> _folders;
		private readonly ObservableCollection<FolderInfo> _folders;
		private readonly Queue<long> _emptyFolderPoses;

		private readonly EncodingService _encService;
		private readonly CryptService _cryptService;
		#endregion APP FIELDS

		public ReadOnlyObservableCollection<FolderInfo> Folders => new(_folders);

		public DataService(string dataPath)
		{
			_dataFile = new FileInfo(dataPath);
			_emptyFolderPoses = new Queue<long>(DEFAULT_FOLDERS_COUNT);
			//_folders = new List<FolderInfo>(DEFAULT_FOLDERS_COUNT);
			_folders = new();
			_folders.Add(new(StatusEnum.Normal, 2, 2, "Name1", "Descr1")); //DEBUG
			//_folders.Add(new(StatusEnum.Normal, 3, 4, "Name 2", "Descr 2")); //DEBUG
			_encService = new EncodingService(new UTF8Encoding(), 96, 192);
			_cryptService = new CryptService(_dataFile, maxCryptBlock: 512);
		}

		public void Init()
		{
			_lastLoginfoId = _cryptService.ReadUInt16();
			_lastFolderinfoId = _cryptService.ReadUInt16();
			var _foldersCountT = _cryptService.ReadUInt16();
			var _emptyFoldersCountT = _cryptService.ReadUInt16();
			_cryptService.Seek(BYTES_BODY);

			//_folders.Capacity = _foldersCountT > 64 ? _foldersCountT.ToUpperPowerOf2() : 64;

			long preFolderPos;
			StatusEnum status;

			while (_emptyFolderPoses.Count < _emptyFoldersCountT)
			{
				preFolderPos = _cryptService.Position;
				status = (StatusEnum)_cryptService.ReadUInt32();
				if ((status & StatusEnum.NULL) != 0 || status == 0) //null folder
				{
					_emptyFolderPoses.Enqueue(preFolderPos);
					_cryptService.Seek(preFolderPos + FolderInfo.BYTES_NULLFOLDER);
				}
				else //non-null folder
				{
					_folders.Add(ReadFolderInfoBody(status));
				}
			}
			while (_folders.Count < _foldersCountT)
			{
				preFolderPos = _cryptService.Position;
				_folders.Add(ReadFolderInfoBody((StatusEnum)_cryptService.ReadUInt32()));
			}
		}

		#region DATA IO
		//read at current pos
		private FolderInfo ReadFolderInfoBody(StatusEnum status)
		{
			var res = new FolderInfo(_cryptService.Position - FolderInfo.BYTES_STATUS, status, _cryptService.ReadUInt16(), _cryptService.ReadUInt16(),
				_encService.GetString(_cryptService.ReadBytes(FolderInfo.BYTES_NAME)), _encService.GetString(_cryptService.ReadBytes(FolderInfo.BYTES_DESCR)));
			_cryptService.Seek(res.FilePos + res.GetTotalByteLength());
			return res;
		}
		private LogInfo ReadLogInfo(UInt16 id)
		{
			return new LogInfo(_cryptService.Position - LogInfo.BYTES_ID, id, (LogInfoAttributes)_cryptService.ReadUInt32(),
				_encService.GetString(_cryptService.ReadBytes(LogInfo.BYTES_NAME)), _encService.GetString(_cryptService.ReadBytes(LogInfo.BYTES_DESCR)),
				_cryptService.ReadDateTime(), _cryptService.ReadBytes(LogInfo.BYTES_CLOGIN).ToArray(), _cryptService.ReadBytes(LogInfo.BYTES_CPASS).ToArray());
		}

		//write at current pos
		private void WriteFolderInfo(FolderInfo fi)
		{
			fi.FilePos = _cryptService.Position;
			_cryptService.Write((UInt32)fi.Status);
			_cryptService.Write(fi.Count);
			_cryptService.Write(fi.Id);
			_cryptService.Write(_encService.GetBytes(fi.Name, FolderInfo.BYTES_NAME));
			_cryptService.Write(_encService.GetBytes(fi.Description, FolderInfo.BYTES_DESCR));
			_cryptService.FlushToFile();
			int i;
			for (i = 0; i < fi.Count; i++)
			{
				WriteLogInfo(fi[i]);
			}
			for (; i < fi.Capacity; i++)
			{
				_cryptService.Write(LogInfo.EmptyLogInfo); //this is slow, mb worth to try write big array instead of few smalls?
				_cryptService.FlushToFile();
			}
			var nulls = fi.GetExtraNullFoldersCount();
			for (i = 0; i < nulls; i++)
			{
				_cryptService.WriteThrough(FolderInfo.NullBody);
			}
		}
		private void WriteLogInfo(LogInfo li)
		{
			li.FilePos = _cryptService.Position;
			_cryptService.Write(li.Id);
			_cryptService.Write((UInt32)li.Attributes);
			_cryptService.Write(_encService.GetBytes(li.Name, LogInfo.BYTES_NAME));
			_cryptService.Write(_encService.GetBytes(li.Descr, LogInfo.BYTES_DESCR));
			_cryptService.Write(li.Date);
			_cryptService.Write(li.CLogin);
			_cryptService.Write(li.CPass);
			_cryptService.FlushToFile();
		}

		//find pos and write
		private void WriteNewFolderInfo(FolderInfo fi)
		{
			_cryptService.Seek(GetFreePosForFolderInfo(fi.Status));
			WriteFolderInfo(fi);
		}
		private void WriteNewLogInfo(FolderInfo fi, LogInfo li)
		{
			_cryptService.Seek(GetFreePosForLogInfo(fi));
			WriteLogInfo(li);
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_STATUS);
			_cryptService.Write((UInt16)(fi.Count + 1));
			_cryptService.FlushToFile();
		}

		//find pos and delete
		private void DeleteFolderInfo(FolderInfo fi, bool forceZeroMem = false)
		{
			_cryptService.Seek(fi.FilePos);
			if (forceZeroMem) //zero all folder
			{
				var totalzise = fi.GetTotalByteLength();
				Span<byte> buff = stackalloc byte[totalzise];
				_cryptService.WriteThrough(buff);
			}
			else //make status to NULL-folder
			{
				_cryptService.Write((UInt32)(fi.Status | StatusEnum.NULL));
				_cryptService.FlushToFile();
			}
		}
		private void DeleteLogInfo(FolderInfo fi, LogInfo li, bool forceZeroMem = false)
		{
			_cryptService.Seek(li.FilePos);
			if (forceZeroMem)
			{
				_cryptService.WriteThrough(LogInfo.EmptyLogInfo);
			}
			else
			{
				_cryptService.Write((UInt16)0);
				_cryptService.FlushToFile();
			}
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_STATUS);
			_cryptService.Write((UInt16)(fi.Count - 1));
			_cryptService.FlushToFile();
		}

		private long GetFreePosForLogInfo(FolderInfo fi)
		{
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_BODY);
			UInt16 id;
			long prepos;
			for (int i = 0; i < fi.Capacity; i++)
			{
				prepos = _cryptService.Position;
				id = _cryptService.ReadUInt16();
				if (id == 0) return prepos;
				_cryptService.Seek(prepos + LogInfo.BYTES_LOGINFO);
			}
			throw new ArgumentOutOfRangeException("fi");
		}
		private long GetFreePosForFolderInfo(StatusEnum status)
		{
			return (((status & StatusEnum.Normal) != 0) && _emptyFolderPoses.TryDequeue(out long res)) ? res : _cryptService.Length;
		}

		private void UpdateBody()
		{
			_cryptService.Seek(0);
			_cryptService.Write(_lastLoginfoId);
			_cryptService.Write(_lastFolderinfoId);
			_cryptService.Write(FoldersCount);
			_cryptService.Write(EmptyFoldersCount);
			_cryptService.FlushToFile();
		}
		#endregion //DATA IO

		#region DATA REG
		public void RegLogInfo(FolderInfo fi, string name, string descr, string login, string pass, CryptMethod cryptMethod)
		{
			if (fi.IsFull) throw new ArgumentOutOfRangeException("FolderInfo.Count", fi.Count, "Folder is full!");
			var li = new LogInfo(++_lastLoginfoId, cryptMethod.Attrs, name, descr, DateTime.UtcNow,
				_cryptService.Crypt(cryptMethod, _encService.GetBytes(login, LogInfo.BYTES_CLOGIN)).ToArray(),
				_cryptService.Crypt(cryptMethod, _encService.GetBytes(pass, LogInfo.BYTES_CPASS)).ToArray()); //create loginfo
			WriteNewLogInfo(fi, li); //write to file(also increment 'count' in folderinfo) with auto-pos
			fi.Add(li); //add to memory(will not be added if folder not cached)
			UpdateBody(); //update body(folders_count etc..)
		}
		public void RegFolderInfo(StatusEnum status, string name, string descr)
		{
			var fi = new FolderInfo(status, 0, ++_lastFolderinfoId, name, descr);
			WriteNewFolderInfo(fi); //write to file(find pos to fit folder)
			_folders.Add(fi); //add to memory
			UpdateBody(); //update body(last folder ID, folders count, <empty_folders count>?)
		}

		public void PreaddFolderInfo()
		{
			_folders.Insert(0, new(StatusEnum.Normal, 0, (ushort)(_lastFolderinfoId + 1), "", ""));
		}
		#endregion //DATA REG

		#region DATA GET
		public void ReadFolderInfoContent(FolderInfo fi, bool forceUpdate = false)
		{
			if (fi.IsCached)
			{
				if (!forceUpdate) throw new FolderCachedException(fi);
				else fi.ClearCache();
			}
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_BODY);
			var lis = fi.BeginCache();
			UInt16 id;
			UInt16 found = 0;
			while (found < fi.Count)
			{
				id = _cryptService.ReadUInt16();
				if (id == 0) _cryptService.Seek(_cryptService.Position + LogInfo.BYTES_LOGINFO - LogInfo.BYTES_ID);
				else lis[found++] = ReadLogInfo(id);
			}
			fi.EndCache(found);
		}
		#endregion //DATA GET

		#region DATA EDIT
		public void RemoveFolderInfo(int index, bool forceZeroMem = false)
		{
			var fi = _folders[index]; //get folder
			DeleteFolderInfo(fi, forceZeroMem); //remove folder from file
			_folders.RemoveAt(index); //remove folder from mem
			for (int i = 0; i < fi.SizeMultiplier; i++)
			{
				_emptyFolderPoses.Enqueue(fi.FilePos + (i * FolderInfo.BYTES_NULLFOLDER));
			} //add pos to empty folders
			UpdateBody(); //update body(folders count, empty folders count)
		}
		public void RemoveLogInfo(FolderInfo fi, int index, bool forceZeroMem = false)
		{
			var li = fi[index]; //get item
			DeleteLogInfo(fi, li, forceZeroMem); //remove loginfo from file and update folderinfo count prop
			fi.RemoveAt(index); //remove loginfo from mem
		}
		#endregion //DATA EDIT

		public void Dispose()
		{
			((IDisposable)_cryptService)?.Dispose();
		}
	}
}
