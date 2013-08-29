// ***********************************************************************
// <copyright file="BoolMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class BoolMetadata<JsonType> : ValueMetadata<JsonType,TBool>
        where JsonType : Json
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public BoolMetadata(JsonType app, TBool prop) : base(app, prop) { }

    }
}