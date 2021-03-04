using System;
using System.IO;

namespace DataModule
{
	class Program
	{
		static void Main(string[] args)
		{
			string p = @"D:\TestCore_testfolder\testbd";
			if (File.Exists(p)) File.Delete(p);
		}
	}
}
