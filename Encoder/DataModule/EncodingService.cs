using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModule
{
	internal class EncodingService
	{
		internal EncodingService(Encoding encoding, int charMaxCount, int byteMaxCount)
		{
			_enc = encoding.GetEncoder();
			_dec = encoding.GetDecoder();
			_bufferCharDecoding = new char[charMaxCount];
			_bufferCharEncoding = new byte[byteMaxCount];
		}

		private readonly Encoder _enc;
		private readonly Decoder _dec;
		private readonly char[] _bufferCharDecoding;
		private readonly byte[] _bufferCharEncoding;

		internal unsafe string GetString(Span<byte> value)
		{
			fixed (char* pch = _bufferCharDecoding)
			fixed (byte* pb = value)
			{
				_dec.Convert(pb, value.Length, pch, _bufferCharDecoding.Length, true, out _, out _, out _);
				return new string(pch);
			}
		}

		internal unsafe Span<byte> GetBytes(string value, int byteCount)
		{
			fixed (char* pch = value)
			fixed (byte* pb = _bufferCharEncoding)
			{
				_enc.Convert(pch, value.Length, pb, byteCount, true, out _, out _, out _);
				return new Span<byte>(pb, byteCount);
			}
		}
	}
}
