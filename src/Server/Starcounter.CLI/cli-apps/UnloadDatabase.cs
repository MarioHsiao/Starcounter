using System;
using Starcounter;
 
namespace UnloadDatabase {
    class Program {
        static void Main(string[] args) {
            string filePath = @"C:\Users\Public\Documents\ReloadData.sql";
            if (args.Length == 1)
                filePath = args[0];
            int unloaded = Db.Unload(filePath);
            Console.WriteLine("Unloaded: {0} objects", unloaded);
        }
    }
}