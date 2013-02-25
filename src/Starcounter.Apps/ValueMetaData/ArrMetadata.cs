// ***********************************************************************
// <copyright file="ArrMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// Class ArrMetadata
    /// </summary>
    /// <typeparam name="AppType">The type of the app type.</typeparam>
    /// <typeparam name="TemplateType">The type of the template type.</typeparam>
    public class ArrMetadata<AppType,TemplateType> : ValueMetadata {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="prop"></param>
        public ArrMetadata(Obj app, Template prop) : base(app, prop) { }

    }
}