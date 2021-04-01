using DataModule.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DataModule
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class FolderInfo : IList<LogInfo>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		#region CONSTS
		internal const int DEFAULT_QUANTITY = 64;

		internal const int BYTES_STATUS = 4;
		internal const int BYTES_QUANTITY = 2;
		private const int BYTES_ID = 2;
		private const int BYTES_NAME = 40;//dividable by 8
		private const int BYTES_DESCR = 144;//dividable by 8
		internal const int BYTES_BODY = 0
			+ BYTES_STATUS
			+ BYTES_QUANTITY
			+ BYTES_ID
			+ BYTES_NAME
			+ BYTES_DESCR
			;
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

		private readonly int _capacity;

		private int _head;

		private int _tail;

		public StatusEnum Status
		{
			get
			{
				return _status;
			}
		}

		public int Count
		{
			get => _count;
			private set
			{
				_count = (ushort)value;
				NotifyPropertyChanged();
			}
		}


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
			private set
			{
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
				_descr = value;
				NotifyPropertyChanged();
			}
		}

		public bool IsFull => Count == _capacity;
		public bool IsReadOnly => false;
		#endregion //APP FIELDS

		#region CONSTRUCTORS
		public FolderInfo(long filePos, StatusEnum status, int count, ushort id, string name, string descr)
		{
			_status = status;
			_id = id;
			Name = name;
			Description = descr;
			Count = count;
			_logInfos = new LogInfo[((int)status / 2 * DEFAULT_QUANTITY)];
			FilePos = filePos;
			_head = 0;
			_tail = 0;
			_capacity = DEFAULT_QUANTITY * StatusToMulti();
		}
		#endregion //CONSTRUCTORS

		//#region IO
		//#region INPUT
		//public static FolderInfo ReadBodyFromFile(BinaryReader br)
		//{
		//	var res = new FolderInfo(br.BaseStream.Position, (StatusEnum)br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(),
		//		Encoding.Default.GetString(br.ReadBytes(BYTES_NAME)), Encoding.Default.GetString(br.ReadBytes(BYTES_DESCR)));
		//	br.BaseStream.Seek(br.BaseStream.Position + res._logInfos.Capacity * LogInfo.BYTES_LOGINFO + res.GetNullBytes(), SeekOrigin.Begin);
		//	return res;
		//}

		//public void ReadLoginfosFromFile(BinaryReader br)
		//{
		//	while (_logInfos.Count < Quantity)
		//	{
		//		var li = LogInfo.ReadFromFile(br);
		//		if (li is not null)
		//		{
		//			_logInfos.Add(li);
		//			li.Parent = this;
		//		}

		//	}
		//}
		//#endregion //INPUT

		//#region OUTPUT
		//public static void WriteNullFoldersToFile(BinaryWriter bw, uint count)
		//{
		//	for (int y = 0; y < count; y++)
		//	{
		//		bw.Write(NullBody);
		//		for (int i = 0; i < DEFAULT_QUANTITY; i++)
		//		{
		//			bw.Write(LogInfo.EmptyLogInfo);
		//		}
		//	}
		//}

		//public void WriteBodyToFile(BinaryWriter bw)
		//{
		//	bw.Write((UInt32)Status);
		//	bw.Write(Quantity);
		//	bw.Write(Id);
		//	byte[] nameb = new byte[BYTES_NAME];
		//	Encoding.Default.GetBytes(Name, 0, Name.Length.EqualOrLess(BYTES_NAME * 2), nameb, 0);
		//	bw.Write(nameb);

		//	byte[] descrb = new byte[BYTES_DESCR];
		//	Encoding.Default.GetBytes(Descr, 0, Descr.Length.EqualOrLess(BYTES_DESCR * 2), descrb, 0);
		//	bw.Write(descrb);
		//	for (int i = 0; i < _logInfos.Capacity; i++)
		//	{
		//		bw.Write(LogInfo.EmptyLogInfo);
		//	}
		//	var nb = GetNullBytes();
		//	if (nb > 0)
		//	{
		//		byte[] nullb = new byte[nb];
		//		bw.Write(nullb);
		//	}
		//}

		///// <summary>
		///// Save loginfo to file and add it to folder.
		///// </summary>
		///// <param name="bw">Stream to write</param>
		///// <param name="br">Stream to read(must be the same as write)</param>
		///// <param name="logInfo">Loginfo to add</param>
		///// <returns>FolderFullException if folder full.</returns>
		//internal void AddNewLoginfo(BinaryWriter bw, BinaryReader br, LogInfo logInfo)
		//{
		//	if (this.IsFull) throw new FolderFullException(this);
		//	br.BaseStream.Seek(this.FilePos + FolderInfo.BYTES_STATUS, SeekOrigin.Begin);
		//	bw.Write(++this.Quantity);
		//	br.BaseStream.Seek(this.FilePos + FolderInfo.BYTES_BODY, SeekOrigin.Begin);
		//	while (br.ReadUInt16() != 0)
		//	{
		//		br.BaseStream.Seek(br.BaseStream.Position + LogInfo.BYTES_LOGINFO - LogInfo.BYTES_ID, SeekOrigin.Begin);
		//	}
		//	br.BaseStream.Seek(br.BaseStream.Position - LogInfo.BYTES_ID, SeekOrigin.Begin);
		//	logInfo.WriteToFile(bw);
		//	this._logInfos.Add(logInfo);
		//}

		//internal void RemoveLoginfo(BinaryWriter bw, UInt16 logInfoId)
		//{
		//	var li = this[logInfoId];
		//	if (li == null) throw new FolderNotCachedException(this);
		//	bw.BaseStream.Seek(li.FilePos, SeekOrigin.Begin);
		//	bw.Write((UInt16)0);
		//}
		//#endregion //OUTPUT
		//#endregion //IO

		private int GetNullBytes()
		{
			return (((int)this.Status / 2) - 1) * BYTES_BODY;
		}

		internal int StatusToMulti() => (int)((UInt32)Status / 2);

		public int IndexOf(LogInfo item)
		{
			for (int i = 0; i < _logInfos.Length; i++)
			{
				if (item.Id == _logInfos[i].Id) return i;
			}
			return -1;
		}

		void IList<LogInfo>.RemoveAt(int index)
		{
			if (index >= Count) throw new IndexOutOfRangeException();
			int r = FakeToReal(index);
			LogInfo rem = _logInfos[r];
			if (index >= Count / 2)
			{
				int fakeI = index;
				while (fakeI < Count - 1)
				{
					_logInfos[FakeToReal(fakeI)] = _logInfos[FakeToReal(++fakeI)];
				}
				_tail = (_tail + _capacity - 1) % _capacity;
			}
			else
			{
				int fakeI = index;
				while (fakeI > 0)
				{
					_logInfos[FakeToReal(fakeI)] = _logInfos[FakeToReal(--fakeI)];
				}
				_head = (_head + 1) % _capacity;
			}

			Count--;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, rem, index));
		}
		void IList<LogInfo>.Insert(int index, LogInfo item)
		{
			if (IsFull) throw new FolderFullException(this);
			if (index > Count) throw new IndexOutOfRangeException();
			if (index >= Count / 2)
			{
				int i = Count;
				while (i > index)
				{
					_logInfos[FakeToReal(i)] = _logInfos[FakeToReal(--i)];
				}
				_tail = (_tail + 1) % _capacity;
			}
			else
			{
				_head = (_head + _capacity - 1) % _capacity;
				int i = 1;
				while (i < index)
				{
					_logInfos[FakeToReal(i)] = _logInfos[FakeToReal(++i)];
				}
			}
			_logInfos[FakeToReal(index)] = item;
			Count++;
			NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		private int FakeToReal(int index) => (_head + index) % _capacity;

		public void Add(LogInfo item)
		{
			((IList<LogInfo>)this).Insert(Count, item);
		}

		bool ICollection<LogInfo>.Remove(LogInfo item)
		{
			int index = IndexOf(item);
			if (index == -1) return false;
			((IList<LogInfo>)this).RemoveAt(IndexOf(item));
			return true;
		}
		void ICollection<LogInfo>.Clear()
		{
			for (int i = 0; i < Count; i++)
			{
				_logInfos[FakeToReal(i)] = null;
			}
			_head = 0;
			_tail = 0;
			Count = 0;
		}

		LogInfo IList<LogInfo>.this[int index]
		{
			get => _logInfos[FakeToReal(index)];
			set
			{
				var ri = FakeToReal(index);
				var old = _logInfos[ri];
				_logInfos[ri] = value;
				NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
			}
		}
		#region UNIMPL!!




		public bool Contains(LogInfo item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(LogInfo[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}


		public IEnumerator<LogInfo> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion //UNIMPL!!

		#region NOTIFS
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		private void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}
		#endregion //NOTIFS

		#region STATIC
		static FolderInfo()
		{
			NullBody = new byte[BYTES_BODY];
			NullBody[0] = 1;
		}
		private static readonly byte[] NullBody;
		#endregion //STATIC
	}

	public enum StatusEnum : UInt32
	{
	}
