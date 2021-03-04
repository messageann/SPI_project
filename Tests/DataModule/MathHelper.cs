using System;

namespace Core3Tests.DataModule
{
	public static class MathHelper
	{
		//check alternatives
		public static UInt16 ToTheUpperPowerOf2(this UInt16 source)
		{
			UInt16 power = 0;
			while (source > 0)
			{
				source >>= 1;
				power++;
			}
			return (ushort)(1 << power);
		}
		public static int EqualOrLess(this int source, int max) => source > max ? max : source;
	}

}
