
using Starcounter;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Internal.XSON.JsonByExample;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Modules {

    /// <summary>
    /// Represents this module
    /// </summary>
    public static class Starcounter_XSON_JsonByExample {

        /// <summary>
        /// Contains all dependency injections into this module
        /// </summary>
        internal static class Injections {
            //            internal static ;
        }


        /// <summary>
        /// By default, Starcounter creates
        /// a JSON-by-example reader that allows you to convert a JSON file to a XOBJ template using the format
        /// string "json". You can inject other template formats here.
        /// </summary>
        public static Dictionary<string, IXsonTemplateMarkupReader> MarkupReaders = new Dictionary<string, IXsonTemplateMarkupReader>();

        public static void Initialize() {
            MarkupReaders.Add("json", new JsonByExampleTemplateReader());
        }



        /// <summary>
        /// Creates a json template based on the input json.
        /// </summary>
        /// <param name="script2">The json</param>
        /// <param name="restrictToDesigntimeVariable">if set to <c>true</c> [restrict to designtime variable].</param>
        /// <returns>an TObj instance</returns>
        public static TypeTObj CreateFromJs<TypeObj,TypeTObj>(string script2, bool restrictToDesigntimeVariable)
            where TypeObj : Json<object>, new()
            where TypeTObj : Schema<TypeObj>, new()
        {
            return _CreateFromJs<TypeObj,TypeTObj>(script2, "unknown", restrictToDesigntimeVariable); //ignoreNonDesignTimeAssignments);
        }

        ///
        public static TypeTObj _CreateFromJs<TypeObj, TypeTObj>(string source,
                                           string sourceReference,
                                           bool ignoreNonDesignTimeAssigments)
            where TypeObj : Json<object>, new()
            where TypeTObj : Schema<TypeObj>, new() {

                return JsonByExampleTemplateReader._CreateFromJs<TypeObj, TypeTObj>(source, sourceReference, ignoreNonDesignTimeAssigments); 
        }
  



        /// <summary>
        /// Reads the file and generates a typed json template.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>a Schema<Json<object>> instance</returns>
        public static Schema<Json<object>> ReadJsonTemplateFromFile(string fileSpec)
        {
            string content = ReadUtf8File(fileSpec);
            var t = _CreateFromJs<Json<object>, Schema<Json<object>>>(content, fileSpec, false);
            if (t.ClassName == null)
            {
                t.ClassName = Path.GetFileNameWithoutExtension(fileSpec);
            }
            return (Schema<Json<object>>)t;
        }


//        public static TObj CreateJsonTemplate(string className, string json) {
//            TObj tobj = CreateFromJs(json, false);
//            if (className != null)
//                tobj.ClassName = className;
//            return tobj;
//        }

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
        public static TApp CreateFromHtmlFile(string fileSpec) {
            string str = ReadUtf8File(fileSpec);
            TApp template = null;
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