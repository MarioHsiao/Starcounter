
using System;

namespace Starcounter.Internal
{
    
    internal static class sccoreerr
    {

        internal static Exception TranslateErrorCode(uint e)
        {
            throw new Exception(e.ToString()); // TODO:
        }
    }
}
