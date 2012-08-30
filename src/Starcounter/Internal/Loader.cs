
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Starcounter.Internal
{
    
    internal static class Loader
    {

        internal static unsafe void RunMessageLoop(void* hsched)
        {
            for (; ; )
            {
                string input = Console.ReadLine();

                input = Environment.CurrentDirectory + "\\" + input; // TODO:

                Assembly assembly = Assembly.LoadFile(input);
                Package package = new Package(assembly);
                IntPtr hPackage = (IntPtr)GCHandle.Alloc(package, GCHandleType.Normal);

                uint e = sccorelib.cm2_schedule(
                    hsched,
                    0,
                    sccorelib_ext.TYPE_PROCESS_PACKAGE,
                    0,
                    0,
                    0,
                    (ulong)hPackage
                    );
                if (e != 0) throw sccoreerr.TranslateErrorCode(e);
            }
        }
    }
}
