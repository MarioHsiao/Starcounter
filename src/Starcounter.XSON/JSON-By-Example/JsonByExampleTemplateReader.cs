using System;
using System.Collections.Generic;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.JsonByExample {
    /// <summary>
    /// 
    /// </summary>
    public class JsonByExampleTemplateReader : IXsonTemplateMarkupReader {
        /// <summary>
        /// Compile markup.
        /// </summary>
        /// <typeparam name="TypeObj"></typeparam>
        /// <typeparam name="TypeTObj"></typeparam>
        /// <param name="markup"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public TypeTObj CompileMarkup<TypeObj,TypeTObj>(string markup, string origin)
            where TypeObj : Json, new()
            where TypeTObj : TObject, new() {
                return _CreateFromJs<TypeObj, TypeTObj>(markup, origin, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceReference">The source reference.</param>
        /// <param name="ignoreNonDesignTimeAssigments">if set to <c>true</c> [ignore non design time assigments].</param>
        /// <returns>TObj.</returns>
        internal static TypeTObj _CreateFromJs<TypeObj, TypeTObj>(string source,
                                          string sourceReference,
                                          bool ignoreNonDesignTimeAssigments)
            where TypeObj : Json, new()
            where TypeTObj : TObject, new() {
            TypeTObj appTemplate;

            ITemplateFactory factory = new TAppFactory<TypeObj, TypeTObj>();

            int skip = 0;
            if (!ignoreNonDesignTimeAssigments) {
                source = "(" + source + ")";
                skip++;
            }
            appTemplate = (TypeTObj)Materializer.BuiltTemplate(source,
                                                           sourceReference,
                                                           skip,
                                                           factory,
                                                           ignoreNonDesignTimeAssigments
                                                    ); //ignoreNonDesignTimeAssignments);

            VerifyTemplates(appTemplate);
            return appTemplate;
        }

        /// <summary>
        /// Verifies the templates.
        /// </summary>
        /// <param name="containerTemplate">The parent template.</param>
        private static void VerifyTemplates(TContainer containerTemplate) {
            CompilerOrigin co;

            if (containerTemplate == null) return;

            foreach (Template t in (IEnumerable<Template>)containerTemplate.Children) {
                if (t is ReplaceableTemplate) {
                    co = t.CompilerOrigin;
                    Starcounter.Internal.JsonTemplate.Error.CompileError.Raise<object>(
                                "Metadata but no field for '" + t.TemplateName + "' found",
                                new Tuple<int, int>(co.LineNo, co.ColNo),
                                co.FileName);
                }

                if (t is TContainer)
                    VerifyTemplates((TContainer)t);
            }
        }
    }
}
