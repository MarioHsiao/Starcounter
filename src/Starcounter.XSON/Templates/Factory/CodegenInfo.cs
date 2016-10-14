using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    internal class CodegenInfo {
        internal string ClassName;
        internal string Namespace;
        internal string BoundToType;
        internal string ReuseType;
        internal ISourceInfo SourceInfo;
    }
}
