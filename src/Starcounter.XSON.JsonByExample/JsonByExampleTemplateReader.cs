

using Modules;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal.XSON.JsonByExample;
using System;

namespace Starcounter.Internal.XSON.JsonByExample {

    /// <summary>
    /// 
    /// </summary>
    public class JsonByExampleTemplateReader : IXsonTemplateMarkupReader {

        /// <summary>
        /// Converts markup to a TObj template
        /// </summary>
        /// <typeparam name="TTObj">The type to create</typeparam>
        /// <param name="format">The format (for example "json")</param>
        /// <param name="markup">The markup (for example {"Name":""}</param>
        /// <param name="origin">Optional origin (i.e. file name) where the markup was obtained. Usefull for debugging in case of syntax errors in the markup.</param>
        /// <returns>The newly created template</returns>
        public TypeTObj CompileMarkup<TypeObj,TypeTObj>(string markup, string origin)
            where TypeObj : Obj, new()
            where TypeTObj : TObj, new() {
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
            where TypeObj : Obj, new()
            where TypeTObj : TObj, new() {
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
