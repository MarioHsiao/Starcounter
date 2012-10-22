﻿// ***********************************************************************
// <copyright file="AstNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Class AstNode
    /// </summary>
    public abstract class AstNode {

        /// <summary>
        /// The INDENTATION
        /// </summary>
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
        /// <value>The parent.</value>
        /// <exception cref="System.Exception"></exception>
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

        /// <summary>
        /// The _ prefix
        /// </summary>
        private List<string> _Prefix = new List<string>();

        /// <summary>
        /// The _ suffix
        /// </summary>
        private List<string> _Suffix = new List<string>();

        /// <summary>
        /// Each node will carry source code in the form of text lines as either
        /// prefix or suffix blocks. This is the prefix block.
        /// </summary>
        /// <value>The prefix.</value>
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
        /// <value>The suffix.</value>
        internal List<string> Suffix { get { return _Suffix; } }

        /// <summary>
        /// Used by the code generator to calculation pretty text indentation for the generated source code.
        /// </summary>
        /// <value>The indentation.</value>
        internal int Indentation { get; set; }


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
            return DumpTree();
        }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <value>The root.</value>
        internal AstNamespace Root {
            get {
                if (this is AstNamespace)
                    return this as AstNamespace;
                return Parent.Root;
            }
        }

        /// <summary>
        /// Gets the top class.
        /// </summary>
        /// <value>The top class.</value>
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

        /// <summary>
        /// _s the generate cs code.
        /// </summary>
        internal void _GenerateCsCode() {
            GenerateCsCodeForNode();
            foreach ( var kid in Children ) {
                kid._GenerateCsCode();
            }
        }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal virtual string DebugString {
            get {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// Generates the cs code for node.
        /// </summary>
        internal abstract void GenerateCsCodeForNode();

        /// <summary>
        /// Gets a value indicating whether [indent children].
        /// </summary>
        /// <value><c>true</c> if [indent children]; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Dumps the tree.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="indent">The indent.</param>
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
