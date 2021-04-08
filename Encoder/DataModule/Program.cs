using DataModule.Models;
using System;
using System.IO;

namespace DataModule
{
	class Program
	{
		static void Main(string[] args)
		{
			string p = @"D:\TestCore_testfolder\dataENC";
			if (File.Exists(p)) File.Delete(p);
			using (var ds = new DataService(p))
			{
				ds.Init();
				ds.RegFolderInfo(StatusEnum.Normal, "folder one", "normal length folder 1st");
			}
			using (var ds = new DataService(p))
			{
				ds.Init();
				ds.RegLogInfo(ds.Folders[0], "google acc1", "old account, almost empty", "myaccount_old@gmail.com", "qwe123rty", new CryptMethod("key1"));
			}
			using (var ds = new DataService(p))
			{
				ds.Init();
				ds.ReadFolderInfoContent(ds.Folders[0]);
			}
		}
	}
}