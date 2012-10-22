﻿// ***********************************************************************
// <copyright file="ActionMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// Class ActionMetadata
    /// </summary>
    public class ActionMetadata : ValueMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public ActionMetadata(App app, Template prop) : base(app, prop) { }
    }
}