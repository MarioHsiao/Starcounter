// ***********************************************************************
// <copyright file="IntMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates
{
    /// <summary>
    /// 
    /// </summary>
    public class LongMetadata<JsonType> : ValueMetadata<JsonType,TLong>
        where JsonType : Json<object>
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="LongMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public LongMetadata(JsonType app, TLong prop) : base(app, prop) { }

	}
}
