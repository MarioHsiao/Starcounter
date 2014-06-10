using System;
using Starcounter;

// NOTE:
// This file is part of the Starcounter installation. It is
// used when running 'staradmin reload' to support reloading of
// databases. Do not modify it unless you are sure about what
// you do. You risk breaking the 'staradmin reload' functionality!

namespace ReloadDatabase {
    /// <summary>
    /// Implements a utility application supporting reloading a
    /// database.
    /// </summary>
    class Program {
        static void Main(string[] args) {
            var filePath = @"C:\Users\Public\Documents\ReloadData.sql";
            if (args.Length == 1)
                filePath = args[0];

            Console.WriteLine("Reload started at {0}", DateTime.Now.TimeOfDay);
            int reloaded = Db.Reload(filePath);
            Console.WriteLine("Reloaded: {0} objects ({1})", reloaded, DateTime.Now.TimeOfDay);
        }
    }
}