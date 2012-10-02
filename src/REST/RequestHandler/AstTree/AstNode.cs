using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// </summary>
    public abstract class AstNode {

        const int INDENTATION=3;

        /// <summary>
        /// See Parent
        /// </summary>
        private AstNode _Parent;

        /// <summary>
        /// Each node has a parent. The DOM tree allows you to move a node to a new parent, thus
        /// making refactoring easier. The refactoring capabilities are used by the Json Attributes
        /// to enable the user to place class declarations without having to nest them deeply.
        /// </summary>
        public AstNode Parent {
            get {
                return _Parent;
            }
            set {
                if (_Parent != null) {
                    if (!_Parent.Children.Remove(this))
                        throw new Exception();
                }
                _Parent = value;
                _Parent.Children.Add(this);
            }
        }

        private List<string> _Prefix = new List<string>();

        private List<string> _Suffix = new List<string>();

        /// <summary>
        /// Each node will carry source code in the form of text lines as either
        /// prefix or suffix blocks. This is the prefix block.
        /// </summary>
        internal List<string> Prefix { get { return _Prefix; } }

        /// <summary>
        /// As the DOM nodes only carry blocks of classes or members, there is a simple child/parent
        /// relationship between nodes. In addition, nodes may point to other nodes in the derived
        /// node classes. For instance, a property member node may point to its type in addition to its
        /// declaring class (its Parent).
        /// </summary>
        internal List<AstNode> Children = new List<AstNode>();

        /// <summary>
        /// Each node will carry source code in the form of text lines as either
        /// prefix or suffix blocks. This is the prefix block.
        /// </summary>
        internal List<string> Suffix { get { return _Suffix; } }

        /// <summary>
        /// Used by the code generator to calculation pretty text indentation for the generated source code.
        /// </summary>
        internal int Indentation { get; set; }


        public override string ToString() {
            return DumpTree();
        }

        internal AstNamespace Root {
            get {
                if (this is AstNamespace)
                    return this as AstNamespace;
                return Parent.Root;
            }
        }

        internal AstRequestProcessorClass TopClass {
            get {
                return Root.Children[0] as AstRequestProcessorClass;
            }
        }

        /// <summary>
        /// Returns a multiline string representation of the code dom tree
        /// </summary>
        /// <returns>A multiline string</returns>
        internal string DumpTree() {
            var sb = new StringBuilder();
            DumpTree(sb, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Returns a multiline string representation of the code dom tree
        /// </summary>
        /// <returns>A multiline string</returns>
        public string GenerateCsSourceCode() {
            var sb = new StringBuilder();
            _GenerateCsCode();
            WriteCode(sb, 0);
            return sb.ToString();
        }

        internal void _GenerateCsCode() {
            GenerateCsCodeForNode();
            foreach ( var kid in Children ) {
                kid._GenerateCsCode();
            }
        }

        internal virtual string DebugString {
            get {
                return this.GetType().Name;
            }
        }

        internal abstract void GenerateCsCodeForNode();

        internal virtual bool IndentChildren {
            get {
                return true;
            }
        }

        /// <summary>
        /// Appends the generated C# code to the supplied string builder. This 
        /// method recursivly adds the code of the child nodes
        /// </summary>
        /// <param name="sb">The stringbuilder used</param>
        /// <param name="indent">The indentation at this tree node level</param>
        private void WriteCode(StringBuilder sb, int indent) {
            foreach (var str in Prefix) {
                if (str != "")
                    sb.Append(' ', indent);
                sb.AppendLine(str);
            }
            foreach (var kid in Children) {
                kid.WriteCode(sb, indent + (IndentChildren?INDENTATION:0) );
            }
            foreach (var str in Suffix) {
                if (str != "")
                    sb.Append(' ', indent);
                sb.AppendLine(str);
            }
        }

        private void DumpTree(StringBuilder sb, int indent) {
            var str = DebugString;
            sb.Append(' ', indent);
            sb.AppendLine(DebugString);
//            if (IndentChildren)
                indent += INDENTATION;
            foreach (var kid in Children) {
                kid.DumpTree(sb, indent );
            }
        }
    }

}
