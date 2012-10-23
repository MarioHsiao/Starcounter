﻿// ***********************************************************************
// <copyright file="StringMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// Class StringMetadata
    /// </summary>
    public class StringMetadata : ValueMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
       public StringMetadata(App app, Template prop) : base(app, prop) { }
//         public event Action<string> Changed;

    }
}