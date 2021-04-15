﻿using DataModule.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
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
		internal const int BYTES_NAME = 40;//dividable by 8
		internal const int BYTES_DESCR = 144;//dividable by 8
		internal const int BYTES_BODY = 0
			+ BYTES_STATUS
			+ BYTES_QUANTITY
			+ BYTES_ID
			+ BYTES_NAME
			+ BYTES_DESCR
			;

		internal const int BYTES_NULLFOLDER = BYTES_BODY + DEFAULT_QUANTITY * LogInfo.BYTES_LOGINFO;
		#endregion //CONSTS

		#region BODY FIELDS
		private readonly StatusEnum _status;

		private UInt16 _count;

		private readonly UInt16 _id;

		private string _name;

		private string _descr;

		private readonly LogInfo[] _logInfos;
		#endregion //BODY FIELDS

		#region APP FIELDS
		internal long FilePos;

		private int _liMemCount;
		private bool _cached;

		public StatusEnum Status
		{
			get
			{
				return _status;
			}
		}
		public UInt16 Count => _count; //count in file
		public UInt16 Id
		{
			get => _id;
		}
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (_isInited) throw new MemberAccessException("Cant edit inited folder!");
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
				if (_isInited) throw new MemberAccessException("Cant edit inited folder!");
				_descr = value;
				NotifyPropertyChanged();
			}
		}
		public bool IsCached => _cached;

		public bool IsFull => _count == _logInfos.Length;
		public int Capacity => _logInfos.Length;

		private bool _isInited;
		public bool IsInited
		{
			get => _isInited;
			set
			{
				_isInited = value;
				NotifyPropertyChanged();
			}
		}

		private readonly bool _isCrypted;
		public bool IsCrypted
		{
			get => _isCrypted;
		}

		internal readonly int SizeMultiplier;
		#endregion //APP FIELDS

		#region CONSTRUCTORS

		private FolderInfo()
		{
			_liMemCount = 0;
			_cached = false;
			_isInited = false;
		}

		internal FolderInfo(long filePos, StatusEnum status, UInt16 count, ushort id, string name, string descr) : this()
		{
			FilePos = filePos;
			_status = status;
			_count = count;
			_id = id;
			Name = name;
			Description = descr;
			IsInited = true;
			SizeMultiplier = (int)(status & AllLengths) / 2;

			_logInfos = new LogInfo[((int)status / 2 * DEFAULT_QUANTITY)];
			_isCrypted = (_status & StatusEnum.Crypted) == StatusEnum.Crypted;
		}

		internal FolderInfo(StatusEnum status, UInt16 count, ushort id, string name, string descr) : this(0, status, count, id, name, descr) { }
		#endregion //CONSTRUCTORS

		internal int GetExtraNullFoldersCount()
		{
			return SizeMultiplier - 1;
		}
		private static StatusEnum AllLengths => StatusEnum.Normal | StatusEnum.X2 | StatusEnum.X4 | StatusEnum.X8;
		internal int GetTotalByteLength() => BYTES_NULLFOLDER * SizeMultiplier;

		internal bool Add(LogInfo value)
		{
			if (IsFull || !_cached) return false;
			//_tail = (_tail + 1) % _logInfos.Length;
			//_logInfos[_tail] = value;
			_logInfos[_liMemCount++] = value;
			_count++;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, _liMemCount-1));
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
			_count--;
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

		internal void ClearCache()
		{
			Array.Clear(_logInfos, 0, _logInfos.Length);
			_liMemCount = 0;
			_cached = false;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		internal LogInfo[] BeginCache()
		{
			if (!_cached) return _logInfos;
			throw new FolderCachedException(this);
		}

		internal void EndCache(int count)
		{
			if (!_cached)
			{
				_liMemCount = count;
				_count = (ushort)count;
				_cached = true;
				foreach (var t in this)
				{
					NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t));
				}
			}
			else throw new FolderCachedException(this);
		}

		#region IEnumerable interface
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_logInfos, _liMemCount);
		private class Enumerator : IEnumerator
		{
			private readonly LogInfo[] _arr;
			private readonly int _count;
			//private int _head;
			//private int _tail;
			private int _pos;
			internal Enumerator(LogInfo[] arr, int count)
			{
				_arr = arr;
				_count = count;
				_pos = -1;
			}
			object IEnumerator.Current => Current;
			public LogInfo Current => _arr[_pos];
			bool IEnumerator.MoveNext()
			{
				_pos++;
				return (_pos < _count);
			}
			void IEnumerator.Reset()
			{
				_pos = -1;
			}
			//check which current will 'foreach' use
		}
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
		public FolderInfo CreateUserBodyCopy()
		{
			FolderInfo res = new(this.FilePos, this.Status, this.Count, this.Id, this.Name, this.Description);
			return res;
		}

		public void CopyUserBodyTo(FolderInfo dest)
		{
			dest.Name = this.Name;
			dest.Description = this.Description;
		}

		#region STATIC
		static FolderInfo()
		{
			NullBody[0] = 1;
		}
		internal static readonly byte[] NullBody = new byte[BYTES_BODY];
		#endregion //STATIC
	}

	[Flags]//redurant?
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
