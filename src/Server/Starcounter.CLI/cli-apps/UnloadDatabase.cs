using System;
using System.IO;
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
        static int Main(string[] args) {
            if (args.Length != 3) {
                // Arguments expected: file, allowPartial, shiftID
                // To use the default file, pass "@".
                Console.WriteLine("Invalid arguments. Use 'file allowPartial shiftID'");
                return -1;
            }

            var filePath = args[0];
            if (filePath == "@") {
                var fileName = Db.Environment.DatabaseNameLower + ".sql";
                filePath = Path.Combine(@"C:\Users\Public\Documents", fileName);
            }
            var allowPartial = bool.Parse(args[1]);
            var shiftID = ulong.Parse(args[2]);

            Console.WriteLine("Unload started at {0}", DateTime.Now.TimeOfDay);
            int unloaded = Db.Unload(filePath, shiftID, !allowPartial);
            Console.WriteLine("Unloaded: {0} objects ({1})", unloaded, DateTime.Now.TimeOfDay);
            return 0;
        }
    }
}