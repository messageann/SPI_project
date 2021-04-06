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
			{
                DataService ds = new DataService(p);
                ds.Init();
                //ds.RegLogInfo(new Models.FolderInfo())
			}            
        }
    }
}