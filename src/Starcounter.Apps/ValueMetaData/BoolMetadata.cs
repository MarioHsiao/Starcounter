﻿// ***********************************************************************
// <copyright file="BoolMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// Class BoolMetadata
    /// </summary>
    public class BoolMetadata : ValueMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public BoolMetadata(App app, Template prop) : base(app, prop) { }

    }
}