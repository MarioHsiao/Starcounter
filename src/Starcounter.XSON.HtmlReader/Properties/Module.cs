﻿using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using Starcounter.Templates;
using Modules;

using Starcounter.Advanced.XSON;
using Starcounter;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;

namespace Modules {

    /// <summary>
    /// Represents this module
    /// </summary>
    internal static class Starcounter_XSON_HtmlReader {

        /// <summary>
        /// Contains all dependency injections into this module
        /// </summary>
        internal static class Injections {

        }

        /// <summary>
        /// Reads the UTF8 file.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>System.String.</returns>
        private static string ReadUtf8File(string fileSpec) {
            FileStream fs = File.OpenRead(fileSpec);
            long len = fs.Length;
            var buffer = new byte[len];
            fs.Read(buffer, 0, (int)len);
            fs.Close();
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Creates from HTML file.
        /// </summary>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>TJson</returns>
        /// <exception cref="System.Exception"></exception>
        public static TJson CreatePuppetTemplateFromHtmlFile(string fileSpec) {
            string str = ReadUtf8File(fileSpec);
            TJson template = null;
            var html = new HtmlDocument();
            bool shouldFindTemplate = (str.ToUpper().IndexOf("$$DESIGNTIME$$") >= 0);
            html.Load(new StringReader(str));
            foreach (HtmlNode link in html.DocumentNode.SelectNodes("//script")) {
                string js = link.InnerText;
                template = (TJson)Starcounter_XSON_JsonByExample.CreateFromJs<Json<object>, TJson>(js, true);
                if (template != null)
                    return template;
            }
            if (shouldFindTemplate)
                throw new Exception(String.Format("SCERR????. The $$DESIGNTIME$$ declaration is misplaced in file {0}. The $$DESIGNTIME$$ template should be put in a separate <script> tag.", fileSpec));
            return null;
        }



    }
}