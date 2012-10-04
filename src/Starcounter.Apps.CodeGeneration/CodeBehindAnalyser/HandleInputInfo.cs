using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class HandleInputInfo
    {
        internal HandleInputInfo(String ns, String cn, String it)
        {
            Namespace = ns;
            ClassName = cn;
            InputType = it;
        }

        public readonly String Namespace;
        public readonly String ClassName;
        public readonly String InputType;
    }
}
