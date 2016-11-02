using System;

namespace Starcounter.XSON.Interfaces {
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
        ITemplateCodeGenerator CreateGenerator(Type defaultRootTemplateType, string lang, object template, object metadata);
    }
}
