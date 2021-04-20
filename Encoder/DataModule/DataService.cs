using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using DataModule.Exceptions;
using DataModule.Models;

namespace DataModule
{
	public sealed class DataService : IDisposable
	{
		#region CONSTS
		internal const int DEFAULT_FOLDERS_COUNT = 16;

		internal const int BYTES_LASTLOGINFOID = sizeof(UInt32);
		internal const int BYTES_LASTFOLDERINFOID = 2;
		internal const int BYTES_FOLDERSCOUNT = 2;
		internal const int BYTES_EMPTYFOLDERSCOUNT = 2;

		internal const int BYTES_BODY = 128;
		internal const int BLOCKS_PER_BODY = BYTES_BODY / CryptService.BLOCK_SIZE;
		#endregion //CONSTS

		#region BODY FIELDS
		private UInt32 _lastLoginfoId = 0;
		private UInt16 _lastFolderinfoId = 0;
		private UInt16 FoldersCount => (ushort)_folders.Count;
		private UInt16 EmptyFoldersCount => (ushort)_emptyFolderPoses.Count;
		#endregion //BODY FIELDS

		#region APP FIELDS
		private readonly ObservableCollection<FolderInfo> _folders = new();
		private readonly Queue<long> _emptyFolderPoses = new(DEFAULT_FOLDERS_COUNT);

		private readonly EncodingService _encService = new(new UTF8Encoding(), 96, 192);
		private readonly CryptService _cryptService;

		private int _editFolderIndex = -1;

		private bool _disposedValue = false;
		#endregion APP FIELDS
		public bool InEditFolder => _editFolderIndex != -1;

		private FolderInfo _copyEditFolder = null;

		public ReadOnlyObservableCollection<FolderInfo> Folders => new(_folders);

		public DataService(string dataPath)
		{
			_cryptService = new CryptService(new(dataPath));
		}

		public void Init()
		{
			if (_cryptService.Length < BYTES_BODY)
			{
				UpdateBody();
				return;
			}
			_cryptService.PrepareReadBlocks(BLOCKS_PER_BODY);
			_lastLoginfoId = _cryptService.GetUInt32();
			_lastFolderinfoId = _cryptService.GetUInt16();
			int _foldersCountT = _cryptService.GetUInt16();
			int _emptyFoldersCountT = _cryptService.GetUInt16();
			long preFolderPos = 0;
			FolderInfo fi;
			while (_emptyFolderPoses.Count < _emptyFoldersCountT)
			{
				preFolderPos = _cryptService.Position;
				fi = ReadFolderInfoBody();
				if (fi == null)
				{
					_emptyFolderPoses.Enqueue(preFolderPos);
					_cryptService.Seek(preFolderPos + FolderInfo.BYTES_NULLFOLDER);
				}
				else
				{
					_folders.Add(fi);
					_cryptService.Seek(preFolderPos + fi.GetTotalByteLength());
				}
			}
			while (_folders.Count < _foldersCountT)
			{
				fi = ReadFolderInfoBody();
				_folders.Add(fi);
				_cryptService.Seek(fi.FilePos + fi.GetTotalByteLength());
			}
		}

		#region WRITE STRUCT
		private void WriteFolderInfoBody(FolderInfo fi)
		{
			fi.FilePos = _cryptService.Position;
			_cryptService.PrepareWriteBlocks(FolderInfo.BLOCKS_PER_BODY);
			_cryptService.Write((uint)fi.Status);
			_cryptService.Write(fi.Count);
			_cryptService.Write(fi.Id);
			_cryptService.Write(_encService.GetBytes(fi.Name, FolderInfo.BYTES_NAME));
			_cryptService.Write(_encService.GetBytes(fi.Name, FolderInfo.BYTES_DESCR));
			_cryptService.Write(fi.HMAC);
			_cryptService.FlushPreparedBlocks();
		}
		private void WriteLogInfo(LogInfo li)
		{
			li.FilePos = _cryptService.Position;
			_cryptService.PrepareWriteBlocks(LogInfo.BLOCKS_PER);
			_cryptService.Write(li.ID);
			_cryptService.Write((UInt16)li.Attributes);
			_cryptService.Write(_encService.GetBytes(li.Name, 40));
			_cryptService.Write(_encService.GetBytes(li.Description, 40));
			_cryptService.Write(li.DateCreated);
			_cryptService.Write(li.CryptedLogin);
			_cryptService.Write(li.CryptedPass);
			_cryptService.Write(li.HMAC);
			_cryptService.FlushPreparedBlocks();
		}
		private void WriteFolderInfoContent(FolderInfo fi)
		{
			int i = 0;
			foreach (var li in fi)
			{
				WriteLogInfo(li); i++;
			}
			for (; i < fi.Capacity; i++)
			{
				_cryptService.WriteAndFlush(LogInfo.EmptyLogInfo);
			}
			int nullbytes = fi.GetExtraNullBodysCount();
			for (i = 0; i < nullbytes; i++)
			{
				_cryptService.WriteAndFlush(FolderInfo.NullBody);
			}
		}
		#endregion //WRITE STRUCT
		#region READ STRUCT
		private FolderInfo ReadFolderInfoBody()
		{
			long pos = _cryptService.Position;
			_cryptService.PrepareReadBlocks(FolderInfo.BLOCKS_PER_BODY);
			StatusEnum status = (StatusEnum)_cryptService.GetUInt32();
			if ((status & StatusEnum.NULL) == StatusEnum.NULL) return null;
			return new FolderInfo(pos, status, _cryptService.GetUInt16(), _cryptService.GetUInt16(),
				_encService.GetString(_cryptService.GetBytes(FolderInfo.BYTES_NAME)),
				_encService.GetString(_cryptService.GetBytes(FolderInfo.BYTES_DESCR)),
				_cryptService.GetBytes(FolderInfo.BYTES_HMAC).ToArray());
		}
		public void ReadFolderInfoContent(FolderInfo fi, bool forceUpdate = false)
		{
			if (fi.IsCached)
			{
				if (!forceUpdate) return;
				else fi.ClearCache();
			}
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_BODY);
			var lis = fi.BeginCache();
			LogInfo li;
			int found = 0;
			while (found < fi.Count)
			{
				li = ReadLogInfo();

				if (li != null)
				{
					lis[found++] = li;
				}
			}
			fi.EndCache(found);
		}
		public bool TryReadFolderInfoContent(FolderInfo fi, string pass)
		{
			if (!fi.IsCrypted) throw new FolderNotCryptedException(fi);
			var folderkey = _cryptService.GetFolderKey(_encService.GetBytes(pass), fi.HMAC);
			if (folderkey == null) return false;
			fi.Key = folderkey;
			_cryptService.AddFolderCryptLayer(fi.Key);
			ReadFolderInfoContent(fi);
			_cryptService.RemoveFolderCryptLayer();
			return true;
		}
		private LogInfo ReadLogInfo()
		{
			long pos = _cryptService.Position;
			UInt32 id = _cryptService.GetUInt32();
			if (id < 1U) return null;
			return new LogInfo(pos, id, (LogInfoAttributes)_cryptService.GetUInt16(),
				_encService.GetString(_cryptService.GetBytes(LogInfo.BYTES_NAME)),
				_encService.GetString(_cryptService.GetBytes(LogInfo.BYTES_DESCR)),
				_cryptService.GetDateTime(),
				_cryptService.GetBytes(LogInfo.BYTES_CLOGIN).ToArray(), _cryptService.GetBytes(LogInfo.BYTES_CPASS).ToArray(),
				_cryptService.GetBytes(LogInfo.BYTES_HMAC).ToArray());
		}
		#endregion //READ STRUCT
		#region UPDATE EXISTING DATA
		private void UpdateBody()
		{
			_cryptService.Seek(0);
			_cryptService.PrepareWriteBlocks(BLOCKS_PER_BODY);
			_cryptService.Write(_lastLoginfoId);
			_cryptService.Write(_lastFolderinfoId);
			_cryptService.Write(FoldersCount);
			_cryptService.Write(EmptyFoldersCount);
			_cryptService.FlushPreparedBlocks();
		}
		private void UpdateFolderInfoBody(FolderInfo fi)
		{
			_cryptService.Seek(fi.FilePos);
			WriteFolderInfoBody(fi);
		}
		private void UpdateLogInfo(LogInfo li)
		{
			_cryptService.Seek(li.FilePos);
			WriteLogInfo(li);
		}
		#endregion //UPDATE EXISTING DATA
		#region FIND POS
		private long GetPosForFolderInfo(StatusEnum status)
		{
			return (status & StatusEnum.Normal) == StatusEnum.Normal || !_emptyFolderPoses.TryDequeue(out var pos) ? _cryptService.Length : pos;
		}
		private long GetPosForLogInfo(FolderInfo fi)
		{
			if (fi.IsFull) throw new FolderFullException(fi);
			_cryptService.Seek(fi.FilePos + FolderInfo.BYTES_BODY);
			long pos;
			for (int i = 0; i < fi.Capacity; i++)
			{
				pos = _cryptService.Position;
				_cryptService.PrepareReadBlocks(LogInfo.BLOCKS_PER);
				if (_cryptService.GetUInt32() == 0)
				{
					return pos;
				}
			}
			throw new ArgumentException("Cant find space for loginfo!", nameof(fi));
		}
		#endregion //FIND POS

		#region STRUCT PREADD
		public void PreaddFolderInfo()
		{
			_folders.Insert(0, new FolderInfo(StatusEnum.Normal, 0, ++this._lastFolderinfoId, string.Empty, string.Empty, null));
			BeginEditFolderInfoBody(0);
		}
		public void CancelPreaddFolderInfo()
		{
			if (_editFolderIndex == -1) throw new InvalidOperationException("No folders in edit mode!");
			_folders.RemoveAt(_editFolderIndex);
			_editFolderIndex = -1;
			_lastFolderinfoId--;
		}
		#endregion //STRUCT PREADD

		#region TOTAL REMOVE
		public void RemoveFolderInfo(int index)
		{
			var fi = _folders[index]; //get folder
			for (int i = 0; i < fi.SizeMultiplier; i++)
			{
				_emptyFolderPoses.Enqueue(fi.FilePos + (i * FolderInfo.BYTES_NULLFOLDER));
			} //add pos to empty folders
			fi.IsInited = false;
			fi.Status |= StatusEnum.NULL;
			fi.IsInited = true;
			UpdateFolderInfoBody(fi);
			_folders.RemoveAt(index);
			UpdateBody(); //update body(folders count, empty folders count)
		}
		public void RemoveLogInfo(FolderInfo fi, int index)
		{
			var li = fi[index]; //get item
			li.IsInited = false;
			li.ID = 0;
			li.IsInited = true;
			UpdateLogInfo(li);
			fi.RemoveAt(index); //remove loginfo from mem
		}
		#endregion //TOTAL REMOVE

		#region DATA EDIT
		public void BeginEditFolderInfoBody(int index)
		{
			if (InEditFolder) throw new FolderInEditModeException(_folders[_editFolderIndex]);
			var fi = _folders[index];
			if (fi.IsCrypted && !fi.HasKey) throw new FolderNotUnlockedException(fi);
			_copyEditFolder = fi.CreateUserBodyCopy();
			fi.IsInited = false;
			_editFolderIndex = index;
		}
		public void CancelEditFolderInfoBody()
		{
			if (!InEditFolder) throw new InvalidOperationException("No folders in edit mode!");
			_copyEditFolder.CopyUserBodyTo(_folders[_editFolderIndex]);
			_folders[_editFolderIndex].IsInited = true;
			_editFolderIndex = -1;
			_copyEditFolder = null;
		}
		public void EndEditFolderInfoBody(string newPass)
		{
			if (!InEditFolder) throw new InvalidOperationException("No folders in edit mode!");
			var fi = _folders[_editFolderIndex];
			bool passProvided = !string.IsNullOrEmpty(newPass);
			if (fi.FilePos == 0L)
			{
				_cryptService.Seek(GetPosForFolderInfo(fi.Status));
				if (passProvided)
				{
					fi.HMAC = _cryptService.CreateHMAC(_encService.GetBytes(newPass), out var key);
					fi.Key = key;
				}
				WriteFolderInfoBody(fi);
				if (passProvided)
				{
					_cryptService.AddFolderCryptLayer(fi.Key);
				}
				WriteFolderInfoContent(fi);
				if (passProvided)
				{
					_cryptService.RemoveFolderCryptLayer();
				}
				UpdateBody();
			}
			else
			{
				if (passProvided)
				{
					fi.HMAC = _cryptService.CreateHMAC(_encService.GetBytes(newPass), out var key);
					fi.Key = key;
				}
				UpdateFolderInfoBody(fi);
				if (passProvided)
				{
					_cryptService.AddFolderCryptLayer(fi.Key);
					WriteFolderInfoContent(fi);
					_cryptService.RemoveFolderCryptLayer();
				}
			}
			fi.IsInited = true;
			_editFolderIndex = -1;
			_copyEditFolder = null;
		}
		#endregion //DATA EDIT

		#region DISPOSE PATTERN
		private void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					((IDisposable)_cryptService).Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				_disposedValue = true;
			}
		}
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion //DISPOSE PATTERN
	}
}
