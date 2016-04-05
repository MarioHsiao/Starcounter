using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Starcounter.XSON.PartialClassGenerator {

    /// <summary>
    /// Helpers when working with Roslyn syntax trees.
    /// </summary>
    internal static class RoslynSyntaxHelpers {
        /// <summary>
        /// Returns the full namespace of a class node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetFullNamespace(ClassDeclarationSyntax node) {
            // class Foo {} = ""
            // namespace Outer { class Foo {} } = "Outer"
            // namespace Outer { namespace Inner { class Foo {} } } = "Outer.Inner"
            // namespace Outer { namespace Inner { class Foo { class Bar } } } = "" (when passing in Bar)

            var result = string.Empty;
            var ns = node.Parent as NamespaceDeclarationSyntax;
            while (ns != null) {
                result = ns.Name.ToString() + "." + result;
                ns = ns.Parent as NamespaceDeclarationSyntax;
            }
            return result.TrimEnd('.');
        }
    }
}
