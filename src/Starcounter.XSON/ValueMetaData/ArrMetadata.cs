// ***********************************************************************
// <copyright file="ArrMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="AppType"></typeparam>
    /// <typeparam name="TemplateType"></typeparam>
    public class ArrMetadata<AppType,TemplateType> : ValueMetadata {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="prop"></param>
        public ArrMetadata(Obj app, Template prop) : base(app, prop) { }

    }
}