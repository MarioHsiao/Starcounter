// ***********************************************************************
// <copyright file="Paths.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Starcounter.Internal {
    /// <summary>
    /// Class Paths
    /// </summary>
    public class Paths {
        /// <summary>
        /// Strips the file name without extention.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>System.String.</returns>
        public static string StripFileNameWithoutExtention(string fileSpec) {
            string fileName = Path.GetFileName(fileSpec);
            string[] parts = fileName.Split('.');
            return parts[0];
        }
    }
}
