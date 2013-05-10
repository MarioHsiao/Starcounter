// ***********************************************************************
// <copyright file="IAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {


    /// <summary>
    /// 
    /// </summary>
    public interface IJsonFactory {

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //object CreateJsonInstance(string json);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        object CreateJsonTemplate(string json);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        object CreateJsonTemplateFromFile(string filePath);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        object CreateJsonSerializer(object jsonTemplate);

        /// <summary>
        /// 
        /// </summary>
        ICompilerService Compiler { get; }
    }
}