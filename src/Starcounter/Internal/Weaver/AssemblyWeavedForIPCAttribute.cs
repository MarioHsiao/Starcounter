// ***********************************************************************
// <copyright file="AssemblyWeavedForIPCAttribute.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Class AssemblyWeavedForIPCAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    internal class AssemblyWeavedForIPCAttribute : Attribute {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyWeavedForIPCAttribute" /> class.
        /// </summary>
        public AssemblyWeavedForIPCAttribute()
            : base() {
        }
    }
}
