
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Starcounter.Internal
{
    
    internal class Package
    {

        internal static void Process(IntPtr hPackage)
        {
            GCHandle gcHandle = (GCHandle)hPackage;
            Package p = (Package)gcHandle.Target;
            gcHandle.Free();
            p.Process();
        }

        private Assembly assembly_;

        internal Package(Assembly assembly)
        {
            assembly_ = assembly;
        }

        internal void Process()
        {
            assembly_.EntryPoint.Invoke(null, new object[] {null});
        }
    }
}
