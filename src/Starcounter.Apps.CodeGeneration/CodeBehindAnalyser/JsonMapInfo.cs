using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class JsonMapInfo
    {
        public readonly String Namespace;
        public readonly String ClassName;
        public readonly List<String> ParentClasses;
        public readonly String JsonMapName;

        internal JsonMapInfo(String ns, String cn, List<String> pc, String jmn)
        {
            Namespace = ns;
            ClassName = cn;
            ParentClasses = pc;
            JsonMapName = jmn;
        }
    }
}
