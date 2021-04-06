using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModule.Models
{
	public readonly struct CryptMethod
	{
		internal readonly LogInfoAttributes Attrs;
		internal readonly string Skey;
		internal readonly byte Bkey;
		internal readonly char CKey;

		private CryptMethod(LogInfoAttributes attrs, string skey = null, byte bkey = default(byte), char cKey = default(char))
		{
			Attrs = attrs;
			Skey = skey;
			Bkey = bkey;
			CKey = cKey;
		}

		public CryptMethod(string Skey) : this(LogInfoAttributes.L1Crypt, skey: Skey)
		{

		}
	}
}
