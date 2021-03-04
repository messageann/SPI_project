using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DataModule
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class LogInfo
	{
		#region loginfo consts
		internal const ushort BYTES_ID = 2;
		private const ushort BYTES_NAME = 32;
		private const ushort BYTES_DESCR = 114;
		private const ushort BYTES_DATE = 8;
		private const ushort BYTES_CLOGIN = 64;
		private const ushort BYTES_CPASS = 100;

		internal const ushort BYTES_LOGINFO = 0
			+ BYTES_ID//id
			+ BYTES_NAME//name
			+ BYTES_CLOGIN//login
			+ BYTES_CPASS//pass
			+ BYTES_DESCR//description
			+ BYTES_DATE//date created
			;
		#endregion loginfo consts
		internal static readonly byte[] EmptyLogInfo = new byte[BYTES_LOGINFO];

		#region BODY FIELDS
		internal readonly UInt16 Id;
		private readonly string Name;
		private readonly string Descr;
		private readonly DateTime Date;
		private readonly string CLogin;
		private readonly string CPass;
		#endregion //BODY FIELDS

		internal readonly long FilePos;

		public LogInfo(long filePos, ushort id, string name, string descr, DateTime date, string clogin, string cpass)
		{
			FilePos = filePos;
			Id = id;
			Name = name;
			Descr = descr;
			Date = date;
			CLogin = clogin;
			CPass = cpass;
		}

		//change encoding
		public static LogInfo ReadFromFile(BinaryReader br)
		{
			var pos = br.BaseStream.Position;
			var id = br.ReadUInt16();
			if (id == 0) return null;
			return new LogInfo(pos, id, Encoding.Default.GetString(br.ReadBytes(BYTES_NAME)), Encoding.Default.GetString(br.ReadBytes(BYTES_DESCR)),
				DateTime.FromBinary(br.ReadInt64()), Encoding.Default.GetString(br.ReadBytes(BYTES_CLOGIN)), Encoding.Default.GetString(br.ReadBytes(BYTES_CPASS)));
		}

		//check ToBinary vs ToFileTime (speed)
		//change encoding
		public void WriteToFile(BinaryWriter bw)
		{
			bw.Write(Id);

			byte[] nameb = new byte[BYTES_NAME];
			Encoding.Default.GetBytes(Name, 0, Name.Length.EqualOrLess(BYTES_NAME * 2), nameb, 0);
			bw.Write(nameb);

			byte[] descrb = new byte[BYTES_DESCR];
			Encoding.Default.GetBytes(Descr, 0, Descr.Length.EqualOrLess(BYTES_DESCR * 2), descrb, 0);
			bw.Write(descrb);

			bw.Write(Date.ToBinary());

			byte[] cloginb = new byte[BYTES_CLOGIN];
			Encoding.Default.GetBytes(CLogin, 0, CLogin.Length.EqualOrLess(BYTES_CLOGIN * 2), cloginb, 0);
			bw.Write(cloginb);

			byte[] cpassb = new byte[BYTES_CPASS];
			Encoding.Default.GetBytes(CPass, 0, CPass.Length.EqualOrLess(BYTES_CPASS * 2), cpassb, 0);
			bw.Write(cpassb);
		}
	}
}
