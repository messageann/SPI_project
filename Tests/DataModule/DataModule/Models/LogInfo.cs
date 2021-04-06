using System;
using System.Runtime.InteropServices;

namespace DataModule.Models
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class LogInfo
	{
		#region loginfo consts
		internal const ushort BYTES_ID = 2;
		internal const ushort BYTES_ATTRIBUTES = 4;
		internal const ushort BYTES_NAME = 32;
		internal const ushort BYTES_DESCR = 114;
		internal const ushort BYTES_DATE = 8;
		internal const ushort BYTES_CLOGIN = 64;
		internal const ushort BYTES_CPASS = 96;

		internal const ushort BYTES_LOGINFO = 0
			+ BYTES_ID
			+ BYTES_ATTRIBUTES
			+ BYTES_NAME
			+ BYTES_CLOGIN
			+ BYTES_CPASS
			+ BYTES_DESCR
			+ BYTES_DATE
			;
		#endregion loginfo consts
		internal static readonly byte[] EmptyLogInfo = new byte[BYTES_LOGINFO];

		#region BODY FIELDS
		internal readonly UInt16 Id;
		internal LogInfoAttributes Attributes;
		internal readonly string Name;
		internal readonly string Descr;
		internal readonly DateTime Date;
		internal readonly byte[] CLogin;
		internal readonly byte[] CPass;
		#endregion //BODY FIELDS

		internal long FilePos;

		public LogInfo(long filePos, ushort id, LogInfoAttributes attributes, string name, string descr, DateTime date, byte[] clogin, byte[] cpass)
		{
			FilePos = filePos;
			Id = id;
			Attributes = attributes;
			Name = name;
			Descr = descr;
			Date = date;
			CLogin = clogin;
			CPass = cpass;
		}
	}

	public enum LogInfoAttributes : UInt32
	{
		None = 0,
		L1Crypt = 1U << 0,
		L2Crypt = 1U << 1,
		L3Crypt = 1U << 2
	}
}
