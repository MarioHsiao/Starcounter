// ***********************************************************************
// <copyright file="ObjectMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ObjectMetadata : ValueMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public ObjectMetadata(Obj app, Template prop) : base(app, prop) { }

    }
}