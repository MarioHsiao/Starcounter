﻿
using Starcounter.Templates;
namespace Starcounter.Advanced.XSON {

    /// <summary>
    /// Starcounter provides JSON-by-example to create XSON templates from
    /// JSON files out-of-the-box. You can however provide custom markup languages
    /// to create templates.
    /// </summary>
    public interface IXsonTemplateMarkupReader {

        /// <summary>
        /// Compile markup.
        /// </summary>
        /// <typeparam name="TypeObj"></typeparam>
        /// <typeparam name="TypeTObj"></typeparam>
        /// <param name="markup"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        TTemplate CompileMarkup<TJson, TTemplate>(string markup, string origin)
            where TJson : Json, new()
            where TTemplate : TValue;

    }
}
