using DataModule.Exceptions;
using DataModule.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DataModule
{
	internal class CryptService : IDisposable
	{
		#region CONSTS
		private const int BUFFER_LENGTH = BLOCK_SIZE * MAX_BLOCKS;
		internal const int BLOCK_SIZE = 128 / 8; //tdes blocksize
		private const int MAX_BLOCKS = LogInfo.BLOCKS_PER; //current max blocks is loginfo
		#endregion //CONSTS

		#region FIELDS
		private readonly Stream _base;
		private CryptoStream _outputStream;
		private CryptoStream _inputStream;

		#region BUFFERS
		private readonly byte[] _bufferRead = new byte[BUFFER_LENGTH];
		private readonly byte[] _bufferReadCryptLayer = new byte[BUFFER_LENGTH];
		private int _posRead = 0;

		private readonly byte[] _bufferWrite = new byte[BUFFER_LENGTH];
		private readonly byte[] _bufferWriteCryptLayer = new byte[BUFFER_LENGTH];
		private int _posWrite = 0;
		private int _preparedWriteBlocks = 0;

		private readonly byte[] _sha256Buffer = new byte[32];
		#endregion //BUFFERS

		#region CRYPTO OBJECTS
		private readonly SHA256 _sha256 = SHA256.Create();
		private readonly Aes _aesBase = Aes.Create();
		private readonly HMACSHA256 _hmac = new();

		#region FILE
		private ICryptoTransform _encryptFile;
		private ICryptoTransform _decryptFile;
		#endregion //FILE

		#region FOLDER
		private ICryptoTransform _encryptLayer = null;
		private ICryptoTransform _decryptLayer = null;
		#endregion //FOLDER

		#endregion //CRYPTO OBJECTS

		private bool disposedValue;
		#endregion //FIELDS

		internal CryptService(FileInfo file)
		{
			if (!file.Exists)
			{
				_base = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
			}
			else
			{
				_base = new FileStream(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
			}
			_aesBase.Mode = CipherMode.ECB;
			_aesBase.Padding = PaddingMode.None;
			_aesBase.BlockSize = 128;
			_aesBase.KeySize = 256;
		}

		internal void Init(byte[] key)
		{
			if (key == null)
			{
				//_outputStream = _base;
				//_inputStream = _base;
				throw new NoPasswordException();
			}
			else //account crypt ON
			{
				_outputStream = new CryptoStream(_base, _encryptFile = _aesBase.CreateEncryptor(key, null), CryptoStreamMode.Write, true);
				_inputStream = new CryptoStream(_base, _decryptFile = _aesBase.CreateDecryptor(key, null), CryptoStreamMode.Read, true);
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
		internal byte[] CreateHMAC256FromKey(byte[] pass, out byte[] key)
		{
			_hmac.Key = pass;
			key = _sha256.ComputeHash(pass);
			ClearBytes(pass);
			return _hmac.ComputeHash(key);
		}
		//internal byte[] CreateHMAC256(byte[] pass, out byte[] key,byte[] inp1, byte[] inp2)
		//{
		//	_hmac.Key = pass;
		//	ClearBytes(pass);
		//	_hmac.TransformBlock(inp1, 0, inp1.Length, null, 0);
		//	_hmac.TransformBlock(key = _sha256.ComputeHash(pass), 0, key.Length, null, 0);
		//	_ = _hmac.TransformFinalBlock(inp2, 0, inp2.Length);
		//	return _hmac.Hash;
		//}
		internal static bool BytesEqual(ReadOnlySpan<byte> arr1, ReadOnlySpan<byte> arr2) => arr1.SequenceEqual(arr2);
		internal static void ClearBytes(Span<byte> arr) => arr.Clear();

		#region PREPARE BLOCKS
		internal void PrepareReadBlocks(int count)
		{
			if (count > MAX_BLOCKS) throw new ArgumentOutOfRangeException(nameof(count));
			_inputStream.Read(_bufferRead, 0, count * BLOCK_SIZE);
			_decryptLayer?.TransformBlock(_bufferRead, 0, count * BLOCK_SIZE, _bufferRead, 0);
			_posRead = 0;
		}
		internal void PrepareWriteBlocks(int count)
		{
			if (count > MAX_BLOCKS) throw new ArgumentOutOfRangeException(nameof(count));
			if (_preparedWriteBlocks != 0) throw new InvalidOperationException("Previos blocks not flushed!");
			_preparedWriteBlocks = count;
			//_bufferWrite.Clear(); //check performance vs array clear
			ClearBytes(_bufferWrite);
			_posWrite = 0;
		}
		#endregion //PREPARE BLOCKS

		#region GET
		internal unsafe UInt16 GetUInt16()
		{
			UInt16 res;
			fixed (byte* pb = _bufferRead)
			{
				res = *(UInt16*)(pb + _posRead);
			}
			_posRead += sizeof(UInt16);
			return res;
		}
		internal unsafe UInt32 GetUInt32()
		{
			UInt32 res;
			fixed (byte* pb = _bufferRead)
			{
				res = *(UInt32*)(pb + _posRead);
			}
			_posRead += sizeof(UInt32);
			return res;
		}
		internal unsafe DateTime GetDateTime()
		{
			DateTime res;
			fixed (byte* pb = _bufferRead)
			{
				res = *(DateTime*)(pb + _posRead);
			}
			_posRead += sizeof(DateTime);
			return res;
		}
		internal Span<byte> GetBytes(int count)
		{
			//var res = _bufferRead.Span.Slice(_posRead, count);
			var res = new Span<byte>(_bufferRead, _posRead, count);
			_posRead += count;
			return res;
		}
		#endregion //GET

		internal void ReadThroughCrypt(Span<byte> buffer)
		{
			_base.Read(buffer);
		}

		#region WRITE
		internal unsafe void Write(UInt16 value)
		{
			fixed (byte* pb = _bufferWrite)
			{
				*(UInt16*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(UInt16);
		}
		internal unsafe void Write(UInt32 value)
		{
			fixed (byte* pb = _bufferWrite)
			{
				*(UInt32*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(UInt32);
		}
		internal unsafe void Write(DateTime value)
		{
			fixed (byte* pb = _bufferWrite)
			{
				*(DateTime*)(pb + _posWrite) = value;
			}
			_posWrite += sizeof(DateTime);
		}
		internal void Write(ReadOnlySpan<byte> value)
		{
			//value.CopyTo(_bufferWrite.Span[_posWrite..]);
			value.CopyTo(new Span<byte>(_bufferWrite, _posWrite, value.Length));
			_posWrite += value.Length;
		}
		internal void WriteAndFlush(byte[] value)
		{
			if (value.Length % BLOCK_SIZE != 0) throw new ArgumentException("Length must be blocksized!", nameof(value));
			_encryptLayer?.TransformBlock(value, 0, value.Length, value, 0);
			_outputStream.Write(value);
			_outputStream.Flush();
		}
		internal void FlushPreparedBlocks()
		{
			if (_preparedWriteBlocks < 1) throw new InvalidOperationException("Blocks must be prepared before flush!");
			//var buf = _bufferWrite.Span.Slice(0, _preparedWriteBlocks * BLOCK_SIZE);
			_encryptLayer?.TransformBlock(_bufferWrite, 0, _preparedWriteBlocks * BLOCK_SIZE, _bufferWrite, 0);
			_outputStream.Write(_bufferWrite, 0, _preparedWriteBlocks * BLOCK_SIZE);
			_outputStream.Flush(); //check idk
			_preparedWriteBlocks = 0;
		}

		internal void WriteThroughCrypt(ReadOnlySpan<byte> value)
		{
			_base.Write(value);
		}
		#endregion //WRITE

		#region ADD CRYPT LAYER
		internal void AddAES256CryptLayerOne(byte[] key)
		{
			if (_encryptLayer != null || _decryptLayer != null) throw new CryptLayerException(true);
			_encryptLayer = _aesBase.CreateEncryptor(key, null);
			_decryptLayer = _aesBase.CreateDecryptor(key, null);
			//if (_tempIn != null || _tempOut != null) throw new CryptLayerException(true);
			//_tempOut = _outputStream;
			//_tempIn = _inputStream;
			//_aesBase.Key = key;
			//_outputStream = new CryptoStream(_outputStream, _encryptLayer = _aesBase.CreateEncryptor(), CryptoStreamMode.Write, true);
			//_inputStream = new CryptoStream(_inputStream, _decryptLayer = _aesBase.CreateDecryptor(), CryptoStreamMode.Read, true);
		}
		internal void RemoveAES256CryptLayerOne()
		{
			if (_encryptLayer == null || _decryptLayer == null) throw new CryptLayerException(false);
			_encryptLayer.Dispose();
			_encryptLayer = null;
			_decryptLayer.Dispose();
			_decryptLayer = null;
			//if (_tempIn == null || _tempOut == null) throw new CryptLayerException(false);
			//_outputStream.Dispose();
			//_inputStream.Dispose();
			//_encryptLayer.Dispose();
			//_decryptLayer.Dispose();
			//_outputStream = _tempOut;
			//_inputStream = _tempIn;
			//_tempIn = null;
			//_tempOut = null;
		}
		//internal void AddAES256CryptLayerTwo(byte[] key)
		//{
		//	if (_tempIn2 != null || _tempOut2 != null) throw new CryptLayerException(true);
		//	_tempOut2 = _outputStream;
		//	_tempIn2 = _inputStream;
		//	_aesBase.Key = key;
		//	_outputStream = new CryptoStream(_outputStream, _encrypt2 = _aesBase.CreateEncryptor(), CryptoStreamMode.Write, true);
		//	_inputStream = new CryptoStream(_inputStream, _decrypt2 = _aesBase.CreateDecryptor(), CryptoStreamMode.Read, true);
		//}
		//internal void RemoveAES256CryptLayerTwo()
		//{
		//	if (_tempIn2 == null || _tempOut2 == null) throw new CryptLayerException(false);
		//	_outputStream.Dispose();
		//	_inputStream.Dispose();
		//	_encrypt2.Dispose();
		//	_decrypt2.Dispose();
		//	_outputStream = _tempOut2;
		//	_inputStream = _tempIn2;
		//	_tempIn2 = null;
		//	_tempOut2 = null;
		//}
		#endregion //ADD CRYPT LAYER


		#region DEFAULT STREAM METHODS
		internal void Seek(long dest) => _base.Seek(dest, SeekOrigin.Begin);
		internal long Position => _base.Position;
		internal long Length => _base.Length;
		#endregion //DEFAULT STREAM METHODS

		#region HEAVY CRYPTS


		internal byte[] CryptAES256(byte[] value, byte[] key)
		{
			byte[] res = new byte[value.Length];
			using (var cr = _aesBase.CreateEncryptor(key, null))
			{
				using (var mem = new MemoryStream(res, true))
				using (var cs = new CryptoStream(mem, cr, CryptoStreamMode.Write))
				{
					cs.Write(value);
				}
			}
			return res;
		}

		internal byte[] DecryptAES256(byte[] value, byte[] key)
		{
			byte[] res = new byte[value.Length];
			using (var cr = _aesBase.CreateDecryptor(key, null))
			{
				using (var mem = new MemoryStream(value, false))
				using (var cs = new CryptoStream(mem, cr, CryptoStreamMode.Read))
				{
					cs.Read(res);
				}
			}
			return res;
		}

		internal ICryptoTransform GetEncryptor() => _encryptFile;

		internal ICryptoTransform GetDecryptor() => _decryptFile;

		//internal Span<byte> Crypt(CryptMethod cryptMethod, Span<byte> value)
		//{
		//	if ((cryptMethod.Attrs & LogInfoAttributes.L1Crypt) == LogInfoAttributes.L1Crypt)
		//		CryptL1(value, cryptMethod.Skey);
		//	if ((cryptMethod.Attrs & LogInfoAttributes.L2Crypt) == LogInfoAttributes.L2Crypt)
		//		CryptL2(value, cryptMethod.Bkey);
		//	if ((cryptMethod.Attrs & LogInfoAttributes.L3Crypt) == LogInfoAttributes.L3Crypt)
		//		CryptL3(value, cryptMethod.CKey);
		//	return value;
		//}

		//private void CryptL1(Span<byte> value, string Skey)
		//{
		//	//implement!
		//	return;
		//}
		//private void CryptL2(Span<byte> value, byte Bkey)
		//{
		//	//implement!
		//	return;
		//}
		//private void CryptL3(Span<byte> value, char Ckey)
		//{
		//	//implement!
		//	return;
		//}
		#endregion //HEAVY CRYPTS

		#region DISPOSE PATTERN
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					_outputStream?.Dispose();
					_inputStream?.Dispose();
					_base.Dispose();
					//_tempIn?.Dispose();
					//_tempOut?.Dispose();

					_sha256.Dispose();
					_hmac.Dispose();
					_aesBase.Dispose();

					_encryptFile?.Dispose();
					_decryptFile?.Dispose();
					_encryptLayer?.Dispose();
					_decryptLayer?.Dispose();
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

		#region STATICS
		internal static readonly byte[] Empty256Bits = new byte[256 / 8];
		#endregion //STATICS
	}
}
