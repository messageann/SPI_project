using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Core3Tests.DataModule
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class FolderInfo
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
		internal readonly StatusEnum Status;

		internal UInt16 Quantity;

		internal readonly UInt16 Id;

		private string Name;

		private string Descr;
		#endregion //BODY FIELDS

		internal readonly List<LogInfo> _logInfos;

		internal readonly long FilePos;

		public FolderInfo(long filePos, StatusEnum status, ushort quantity, ushort id, string name, string descr)
		{
			Status = status;
			Id = id;
			Name = name;
			Descr = descr;
			Quantity = quantity;
			_logInfos = new List<LogInfo>((int)status / 2 * DEFAULT_QUANTITY);
			FilePos = filePos;
		}

		public void WriteBodyToFile(BinaryWriter bw)
		{
			bw.Write((UInt32)Status);
			bw.Write(Quantity);
			bw.Write(Id);
			byte[] nameb = new byte[BYTES_NAME];
			Encoding.Default.GetBytes(Name, 0, Name.Length.EqualOrLess(BYTES_NAME*2), nameb, 0);
			bw.Write(nameb);

			byte[] descrb = new byte[BYTES_DESCR];
			Encoding.Default.GetBytes(Descr, 0, Descr.Length.EqualOrLess(BYTES_DESCR*2), descrb, 0);
			bw.Write(descrb);
			for (int i = 0; i < _logInfos.Capacity; i++)
			{
				bw.Write(LogInfo.EmptyLogInfo);
			}
			var nb = GetNullBytes();
			if (nb > 0)
			{
				byte[] nullb = new byte[nb];
				bw.Write(nullb);
			}
		}

		public static FolderInfo ReadBodyFromFile(BinaryReader br)
		{
			var res = new FolderInfo(br.BaseStream.Position, (StatusEnum)br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(),
				Encoding.Default.GetString(br.ReadBytes(BYTES_NAME)), Encoding.Default.GetString(br.ReadBytes(BYTES_DESCR)));
			br.BaseStream.Seek(br.BaseStream.Position + res._logInfos.Capacity * LogInfo.BYTES_LOGINFO + res.GetNullBytes(), SeekOrigin.Begin);
			return res;
		}

		private static readonly byte[] NullBody;

		static FolderInfo()
		{
			NullBody = new byte[BYTES_BODY];
			NullBody[0] = 1;
		}
	}

	public enum StatusEnum : UInt32
	{
		NULL = 1U << 0,
		Normal = 1U << 1,
		X2 = 1U << 2,
		X4 = 1U << 3,
		X8 = 1U << 4
	}
}
