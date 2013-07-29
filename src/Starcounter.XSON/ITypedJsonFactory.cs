// ***********************************************************************
// <copyright file="IAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using Starcounter.XSON.Serializers;

namespace Starcounter.Internal {
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
        /// <returns></returns>
        TypedJsonSerializer CreateJsonSerializer(TObj jsonTemplate);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        CodeBehindMetadata CreateCodeBehindMetadata(string className, string codeBehindFilePath);
    }
}