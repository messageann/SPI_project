using DataModule.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModule
{
	internal class CryptService : IDisposable
	{
		private FileStream _DFS;
		private MemoryStream _mem;
		private readonly Memory<byte> _bufferSmallConverting;
		private readonly Memory<byte> _bufferRead;

		internal CryptService(FileInfo fileInfo, int maxCryptBlock)
		{
			_mem = new MemoryStream(new byte[maxCryptBlock]);
			if (!fileInfo.Exists)
			{
				_DFS = new FileStream(fileInfo.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
				_DFS.SetLength(DataService.BYTES_BODY); //make init crypt!
			}
			else
			{
				_DFS = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
			}
			_bufferSmallConverting = new byte[8];//8 for 64 bit values
			_bufferRead = new byte[maxCryptBlock];
		}

		internal long Position => _DFS.Position;

		internal long Length => _DFS.Length;

		internal void Seek(long dest) => _DFS.Seek(dest, SeekOrigin.Begin);

		private void CryptInternal(Span<byte> value)
		{
			//implement
		}
		private void DecryptInternal(Span<byte> value)
		{
			//implement
		}

		internal void Write(Span<byte> value)
		{
			CryptInternal(value);
			_mem.Write(value);
		}

		internal unsafe void Write(UInt16 value)
		{
			fixed (byte* bp = _bufferSmallConverting.Span)
			{
				*((UInt16*)bp) = value;
			}
			this.Write(_bufferSmallConverting.Span.Slice(0, 2));
		}
		internal unsafe void Write(Int32 value)
		{
			fixed (byte* bp = _bufferSmallConverting.Span)
			{
				*((Int32*)bp) = value;
			}
			this.Write(_bufferSmallConverting.Span.Slice(0, 4));
		}
		internal unsafe void Write(UInt32 value)
		{
			fixed (byte* bp = _bufferSmallConverting.Span)
			{
				*((UInt32*)bp) = value;
			}
			this.Write(_bufferSmallConverting.Span.Slice(0, 4));
		}
		internal unsafe void Write(DateTime value)
		{
			fixed (byte* bp = _bufferSmallConverting.Span)
			{
				*((Int64*)bp) = value.ToBinary();
			}
			this.Write(_bufferSmallConverting.Span.Slice(0, 8));
		}

		internal void FlushToFile()
		{
			_mem.WriteTo(_DFS);
			_DFS.Flush(true);
		}

		internal unsafe UInt16 ReadUInt16Core()
		{
			var t = ReadBytes(2);
			fixed (byte* bp = t)
			{
				return *(UInt16*)bp;
			}
		}
		internal unsafe int ReadInt32Core()
		{
			var t = ReadBytes(4);
			fixed (byte* bp = t)
			{
				return *(Int32*)bp;
			}
		}
		internal unsafe UInt32 ReadUInt32Core()
		{
			var t = ReadBytes(4);
			fixed (byte* bp = t)
			{
				return *(UInt32*)bp;
			}
		}
		internal unsafe DateTime ReadDateTimeCore()
		{
			var t = ReadBytes(8);
			fixed (byte* bp = t)
			{
				return DateTime.FromBinary(*(long*)bp);
			}
		}

		internal Span<byte> ReadBytes(int count)
		{
			if (count > _bufferRead.Length) throw new ArgumentOutOfRangeException("count");
			var res = _bufferRead.Span.Slice(0, count);
			_DFS.Read(res);
			DecryptInternal(res);
			return res;
		}

		internal void Crypt(CryptMethod cryptMethod, byte[] value)
		{
			if ((cryptMethod.Attrs & LogInfoAttributes.L1Crypt) != 0)
				CryptL1(value, cryptMethod.Skey);
			if ((cryptMethod.Attrs & LogInfoAttributes.L2Crypt) != 0)
				CryptL2(value, cryptMethod.Bkey);
			if ((cryptMethod.Attrs & LogInfoAttributes.L3Crypt) != 0)
				CryptL3(value, cryptMethod.CKey);
		}

		private void CryptL1(byte[] value, string Skey)
		{
			//implement!
			return;
		}

		private void CryptL2(byte[] value, byte Bkey)
		{
			//implement!
			return;
		}

		private void CryptL3(byte[] value, char Ckey)
		{
			//implement!
			return;
		}

		public void Dispose()
		{
			((IDisposable)_mem).Dispose();
			((IDisposable)_DFS).Dispose();
		}
	}
}
