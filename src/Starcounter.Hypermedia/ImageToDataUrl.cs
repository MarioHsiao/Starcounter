// ***********************************************************************
// <copyright file="ImageToDataUrl.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
namespace Starcounter.Internal.WebServing {
    /// <summary>
    /// Class ImageToDataUrl
    /// </summary>
    public class ImageToDataUrl {

        /// <summary>
        /// Gets the data URL.
        /// </summary>
        /// <param name="imgFile">The img file.</param>
        /// <returns>System.String.</returns>
        public static string GetDataURL(string imgFile) {
            return "<img src=\"data:image/"
                        + Path.GetExtension(imgFile).Replace(".", "")
                        + ";base64,"
                        + Convert.ToBase64String(File.ReadAllBytes(imgFile)) + "\" />";
        }
    }
}
