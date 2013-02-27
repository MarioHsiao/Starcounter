// ***********************************************************************
// <copyright file="DoubleMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates
{
    /// <summary>
    /// 
    /// </summary>
	public class DoubleMetadata : ValueMetadata
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
		public DoubleMetadata(Obj app, Template prop) : base(app, prop) { }

	}
}
