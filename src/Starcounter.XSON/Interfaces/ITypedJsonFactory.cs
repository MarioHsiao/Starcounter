// ***********************************************************************
// <copyright file="IAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Internal;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// 
    /// </summary>
    public interface ITypedJsonFactory {

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //object CreateJsonInstance(string json);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        TObj CreateJsonTemplate(string className, string json);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        TObj CreateJsonTemplateFromFile(string filePath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonFilePath"></param>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        string GenerateTypedJsonCode(string jsonFilePath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonFilePath"></param>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        string GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath);

//        ICodeBehindMetadata CreateCodeBehindMetadata(string className, string codeBehindFilePath);
    }
}