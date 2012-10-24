// ***********************************************************************
// <copyright file="JsonMapInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    /// <summary>
    /// Class JsonMapInfo
    /// </summary>
    public class JsonMapInfo
    {
        /// <summary>
        /// The namespace
        /// </summary>
        public readonly String Namespace;
        /// <summary>
        /// The class name
        /// </summary>
        public readonly String ClassName;
        /// <summary>
        /// The parent classes
        /// </summary>
        public readonly List<String> ParentClasses;
        /// <summary>
        /// The json map name
        /// </summary>
        public readonly String JsonMapName;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMapInfo" /> class.
        /// </summary>
        /// <param name="ns">The ns.</param>
        /// <param name="cn">The cn.</param>
        /// <param name="pc">The pc.</param>
        /// <param name="jmn">The JMN.</param>
        internal JsonMapInfo(String ns, String cn, List<String> pc, String jmn)
        {
            Namespace = ns;
            ClassName = cn;
            ParentClasses = pc;
            JsonMapName = jmn;
        }
    }
}
