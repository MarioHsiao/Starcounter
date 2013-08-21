
using System;
using System.Text;
namespace Starcounter.Internal {
    public static class TreeHelper {

        /// <summary>
        /// Dumps a tree into a multiline string with a indented tree structure.
        /// Prints the value to ToString() for each node in three tree.
        /// </summary>
        /// <param name="tree">The tree to dump</param>
        /// <param name="what">A delegate to generate the printed string for each node. If null, the ToString() method will be used</param>
        /// <returns>A string</returns>
        public static string GenerateTreeString(IReadOnlyTree tree, Func<IReadOnlyTree,string> what = null ) {
            if (what == null) {
                what = (IReadOnlyTree node) => node.ToString();
            }
            StringBuilder sb = new StringBuilder();
            _WriteTreeNode(sb, tree, 0, what );
            return sb.ToString();
        }

        private static void _WriteTreeNode(StringBuilder sb, IReadOnlyTree node, int indent, Func<IReadOnlyTree, string> what) {
            sb.Append(' ', indent);
            sb.AppendLine( what.Invoke(node) );
            foreach (var kid in node.Children) {
                _WriteTreeNode(sb, kid, indent + 3, what);
            }
        }
    }
}
