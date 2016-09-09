using System;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.JSONByExample {
    /// <summary>
    /// 
    /// </summary>
    public class JsonByExampleTemplateReader : IXsonTemplateMarkupReader {
        ///// <summary>
        ///// Compile markup.
        ///// </summary>
        ///// <typeparam name="TJson"></typeparam>
        ///// <typeparam name="TTemplate"></typeparam>
        ///// <param name="markup"></param>
        ///// <param name="origin"></param>
        ///// <returns></returns>
        //public TTemplate CompileMarkup<TJson,TTemplate>(string markup, string origin)
        //    where TJson : Json, new()
        //    where TTemplate : TValue {
        //        return CreateFromJs<TJson, TTemplate>(markup, origin, false);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="source">The source.</param>
        ///// <param name="sourceReference">The source reference.</param>
        ///// <param name="ignoreNonDesignTimeAssigments">if set to <c>true</c> [ignore non design time assigments].</param>
        ///// <returns>TObj.</returns>
        //internal static TTemplate CreateFromJs<TJson, TTemplate>(string source, string sourceReference, bool ignoreNonDesignTimeAssigments)
        //    where TJson : Json, new()
        //    where TTemplate : TValue {
        //    TTemplate appTemplate;

        //    ITemplateFactory factory = new TAppFactory<TJson, TTemplate>();
        //    int skip = 0;
        //    if (!ignoreNonDesignTimeAssigments) {
        //        source = "(" + source + ")";
        //        skip++;
        //    }
        //    appTemplate = (TTemplate)Materializer.BuiltTemplate(source,
        //                                                   sourceReference,
        //                                                   skip,
        //                                                   factory,
        //                                                   ignoreNonDesignTimeAssigments
        //                                            );

        //    VerifyTemplates(appTemplate);
        //    return appTemplate;
        //}

        ///// <summary>
        ///// Verifies the templates.
        ///// </summary>
        ///// <param name="containerTemplate">The parent template.</param>
        //private static void VerifyTemplates(Template template) {
        //    CompilerOrigin co;
        //    TContainer container;

        //    if (template == null) return;

        //    if (template is ReplaceableTemplate) {
        //        co = template.SourceInfo;
        //        Starcounter.Internal.JsonTemplate.Error.CompileError.Raise<object>(
        //                    "Metadata but no field for '" + template.TemplateName + "' found",
        //                    new Tuple<int, int>(co.LineNo, co.ColNo),
        //                    co.FileName);
        //    }

        //    container = template as TContainer;
        //    if (container != null) {
        //        foreach (Template child in container.Children) {
        //            VerifyTemplates(child);
        //        }
        //    }
        //}
        public Template CompileMarkup(string markup, string origin) {
            throw new NotImplementedException();
        }
    }
}
