using DataModule.Exceptions;
using DataModule.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataModule
{
	internal class CryptService : IDisposable
	{
		#region CONSTS
		private const int BUFFER_LENGTH = BLOCK_SIZE * MAX_BLOCKS;
		internal const int BLOCK_SIZE = 128/8; //tdes blocksize
		private const int MAX_BLOCKS = LogInfo.BLOCKS_PER; //current max blocks is loginfo
		#endregion //CONSTS

		#region FIELDS
		private readonly Stream _base;
		private Stream _outputStream;
		private Stream _inputStream;
		private Stream _tempOut;
		private Stream _tempIn;

		#region BUFFERS
		private readonly Memory<byte> _bufferRead = new(new byte[BUFFER_LENGTH]);
		private int _posRead = 0;

		private readonly Memory<byte> _bufferWrite = new(new byte[BUFFER_LENGTH]);
		private int _posWrite = 0;
		private int _preparedWriteBlocks = 0;

		private readonly byte[] _sha256Buffer = new byte[32];
		#endregion //BUFFERS

		#region CRYPTO OBJECTS
		private readonly SHA256 _sha256 = SHA256.Create(); //Hash for tdes key
		private readonly Aes _aesBase = Aes.Create(); //[OPTIONAL, GLOBAL]
		private readonly HMACSHA256 _hmac = new();

		#region FILE
		private readonly ICryptoTransform _encryptFile;
		private readonly ICryptoTransform _decryptFile;
		#endregion //FILE

		#region FOLDER
		private ICryptoTransform _encryptFolder;
		private ICryptoTransform _decryptFolder;
		#endregion //FOLDER

		#endregion //CRYPTO OBJECTS

		private bool disposedValue;
		#endregion //FIELDS

		internal CryptService(FileInfo file, byte[] pass = null)
		{
			if (!file.Exists)
			{
				_base = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
			}
			else
			{
				_base = new FileStream(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
			}
			_aesBase.BlockSize = 128;
			_aesBase.KeySize = 256;
			_aesBase.Mode = CipherMode.ECB;
			_aesBase.Padding = PaddingMode.None;
			if (pass == null)
			{
				_outputStream = _base;
				_inputStream = _base;
			}
			else //account crypt ON
			{
				_sha256.TryComputeHash(pass, _sha256Buffer, out _);
				_aesBase.Key = _sha256Buffer;
				_outputStream = new CryptoStream(_base, _encryptFile = _aesBase.CreateEncryptor(), CryptoStreamMode.Write, false);
				_inputStream = new CryptoStream(_base, _decryptFile = _aesBase.CreateDecryptor(), CryptoStreamMode.Read, false);
			}
		}

		internal byte[] GetFolderKey(byte[] pass, byte[] hmac)
		{
			_hmac.Key = pass;
			//_hmac.Initialize(); //redurant?
			byte[] possibleKey = _sha256.ComputeHash(pass);
			ClearBytes(pass);
			var verif = BytesEqual(_hmac.ComputeHash(possibleKey), hmac);
			return verif ? possibleKey : null;
		}
		internal byte[] CreateHMAC(byte[] pass, out byte[] key)
		{
			_hmac.Key = pass;
			return _hmac.ComputeHash(key = _sha256.ComputeHash(pass));
		}
		internal static bool BytesEqual(ReadOnlySpan<byte> arr1, ReadOnlySpan<byte> arr2) => arr1.SequenceEqual(arr2);
		internal static void ClearBytes(Span<byte> arr) => arr.Clear();

		#region PREPARE BLOCKS
		internal void PrepareReadBlocks(int count)
		{
			if (count > MAX_BLOCKS) throw new ArgumentOutOfRangeException(nameof(count));
			_inputStream.Read(_bufferRead.Span.Slice(0, BLOCK_SIZE * count));
			_posRead = 0;
		}
		internal void PrepareWriteBlocks(int count)
		{
			if (count > MAX_BLOCKS) throw new ArgumentOutOfRangeException(nameof(count));
			if (_preparedWriteBlocks != 0) throw new InvalidOperationException("Previos blocks not flushed!");
			_preparedWriteBlocks = count;
			_bufferWrite.Span.Clear(); //check performance vs array clear
			_posWrite = 0;
		}
		#endregion //PREPARE BLOCKS

		#region GET
		internal unsafe UInt16 GetUInt16()
		{
			UInt16 res;
			fixed (byte* pb = _bufferRead.Span)
			{
				res = *(UInt16*)(pb + _posRead);
			}
			_posRead += sizeof(UInt16);
			return res;
		}
		internal unsafe UInt32 GetUInt32()
		{
			UInt32 res;
			fixed (byte* pb = _bufferRead.Span)
			{
				res = *(UInt32*)(pb + _posRead);
			}
			_posRead += sizeof(UInt32);
			return res;
		}
		internal unsafe DateTime GetDateTime()
		{
			DateTime res;
			fixed (byte* pb = _bufferRead.Span)
			{
				res = *(DateTime*)(pb + _posRead);
			}
			_posRead += sizeof(DateTime);
			return res;
		}
		internal Span<byte> GetBytes(int count)
		{
			var res = _bufferRead.Span.Slice(_posRead, count);
			_posRead += count;
			return res;
		}
		#endregion //GET

		#region WRITE
		internal unsafe void Write(UInt16 value)
		{
			fixed (byte* pb = _bufferWrite.Span)
			{
				*(UInt16*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(UInt16);
		}
		internal unsafe void Write(UInt32 value)
		{
			fixed (byte* pb = _bufferWrite.Span)
			{
				*(UInt32*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(UInt32);
		}
		internal unsafe void Write(DateTime value)
		{
			fixed (byte* pb = _bufferWrite.Span)
			{
				*(DateTime*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(DateTime);
		}
		internal void Write(ReadOnlySpan<byte> value)
		{
			value.CopyTo(_bufferWrite.Span[_posWrite..]);
			_posWrite += value.Length;
		}
		internal void WriteAndFlush(ReadOnlySpan<byte> value)
		{
			if (value.Length % BLOCK_SIZE != 0) throw new ArgumentException("Length must be blocksized!", nameof(value));
			_outputStream.Write(value);
			_outputStream.Flush();
		}
		#endregion //WRITE

		#region ADD CRYPT LAYER
		internal void AddFolderCryptLayer(byte[] key)
		{
			if (_tempIn != null || _tempOut != null) throw new CryptLayerException(true);
			_tempOut = _outputStream;
			_tempIn = _inputStream;
			_aesBase.Key = key;
			_outputStream = new CryptoStream(_outputStream, _encryptFolder = _aesBase.CreateEncryptor(), CryptoStreamMode.Write, true);
			_inputStream = new CryptoStream(_inputStream, _decryptFolder = _aesBase.CreateDecryptor(), CryptoStreamMode.Read, true);
		}
		internal void RemoveFolderCryptLayer()
		{
			if (_tempIn == null || _tempOut == null) throw new CryptLayerException(false);
			_outputStream.Dispose();
			_inputStream.Dispose();
			_encryptFolder.Dispose();
			_decryptFolder.Dispose();
			_outputStream = _tempOut;
			_inputStream = _tempIn;
			_tempIn = null;
			_tempOut = null;
		}
		#endregion //ADD CRYPT LAYER

		internal void FlushPreparedBlocks()
		{
			if (_preparedWriteBlocks < 1) throw new InvalidOperationException("Blocks must be prepared before flush!");
			_outputStream.Write(_bufferWrite.Span.Slice(0, _preparedWriteBlocks * BLOCK_SIZE));
			_outputStream.Flush(); //check idk
			_preparedWriteBlocks = 0;
		}

		#region DEFAULT STREAM METHODS
		internal void Seek(long dest) => _base.Seek(dest, SeekOrigin.Begin);
		internal long Position => _base.Position;
		internal long Length => _base.Length;
		#endregion //DEFAULT STREAM METHODS

		#region HEAVY CRYPTS
		internal Span<byte> Crypt(CryptMethod cryptMethod, Span<byte> value)
		{
			if ((cryptMethod.Attrs & LogInfoAttributes.L1Crypt) == LogInfoAttributes.L1Crypt)
				CryptL1(value, cryptMethod.Skey);
			if ((cryptMethod.Attrs & LogInfoAttributes.L2Crypt) == LogInfoAttributes.L2Crypt)
				CryptL2(value, cryptMethod.Bkey);
			if ((cryptMethod.Attrs & LogInfoAttributes.L3Crypt) == LogInfoAttributes.L3Crypt)
				CryptL3(value, cryptMethod.CKey);
			return value;
		}

		private void CryptL1(Span<byte> value, string Skey)
		{
			//implement!
			return;
		}
		private void CryptL2(Span<byte> value, byte Bkey)
		{
			//implement!
			return;
		}
		private void CryptL3(Span<byte> value, char Ckey)
		{
			//implement!
			return;
		}
		#endregion //HEAVY CRYPTS

		#region DISPOSE PATTERN
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					_outputStream.Dispose();
					_inputStream.Dispose();
					_base.Dispose();
					_tempOut?.Dispose();
					_tempIn?.Dispose();

					_sha256.Dispose();
					_hmac.Dispose();

					_aesBase.Dispose();
					_encryptFile?.Dispose();
					_decryptFile?.Dispose();
					_encryptFolder?.Dispose();
					_decryptFolder?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}
		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion //DISPOSE PATTERN
	}
}
