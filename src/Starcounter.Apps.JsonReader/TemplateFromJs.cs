
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.JsonReader
{
    public class TemplateFromJs
    {
        public static AppTemplate CreateFromJs(string script2, bool restrictToDesigntimeVariable)
        {
            return _CreateFromJs(script2, "unknown", restrictToDesigntimeVariable); //ignoreNonDesignTimeAssignments);
        }

        private static AppTemplate _CreateFromJs(string source,
                                                 string sourceReference,
                                                 bool ignoreNonDesignTimeAssigments)
        {
            AppTemplate appTemplate;
            ITemplateFactory factory = new Internal.AppTemplateFactory();
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
