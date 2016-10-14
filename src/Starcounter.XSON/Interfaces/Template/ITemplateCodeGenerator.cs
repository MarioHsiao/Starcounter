// ***********************************************************************
// <copyright file="ITemplateCodeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;

namespace Starcounter.XSON.Interfaces {
    /// <summary>
    /// Interface ITemplateCodeGenerator
    /// </summary>
    public interface ITemplateCodeGenerator {
        /// <summary>
        /// Generates the code.
        /// </summary>
        /// <returns>System.String.</returns>
        string GenerateCode();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IReadOnlyTree GenerateAST();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string DumpAstTree();
    }
}
