using System;
using Starcounter;

// NOTE:
// This file is part of the Starcounter installation. It is
// used when running 'staradmin unload' to support unloading of
// databases. Do not modify it unless you are sure about what
// you do. You risk breaking the 'staradmin unload' functionality!
 
namespace UnloadDatabase {
    /// <summary>
    /// Implements a utility application supporting unloading a
    /// database.
    /// </summary>
    class Program {
        static void Main(string[] args) {
            var filePath = @"C:\Users\Public\Documents\ReloadData.sql";
            if (args.Length == 1)
                filePath = args[0];

            Console.WriteLine("Unload started at {0}", DateTime.Now.TimeOfDay);
            int unloaded = Db.Unload(filePath);
            Console.WriteLine("Unloaded: {0} objects ({1})", unloaded, DateTime.Now.TimeOfDay);
        }
    }
}