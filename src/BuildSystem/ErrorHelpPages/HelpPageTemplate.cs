﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ErrorHelpPages {
    
    public sealed class HelpPageTemplate {
        static class Tokens {
            public const string TemplateStart = "<!--Template/-->";
            public const string MetadataStart = "<!--TemplateMetadata-->";
            public const string MetadataStop = "<!--TemplateMetadata/-->";
            public const string AutoGeneratedContentStart = "<!--TemplateAutoGenerated-->";
            public const string AutoGeneratedContentStop = "<!--TemplateAutoGenerated/-->";
            public const string UserContentStart = "<!--TemplateUserContent-->";
            public const string UserContentStop = "<!--TemplateUserContent/-->";
        }

        public readonly string FilePath;

        public string[] Metadata { get; private set; }
        public string[] AutoGenerated { get; private set; }
        public string[] UserContent { get; private set; }
        
        private HelpPageTemplate(string file) {
            if (!File.Exists(file)) {
                throw new FileNotFoundException("Template file not found", file);
            }
            this.FilePath = file;
            this.Metadata = null;
            this.AutoGenerated = null;
            this.UserContent = null;
        }

        public static HelpPageTemplate Read(string templateFilePath) {
            if (string.IsNullOrEmpty(templateFilePath)) {
                throw new ArgumentNullException("templateFilePath");
            }

            var t = new HelpPageTemplate(templateFilePath);
            t.Read();

            return t;
        }

        /// <summary>
        /// Template structure:
        /// 0-n: lines we ignore.
        /// <!--Template/-->
        /// <!--TemplateMetadata-->
        /// All this content is added first on every page, once created.
        /// No variable expansion occur.
        /// <!--TemplateMetadata/-->
        /// <!--TemplateAutoGenerated-->
        /// Ends up in the AutoGenerated section of the page, once it is
        /// created. Should never be modified "by hand" once on the page.
        /// <!--TemplateAutoGenerated/-->
        /// <!--TemplateUserContent-->
        /// Ends up in the user section of the page. Variable expansions
        /// occur when a page is first created, but this content is never
        /// touched by GoodTimes thereafter.
        /// <!--TemplateUserContent/-->
        /// </summary>
        void Read() {
            var content = File.ReadAllLines(FilePath);
            var e = content.AsEnumerable<string>().GetEnumerator();
            while (e.MoveNext()) {
                var line = e.Current;
                if (line.Equals(Tokens.TemplateStart)) {
                    ReadTemplateContent(e);
                    return;
                }
            }

            RaiseFormatError();
        }

        void ReadTemplateContent(IEnumerator<string> e) {
            var metadata = new List<string>();
            var autoGenerated = new List<string>();
            var userContent = new List<string>();

            // Move pass the template start token.
            // Expect the start of the metadata section.

            Assert(e.MoveNext());
            while (e.Current.Trim().Equals(string.Empty)) {
                if (!e.MoveNext()) RaiseFormatError();
            }

            // Parse the content of the metadata-, auto-generated
            // and user content sections. Allow spaces/blank lines
            // between them, but enforce they are all present and
            // well-formatted or raise an exception.

            Assert(e.Current.Equals(Tokens.MetadataStart));
            while (e.MoveNext()) {
                if (e.Current.Equals(Tokens.MetadataStop)) {
                    this.Metadata = metadata.ToArray();
                    break;
                }
                metadata.Add(e.Current);
            }
            Assert(this.Metadata != null);
            while (e.MoveNext()) {
                if (!e.Current.Trim().Equals(string.Empty))
                    break;
            }

            Assert(e.Current.Equals(Tokens.AutoGeneratedContentStart));
            while (e.MoveNext()) {
                if (e.Current.Equals(Tokens.AutoGeneratedContentStop)) {
                    this.AutoGenerated = autoGenerated.ToArray();
                    break;
                }
                autoGenerated.Add(e.Current);
            }
            Assert(this.AutoGenerated != null);
            while (e.MoveNext()) {
                if (!e.Current.Trim().Equals(string.Empty))
                    break;
            }

            Assert(e.Current.Equals(Tokens.UserContentStart));
            while (e.MoveNext()) {
                if (e.Current.Equals(Tokens.UserContentStop)) {
                    this.UserContent = userContent.ToArray();
                    break;
                }
                userContent.Add(e.Current);
            }
            Assert(this.UserContent != null);
        }

        void Assert(bool result) {
            if (!result) RaiseFormatError();
        }

        void RaiseFormatError() {
            throw new FormatException();
        }
    }
}
