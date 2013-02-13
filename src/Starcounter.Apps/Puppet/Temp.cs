// ***********************************************************************
// <copyright file="Temp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter {

    /// <summary>
    /// Struct Media
    /// </summary>
    public struct Media {
        /// <summary>
        /// The content
        /// </summary>
        public HttpResponse Content;

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Media" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Media(string str) {
            return new Media() { Content = new HttpResponse(str) };
        }
        /// <summary>
        /// Performs an implicit conversion from <see cref="HttpResponse" /> to <see cref="Media" />.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Media(HttpResponse content) {
            return new Media() { Content = content };
        }
    }
}