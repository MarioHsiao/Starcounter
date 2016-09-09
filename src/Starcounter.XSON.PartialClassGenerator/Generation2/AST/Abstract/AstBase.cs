using System;
using System.Collections.Generic;
using Starcounter.Internal;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// The DOM tree node base class. The simple DOM tree node is a coarse node tree that
    /// handles classes and properties as block elements. I.e. a complete tree representing
    /// the generated source code (for a JSON template) contains just a new nodes whereas a
    /// complete C# syntax tree would consist of a more complex tree. This makes creating,
    /// manipulating easier and also anables simplicity in source code text generation.
    /// </summary>
    public abstract class AstBase : IReadOnlyTree {
        public bool IsProcessedForAlias = false;   

        /// <summary>
        /// 
        /// </summary>
        public Gen2DomGenerator Generator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstBase(Gen2DomGenerator gen) {
            if (gen == null)
                throw new Exception("The generator must be given");
            Generator = gen;
        }

        internal virtual string CalculateClassAliasIdentifier(int chars) {
            if (Parent != null)
                return Parent.CalculateClassAliasIdentifier(chars);
            return "";
        }


        /// <summary>
        /// </summary>
        private AstBase _SourceParent;

        /// <summary>
        /// 
        /// </summary>
        public AstBase SourceParent {
            get {
                return _SourceParent;
            }
            set {
                if (_SourceParent != null && _SourceParent != value) {
                        throw new Exception();
                }
                _SourceParent = value;
            }
        }

        /// <summary>
        /// See Parent
        /// </summary>
        private AstBase _Parent;

        /// <summary>
        /// Each node has a parent. The DOM tree allows you to move a node to a new parent, thus
        /// making refactoring easier. The refactoring capabilities are used by the Json Attributes
        /// to enable the user to place class declarations without having to nest them deeply.
        /// </summary>
        /// <value>The parent.</value>
        /// <exception cref="System.Exception"></exception>
        public AstBase Parent {
            get {
                return _Parent;
            }
            set {
                if (_Parent != null) {
                    if (!_Parent.Children.Remove(this))
                        throw new Exception();
                }
                //if (SourceParent == null)
                //    SourceParent = value;
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
        /// <value>The prefix.</value>
        public List<string> Prefix { get { return _Prefix; } }

        /// <summary>
        /// As the DOM nodes only carry blocks of classes or members, there is a simple child/parent
        /// relationship between nodes. In addition, nodes may point to other nodes in the derived
        /// node classes. For instance, a property member node may point to its type in addition to its
        /// declaring class (its Parent).
        /// </summary>
        public List<AstBase> Children = new List<AstBase>();

        /// <summary>
        /// Each node will carry source code in the form of text lines as either
        /// prefix or suffix blocks. This is the prefix block.
        /// </summary>
        /// <value>The suffix.</value>
        public List<string> Suffix { get { return _Suffix; } }

        /// <summary>
        /// Used by the code generator to calculation pretty text indentation for the generated source code.
        /// </summary>
        /// <value>The indentation.</value>
        public int Indentation { get; set; }


        /// <summary>
        /// An AST node is a tree node
        /// </summary>
        IReadOnlyList<IReadOnlyTree> IReadOnlyTree.Children {
            get {
                return (IReadOnlyList<IReadOnlyTree>)Children;
            }
        }

        /// <summary>
        /// An AST node is a tree node
        /// </summary>
        IReadOnlyTree IReadOnlyTree.Parent {
            get {
                return Parent;
            }
        }
        
        public virtual bool MarkAsCodegen {
            get {
                return false;
            }

        }

    }
}
