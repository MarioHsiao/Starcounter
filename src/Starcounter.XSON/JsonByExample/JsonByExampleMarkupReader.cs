using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.JsonByExample {
    public class JsonByExampleMarkupReader : IXsonTemplateMarkupReader {
        public Template CreateTemplate(string markup, string source) {
            using (var parser = new JsonByExampleParser(markup, source)) {
                return parser.CreateTemplate();
            }
        }
    }
}
