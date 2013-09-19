// ***********************************************************************
// <copyright file="AstNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.XSON.Serializer.Ast {
    /// <summary>
    /// 
    /// </summary>
    internal abstract class AstBase {
        private const int INDENTATION = 4;

        private AstBase parent;
        private List<string> prefix = new List<string>();
        private List<string> suffix = new List<string>();

        /// <summary>
        /// Each node has a parent. The DOM tree allows you to move a node to a new parent, thus
        /// making refactoring easier. The refactoring capabilities are used by the Json Attributes
        /// to enable the user to place class declarations without having to nest them deeply.
        /// </summary>
        /// <value>The parent.</value>
        /// <exception cref="System.Exception"></exception>
        public AstBase Parent {
            get {
                return parent;
            }
            set {
                if (parent != null) {
                    if (!parent.Children.Remove(this))
                        throw new Exception();
                }
                parent = value;
                parent.Children.Add(this);
            }
        }

        /// <summary>
        /// Each node will carry source code in the form of text lines as either
        /// prefix or suffix blocks. This is the prefix block.
        /// </summary>
        /// <value>The prefix.</value>
        internal List<string> Prefix { get { return prefix; } }

		/// <summary>
		/// Each node will carry source code in the form of text lines as either
		/// prefix or suffix blocks. This is the prefix block.
		/// </summary>
		/// <value>The suffix.</value>
		internal List<string> Suffix { get { return suffix; } }

        /// <summary>
        /// As the DOM nodes only carry blocks of classes or members, there is a simple child/parent
        /// relationship between nodes. In addition, nodes may point to other nodes in the derived
        /// node classes. For instance, a property member node may point to its type in addition to its
        /// declaring class (its Parent).
        /// </summary>
        internal List<AstBase> Children = new List<AstBase>();

        /// <summary>
        /// Used by the code generator to calculation pretty text indentation for the generated source code.
        /// </summary>
        /// <value>The indentation.</value>
        internal int Indentation { get; set; }

		/// <summary>
        /// 
        /// </summary>
        /// <value></value>
        internal virtual string DebugString {
            get {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// Gets a value indicating whether to use indentation when generating code for children.
        /// </summary>
        /// <value></value>
        internal virtual bool IndentChildren {
            get {
                return true;
            }
        }

		public override string ToString() {
			return DebugString;
		}

		public string ToString(bool includeChildren) {
			if (!includeChildren)
				return DebugString;

			var sb = new StringBuilder();
			DumpNode(this, sb, 0);
			return sb.ToString();
		}

		private void DumpNode(AstBase node, StringBuilder sb, int indentation) {
			sb.Append(' ', indentation);
			sb.Append(node.DebugString);
			sb.AppendLine();

			foreach (AstBase child in node.Children) {
				DumpNode(child, sb, indentation + INDENTATION);
			}
		}
    }
}
