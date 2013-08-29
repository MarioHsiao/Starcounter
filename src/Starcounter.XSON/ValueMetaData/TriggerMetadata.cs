// ***********************************************************************
// <copyright file="ActionMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ActionMetadata<JsonType> : ValueMetadata<JsonType,TTrigger>
            where JsonType : Json {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="prop">The prop.</param>
        public ActionMetadata(JsonType app, TTrigger prop) : base(app, prop) { }
    }
}