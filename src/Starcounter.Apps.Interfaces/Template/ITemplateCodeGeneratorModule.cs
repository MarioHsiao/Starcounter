// ***********************************************************************
// <copyright file="ITemplateCodeGeneratorModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates.Interfaces;
using System;
namespace Starcounter.Internal {
    /// <summary>
    /// Interface ITemplateCodeGeneratorModule
    /// </summary>
    public interface ITemplateCodeGeneratorModule {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultNewObjTemplateType"></param>
        /// <param name="lang"></param>
        /// <param name="template"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        ITemplateCodeGenerator CreateGenerator( Type defaultNewObjTemplateType, string lang, object template, object metadata );
    }
}
