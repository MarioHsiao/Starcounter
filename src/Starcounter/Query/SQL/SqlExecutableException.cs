
//using se.sics.prologbeans;
//using Starcounter;
//using Sc.Server.Binding;
//using Sc.Server.Internal;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Security.Permissions;
//using System.Threading;
//using System.Xml;
//using Starcounter.Query.Execution;
using System;
using System.Runtime.Serialization;

namespace Starcounter.Query.Sql
{
    /// <summary>
    /// Used within PrologManager to represent errors with the SQL executable.
    /// </summary>
    [Serializable]
    public class SqlExecutableException : Exception
    {
        internal SqlExecutableException(String message)
            : base(message)
        { }

        internal SqlExecutableException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected SqlExecutableException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        { }
    }
}
