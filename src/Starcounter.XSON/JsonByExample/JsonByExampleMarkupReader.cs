using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.XSON.JsonByExample {
    public class JsonByExampleMarkupReader : IXsonTemplateMarkupReader {
        public Template CreateTemplate(string markup, string source) {
            using (var parser = new JsonByExampleParser(markup, source)) {
                return parser.CreateTemplate();
            }
        }
    }
}
