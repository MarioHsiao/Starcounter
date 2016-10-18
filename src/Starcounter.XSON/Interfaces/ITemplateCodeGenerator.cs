using System.Collections.Generic;
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

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<ITemplateCodeGeneratorWarning> Warnings { get; }
    }
}
