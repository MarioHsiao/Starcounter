// ***********************************************************************
// <copyright file="ScTransformTrace.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Extensibility;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Declare a PostSharp trace source for this assembly.
    /// </summary>
    internal static class ScTransformTrace {
        /// <summary>
        /// PostSharp trace source for this assembly.
        /// </summary>
        public static readonly PostSharpTrace Instance = new PostSharpTrace("ScTransform");
    }
}