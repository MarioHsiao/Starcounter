
namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Represents the root JSON object metadata, used during parsing.
    /// </summary>
    internal sealed class RootClass {
        public readonly string Name;

        public RootClass(string name) {
            Name = name;
        }
    }
}
