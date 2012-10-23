// ***********************************************************************
// <copyright file="SqlExecutableException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

//using se.sics.prologbeans;
//using Starcounter;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecutableException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected SqlExecutableException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        { }
    }
}
