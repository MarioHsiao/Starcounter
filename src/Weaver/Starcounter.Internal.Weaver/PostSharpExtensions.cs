// ***********************************************************************
// <copyright file="PostSharpExtensions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Class PostSharpExtensions
    /// </summary>
    internal static class PostSharpExtensions {
        /// <summary>
        /// Gets the name of the reflection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetReflectionName(this ITypeSignature type) {
            StringBuilder builder = new StringBuilder();
            type.WriteReflectionName(builder, ReflectionNameOptions.None);
            return builder.ToString();
        }

    }
}