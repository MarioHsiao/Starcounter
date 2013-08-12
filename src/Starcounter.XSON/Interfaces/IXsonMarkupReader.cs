
using Starcounter.Templates;
namespace Starcounter.Advanced.XSON {

    /// <summary>
    /// Starcounter provides JSON-by-example to create XSON templates from
    /// JSON files out-of-the-box. You can however provide custom markup languages
    /// to create templates.
    /// </summary>
    public interface IXsonTemplateMarkupReader {

        
        /// <summary>
        /// Converts markup to a TObj template
        /// </summary>
        /// <typeparam name="TTObj">The type to create</typeparam>
        /// <param name="format">The format (for example "json")</param>
        /// <param name="markup">The markup (for example {"Name":""}</param>
        /// <param name="origin">Optional origin (i.e. file name) where the markup was obtained. Usefull for debugging in case of syntax errors in the markup.</param>
        /// <returns>The newly created template</returns>
        TypeTObj CompileMarkup<TypeObj, TypeTObj>(string markup, string origin)
            where TypeObj : Obj, new()
            where TypeTObj : TObj, new();

    }
}
