// ***********************************************************************
// <copyright file="DecimalMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates
{
    /// <summary>
    /// 
    /// </summary>
	public class DecimalMetadata : ValueMetadata
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
		public DecimalMetadata(Obj app, Template prop) : base(app, prop) { }

	}
}
