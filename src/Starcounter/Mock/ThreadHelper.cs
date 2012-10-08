
using Starcounter.Internal;
using System;

namespace Starcounter
{
    
    public static class ThreadHelper
    {

        public static void SetYieldBlock()
        {
            uint r = sccorelib.cm3_set_yblk((IntPtr)0);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        public static void ReleaseYieldBlock()
        {
            uint r = sccorelib.cm3_rel_yblk((IntPtr)0);
            if (r == 0) return;
            ExceptionManager.HandleInternalFatalError(r);
        }
    }
}
