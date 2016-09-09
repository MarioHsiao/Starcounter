
using Starcounter.Templates;
namespace Starcounter.XSON.Interfaces {

    /// <summary>
    /// Starcounter provides JSON-by-example to create XSON templates from
    /// JSON files out-of-the-box. You can however provide custom markup languages
    /// to create templates.
    /// </summary>
    public interface IXsonTemplateMarkupReader {
        /// <summary>
        /// Compile markup.
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        Template CompileMarkup(string markup, string origin);
    }
}
