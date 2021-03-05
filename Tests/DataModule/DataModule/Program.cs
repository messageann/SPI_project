using System;
using System.IO;

namespace DataModule
{
    class Program
    {
        static void Main(string[] args)
        {
            string p = @"bdsdjl";
            if (File.Exists(p)) File.Delete(p);
            {
                DataService dt = new DataService(p);
                dt.Init();
                dt.AddNewFolder(StatusEnum.Normal, "name1", "descr1");
                dt.AddNewFolder(StatusEnum.X4, "namex4", "descrx4");
            }

            {
                DataService dt = new DataService(p);
                dt.Init();
                dt.AddNewLogInfo(dt.Folders[0], "li1", "descrLI", "clogin", "cpass");
                dt.RemoveFolder(2);
            }

            {
                DataService dt = new DataService(p);
                dt.Init();
                foreach (var f in dt.Folders)
                {
                    dt.ReadFolderContent(f);
                    Console.WriteLine(f.Name);
                    foreach (var l in f._logInfos)
                    {
                        Console.WriteLine(l.Name);
                    }
                }
            }
        }
    }
}