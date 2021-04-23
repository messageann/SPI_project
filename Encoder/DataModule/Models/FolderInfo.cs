using DataModule.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataModule.Models
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class FolderInfo : IEnumerable, INotifyCollectionChanged, INotifyPropertyChanged
	{
		#region CONSTS
		internal const int DEFAULT_QUANTITY = 64;

		internal const int BYTES_STATUS = 4;
		internal const int BYTES_QUANTITY = 2;
		internal const int BYTES_ID = 2;
		internal const int BYTES_NAME = 40;
		internal const int BYTES_DESCR = 112;
		internal const int BYTES_HMAC = 32;
		internal const int BYTES_BODY = 0
			+ BYTES_STATUS
			+ BYTES_QUANTITY
			+ BYTES_ID
			+ BYTES_NAME
			+ BYTES_DESCR
			+ BYTES_HMAC
			;

		internal const int BYTES_NULLFOLDER = BYTES_BODY + DEFAULT_QUANTITY * LogInfo.BYTES_LOGINFO;

		internal const int BLOCKS_PER_BODY = BYTES_BODY / CryptService.BLOCK_SIZE;

		internal const int BYTES_KEY = 256 / 8;
		#endregion //CONSTS

		#region BODY FIELDS
		private StatusEnum _status;
		private UInt16 _count;
		private readonly UInt16 _id;
		private string _name;
		private string _descr;
		private byte[] _hmac;
		private readonly LogInfo[] _logInfos;
		#endregion //BODY FIELDS

		#region BODY PROPS
		public StatusEnum Status
		{
			get
			{
				return _status;
			}
			internal set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_status = value;
				IsCrypted = (_status & StatusEnum.Crypted) == StatusEnum.Crypted;
				NotifyPropertyChanged();
			}
		}
		public UInt16 Count
		{
			get => _count;
			set
			{
				_count = value;
				NotifyPropertyChanged();
			}
		}
		public UInt16 Id => _id;
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (_isInited) throw new InitModeException();
				_name = value;
				NotifyPropertyChanged();
			}
		}
		public string Description
		{
			get
			{
				return _descr;
			}
			set
			{
				if (_isInited) throw new InitModeException();
				_descr = value;
				NotifyPropertyChanged();
			}
		}
		internal byte[] HMAC
		{
			get => _hmac;
			set
			{
				if (_isInited) throw new InitModeException();
				_hmac = value;
				if(_hmac != CryptService.Empty256Bits)
				{
					Status |= StatusEnum.Crypted;
				}
				else
				{
					Status &= ~StatusEnum.Crypted;
				}
			}
		}
		#endregion //BODY PROPS

		#region APP FIELDS
		internal long FilePos;
		private int _liMemCount = 0;
		private bool _isCached = false;
		private bool _isInited = false;
		private bool _isCrypted;
		internal readonly int SizeMultiplier;
		internal byte[] _key = CryptService.Empty256Bits;
		private bool _hasKey = false;
		#endregion //APP FIELDS

		#region APP PROPS
		public bool IsCached
		{
			get => _isCached;
			private set
			{
				_isCached = value;
				NotifyPropertyChanged();
			}
		}
		public bool IsFull => _count == _logInfos.Length;
		public int Capacity => _logInfos.Length;

		public bool IsInited
		{
			get => _isInited;
			set
			{
				_isInited = value;
				NotifyPropertyChanged();
			}
		}
		public bool IsCrypted
		{
			get => _isCrypted;
			private set
			{
				_isCrypted = value;
				NotifyPropertyChanged();
			}
		}
		internal byte[] Key
		{
			get => _key;
			set
			{
				_key = value;
				HasKey = _key != CryptService.Empty256Bits;
			}
		}
		public bool HasKey
		{
			get => _hasKey;
			private set
			{
				_hasKey = value;
				NotifyPropertyChanged();
			}
		}
		#endregion //APP PROPS

		#region CONSTRUCTORS
		internal FolderInfo(long filePos, StatusEnum status, UInt16 count, ushort id, string name, string descr, byte[] hmac)
		{
			FilePos = filePos;
			Status = status;
			Count = count;
			_id = id;
			Name = name;
			Description = descr;
			HMAC = (hmac == null || CryptService.BytesEqual(hmac, CryptService.Empty256Bits)) ? CryptService.Empty256Bits : hmac;
			IsInited = true;

			SizeMultiplier = (int)(status & AllLengths) / 2;
			_logInfos = new LogInfo[SizeMultiplier * DEFAULT_QUANTITY];

		}

		internal FolderInfo(StatusEnum status, UInt16 count, ushort id, string name, string descr, byte[] hmac) : this(0, status, count, id, name, descr, hmac) { }
		#endregion //CONSTRUCTORS

		#region LENGTH HELPERS
		internal int GetExtraNullBodysCount()
		{
			return SizeMultiplier - 1;
		}
		internal int GetTotalByteLength() => BYTES_NULLFOLDER * SizeMultiplier;
		#endregion //LENGTH HELPERS

		#region LIST FUNCS
		internal bool Add(LogInfo value)
		{
			if (IsFull || !IsCached) return false;
			//_tail = (_tail + 1) % _logInfos.Length;
			//_logInfos[_tail] = value;
			_logInfos[_liMemCount++] = value;
			Count++;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, _liMemCount - 1));
			return true;
		}

		internal bool Prepand(LogInfo value)
		{
			if (IsFull || !IsCached) return false;
			int i = _liMemCount;
			while (i > 0)
			{
				_logInfos[i] = _logInfos[--i];
			}
			_logInfos[0] = value;
			Count++;
			_liMemCount++;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, 0));
			return true;
		}

		internal void RemoveAt(int index)
		{
			var it = _logInfos[index];
			if (index >= _liMemCount) throw new IndexOutOfRangeException();
			_liMemCount--;
			if (index < _liMemCount)
			{
				Array.Copy(_logInfos, index + 1, _logInfos, index, _liMemCount - index);
			}
			_logInfos[_liMemCount] = null;
			Count--;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, it, index));
		}

		public LogInfo this[int index]
		{
			get
			{
				//if (!_cached) return null;
				if (index >= _liMemCount) throw new IndexOutOfRangeException();
				return _logInfos[index];
				//return _logInfos[(_head + index) % _logInfos.Length];
			}
		}
		#endregion LIST FUNCS

		#region CACHE
		public void ClearCache()
		{
			foreach(var i in this)
			{
				i.ClearCache();
			}
			Array.Clear(_logInfos, 0, _logInfos.Length);
			_liMemCount = 0;
			IsCached = false;
			Key = CryptService.Empty256Bits;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		internal LogInfo[] BeginCache()
		{
			if (!IsCached) return _logInfos;
			throw new FolderCachedException(this);
		}

		internal void EndCache(int count)
		{
			if (!IsCached)
			{
				_liMemCount = count;
				Count = (ushort)count;
				IsCached = true;
				foreach (var t in this)
				{
					NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t));
				}
			}
			else throw new FolderCachedException(this);
		}
		#endregion //CACHE

		#region IEnumerable interface
		public class Enumerator : IEnumerator
		{
			private readonly LogInfo[] _arr;
			private readonly int _count;
			private int _pos;
			internal Enumerator(LogInfo[] arr, int count)
			{
				_arr = arr;
				_count = count;
				_pos = -1;
			}
			object IEnumerator.Current => Current;
			public LogInfo Current => _arr[_pos];
			public bool MoveNext()
			{
				_pos++;
				return (_pos < _count);
			}
			public void Reset()
			{
				_pos = -1;
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public Enumerator GetEnumerator() => new Enumerator(_logInfos, _liMemCount);
		#endregion //IEnumerable interface

		#region NOTIFS
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		private void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}
		#endregion //NOTIFS

		#region BODY COPY
		public FolderInfo CreateUserBodyCopy()
		{
			FolderInfo res = new(this.FilePos, this.Status, this.Count, this.Id, this.Name, this.Description, HMAC);
			res.Key = this.Key;
			return res;
		}

		public void CopyUserBodyTo(FolderInfo dest)
		{
			dest.Status = this.Status;
			//dest._hmac = this._hmac;// ??
			dest.Name = this.Name;
			dest.Description = this.Description;
			dest.Key = this.Key;
		}
		#endregion //COPY

		#region STATIC
		static FolderInfo()
		{
			NullBody[0] = 1;
		}
		internal static readonly byte[] NullBody = new byte[BYTES_BODY];
		private static StatusEnum AllLengths => StatusEnum.Normal | StatusEnum.X2 | StatusEnum.X4 | StatusEnum.X8;
		#endregion //STATIC

		public override string ToString()
		{
			return this.Name;
		}
	}

	[Flags]//redurant? //pohody net
	public enum StatusEnum : UInt32
	{
		NULL = 1U << 0,
		Normal = 1U << 1,
		X2 = 1U << 2,
		X4 = 1U << 3,
		X8 = 1U << 4,
		Crypted = 1U << 10
	}
}
