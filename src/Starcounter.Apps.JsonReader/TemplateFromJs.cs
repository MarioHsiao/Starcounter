// ***********************************************************************
// <copyright file="TemplateFromJs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonTemplate
{
    /// <summary>
    /// Class TemplateFromJs
    /// </summary>
    public class TemplateFromJs
    {
        /// <summary>
        /// Creates from js.
        /// </summary>
        /// <param name="script2">The script2.</param>
        /// <param name="restrictToDesigntimeVariable">if set to <c>true</c> [restrict to designtime variable].</param>
        /// <returns>AppTemplate.</returns>
        public static AppTemplate CreateFromJs(string script2, bool restrictToDesigntimeVariable)
        {
            return _CreateFromJs(script2, "unknown", restrictToDesigntimeVariable); //ignoreNonDesignTimeAssignments);
        }

        /// <summary>
        /// _s the create from js.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceReference">The source reference.</param>
        /// <param name="ignoreNonDesignTimeAssigments">if set to <c>true</c> [ignore non design time assigments].</param>
        /// <returns>AppTemplate.</returns>
        private static AppTemplate _CreateFromJs(string source,
                                                 string sourceReference,
                                                 bool ignoreNonDesignTimeAssigments)
        {
            AppTemplate appTemplate;
            ITemplateFactory factory = new Internal.JsonTemplate.AppTemplateFactory<App,AppTemplate>();
            int skip = 0;
            if (!ignoreNonDesignTimeAssigments)
            {
                source = "(" + source + ")";
                skip++;
            }
            appTemplate = (AppTemplate)Materializer.BuiltTemplate(source,
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
        /// <param name="parentTemplate">The parent template.</param>
        private static void VerifyTemplates(ParentTemplate parentTemplate)
        {
            CompilerOrigin co;

            if (parentTemplate == null) return;

            foreach (Template t in (IEnumerable<Template>)parentTemplate.Children)
            {
                if (t is ReplaceableTemplate)
                {
                    co = t.CompilerOrigin;
                    Error.CompileError.Raise<object>(
                                "Metadata but no field for '" + t.Name + "' found",
                                new Tuple<int, int>(co.LineNo, co.ColNo),
                                co.FileName);
                }

                if (t is ParentTemplate)
                    VerifyTemplates((ParentTemplate)t);
            }
        }


        /// <summary>
        /// Reads the file.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>AppTemplate.</returns>
        public static AppTemplate ReadFile(string fileSpec)
        {
            string content = ReadUtf8File(fileSpec);
            var t = _CreateFromJs(content, fileSpec, false);
            if (t.ClassName == null)
            {
                t.ClassName = Paths.StripFileNameWithoutExtention(fileSpec);
            }
            return t;
        }

        /// <summary>
        /// Reads the UTF8 file.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>System.String.</returns>
        private static string ReadUtf8File(string fileSpec)
        {

            byte[] buffer = null;
            using (FileStream fileStream = new FileStream(
                fileSpec,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {

                long len = fileStream.Length;
                buffer = new byte[len];
                fileStream.Read(buffer, 0, (int)len);
            }

            return Encoding.UTF8.GetString(buffer);
        }

        /*
        public static AppTemplate CreateFromHtmlFile(string fileSpec) {
            string str = ReadUtf8File(fileSpec);
            AppTemplate template = null;
            var html = new HtmlDocument();
            bool shouldFindTemplate = (str.ToUpper().IndexOf("$$DESIGNTIME$$") >= 0);
            html.Load(new StringReader(str));
            foreach (HtmlNode link in html.DocumentNode.SelectNodes("//script")) {
                string js = link.InnerText;
                template = CreateFromJs(js, true);
                if (template != null)
                    return template;
            }
            if (shouldFindTemplate)
                throw new Exception(String.Format("SCERR????. The $$DESIGNTIME$$ declaration is misplaced in file {0}. The $$DESIGNTIME$$ template should be put in a separate <script> tag.", fileSpec));
            return null;
        }
         */
    }
}
