// ***********************************************************************
// <copyright file="IntMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates
{
    /// <summary>
    /// Class IntMetadata
    /// </summary>
	public class IntMetadata : ValueMetadata
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="IntMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
		public IntMetadata(Obj app, Template prop) : base(app, prop) { }

	}
}
