using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
		private UInt16 _foldersCount;
		private UInt16 _emptyFoldersCount;

		#endregion //BODY FIELDS

		#region APP FIELDS
		private readonly FileInfo _dataFile;
		//private FileStream _DFS;
		private readonly List<FolderInfo> _folders;
		private readonly Queue<long> _emptyFolderPoses;

		//private readonly byte[] _bufferSmallConverting;
		//private readonly byte[] _bufferIO;
		private readonly EncodingService _encService;
		private readonly CryptService _cryptService;
		#endregion APP FIELDS

		public DataService(string dataPath)
		{
			_dataFile = new FileInfo(dataPath);
			_emptyFolderPoses = new Queue<long>(DEFAULT_FOLDERS_COUNT);
			_folders = new List<FolderInfo>(DEFAULT_FOLDERS_COUNT);
			//_bufferSmallConverting = new byte[8];//8 for 64 bit values
			//_bufferIO = new byte[192];//find min of maxes
			_encService = new EncodingService(new UTF8Encoding(), 96, 192);
			_cryptService = new CryptService(_dataFile, maxCryptBlock: 512);
		}

		public void Init()
		{
			_lastLoginfoId = _cryptService.ReadUInt16Core();
			_lastFolderinfoId = _cryptService.ReadUInt16Core();
			_foldersCount = _cryptService.ReadUInt16Core();
			_emptyFoldersCount = _cryptService.ReadUInt16Core();
			_cryptService.Seek(BYTES_BODY);

			_folders.Capacity = _foldersCount.ToUpperPowerOf2();

			long preFolderPos;
			StatusEnum status;

			while (_emptyFolderPoses.Count < _emptyFoldersCount)
			{
				preFolderPos = _cryptService.Position;
				status = (StatusEnum)_cryptService.ReadUInt32Core();
				if ((status & StatusEnum.NULL) != 0) //null folder
				{
					_emptyFolderPoses.Enqueue(preFolderPos);
					_cryptService.Seek(preFolderPos + FolderInfo.BYTES_NULLFOLDER);
				}
				else //non-null folder
				{
					_folders.Add(ReadFolderInfoFromFile(status));
				}
			}
			while (_folders.Count < _foldersCount)
			{
				preFolderPos = _cryptService.Position;
				_folders.Add(ReadFolderInfoFromFile((StatusEnum)_cryptService.ReadUInt32Core()));
			}
		}

		//#region CORE IO
		////private unsafe void WriteCore(UInt16 value)
		////{
		////	fixed(byte* bp = _bufferSmallConverting)
		////	{
		////		*((UInt16*)bp) = value;
		////	}
		////	_mem.Write(_bufferSmallConverting, 0, 2);
		////}
		////private unsafe void WriteCore(Int32 value)
		////{
		////	fixed (byte* bp = _bufferSmallConverting)
		////	{
		////		*((Int32*)bp) = value;
		////	}
		////	_mem.Write(_bufferSmallConverting, 0, 4);
		////}
		////private unsafe void WriteCore(UInt32 value)
		////{
		////	fixed (byte* bp = _bufferSmallConverting)
		////	{
		////		*((UInt32*)bp) = value;
		////	}
		////	_mem.Write(_bufferSmallConverting, 0, 4);
		////}
		////private unsafe void WriteCore(DateTime value)
		////{
		////	fixed(byte* bp = _bufferSmallConverting)
		////	{
		////		*((Int64*)bp) = value.ToBinary();
		////	}
		////	_mem.Write(_bufferSmallConverting, 0, 8);
		////}

		//private unsafe UInt16 ReadUInt16Core()
		//{
		//	_DFS.Read(_bufferSmallConverting, 0, 4);
		//	fixed (byte* bp = _bufferSmallConverting)
		//	{
		//		return *((UInt16*)bp);
		//	}
		//}
		//private unsafe int ReadInt32Core()
		//{
		//	_DFS.Read(_bufferSmallConverting, 0, 4);
		//	fixed (byte* bp = _bufferSmallConverting)
		//	{
		//		return *((Int32*)bp);
		//	}
		//}
		//private unsafe UInt32 ReadUInt32Core()
		//{
		//	_DFS.Read(_bufferSmallConverting, 0, 4);
		//	fixed (byte* bp = _bufferSmallConverting)
		//	{
		//		return *((UInt32*)bp);
		//	}
		//}
		//private unsafe DateTime ReadDateTimeCore()
		//{
		//	_DFS.Read(_bufferSmallConverting, 0, 8);
		//	fixed (byte* bp = _bufferSmallConverting)
		//	{
		//		return DateTime.FromBinary(*((long*)bp));
		//	}
		//}

		//private Span<byte> ReadBytes(int count)
		//{
		//	var res = new Span<byte>(_bufferIO, 0, count);
		//	_DFS.Read(res);
		//	return res;
		//}
		//#endregion //CORE IO

		#region DATA IO
		private FolderInfo ReadFolderInfoFromFile(StatusEnum status)
		{
			var res = new FolderInfo(_cryptService.Position - FolderInfo.BYTES_STATUS, status, _cryptService.ReadUInt16Core(), _cryptService.ReadUInt16Core(),
				_encService.GetString(_cryptService.ReadBytes(FolderInfo.BYTES_NAME)), _encService.GetString(_cryptService.ReadBytes(FolderInfo.BYTES_DESCR)));
			_cryptService.Seek(res.FilePos + res.GetTotalByteLength());
			return res;
		}

		//at current pos
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
		private void WriteNewLogInfo(FolderInfo fi, LogInfo li)
		{
			_cryptService.Seek(GetFreePosForLogInfo(fi));
			WriteLogInfo(li);
		}

		//at current pos
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
				_cryptService.Write(LogInfo.EmptyLogInfo);
				_cryptService.FlushToFile();
			}
		}
		private void WriteNewFolderInfo(FolderInfo fi)
		{
			_cryptService.Seek(GetFreePosForFolderInfo());
			WriteFolderInfo(fi);
		}

		private long GetFreePosForLogInfo(FolderInfo fi)
		{
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_BODY);
			UInt16 id;
			long prepos;
			for (int i = 0; i < fi.Capacity; i++)
			{
				prepos = _cryptService.Position;
				id = _cryptService.ReadUInt16Core();
				if (id == 0) return prepos;
				_cryptService.Seek(prepos + LogInfo.BYTES_LOGINFO);
			}
			throw new ArgumentOutOfRangeException("fi");
		}
		private long GetFreePosForFolderInfo()
		{
			if (_emptyFolderPoses.TryDequeue(out var res)) return res;
			return _cryptService.Length;
		}
		#endregion //DATA IO

		#region DATA REG
		//public Task RegLogInfo(FolderInfo fi, string name, string descr, string login, string pass, CryptMethod cryptMethod)
		//{
		//	if (fi.IsFull) throw new ArgumentOutOfRangeException();
		//	var pos = GetFreePosForLogInfo(fi);
		//	var li = new LogInfo(pos, ++_lastLoginfoId, cryptMethod.Attrs, name, descr, DateTime.UtcNow,
		//		_encService.GetBytes(login, LogInfo.BYTES_CLOGIN), _encService.GetBytes(pass, LogInfo.BYTES_CPASS).ToArray);
		//	var iotask = Task.Run(() => WriteNewLogInfo(fi, new LogInfo(pos, )));
		//	fi.Add(li);
		//	return iotask;
		//}
		//public Task RegFolderInfo(StatusEnum status, string name, string descr)
		//{
		//	//var iotask = 
		//}
		#endregion //DATA REG

		public void Dispose()
		{
			((IDisposable)_cryptService)?.Dispose();
		}
	}
}
