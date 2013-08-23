
using Starcounter.XSON.Metadata;
using System;
namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Represents a type (i.e. a class) in the abstract syntax tree (AST tree)
    /// </summary>
    public abstract class AstClass : AstBase {

        private AstClass _InheritedClass;

        /// <summary>
        /// The inherited class (if any)
        /// </summary>
        public AstClass InheritedClass {
            get {
                return _InheritedClass;
            }
            set {
                _InheritedClass = value;
            }
        }

        private CodeBehindClassInfo _MatchedClass;

        /// <summary>
        /// If there is code-behind, the matched (user provided)
        /// class information that matches this system generated
        /// class goes here.
        /// </summary>
        public CodeBehindClassInfo MatchedClass {
            set {
                _MatchedClass = value;
            }
            get {
                return _MatchedClass;
            }
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <param name="generator">The dom generator instance</param>
        public AstClass(Gen2DomGenerator generator)
            : base(generator) {
        }

        public override string Name {
            get { return ClassStemIdentifier; }
        }

        /// <summary>
        /// Gets the name of the class without namespaces, owner classes or generics.
        /// </summary>
        /// <value>The name of the class.</value>
        public abstract string ClassStemIdentifier { get; }

        /// <summary>
        /// Returns the class reference text for the inherited class of this class
        /// </summary>
        /// <remarks>
        /// Returns null if there is no inherited type
        /// </remarks>
        public virtual string Inherits {
            get {
                if (InheritedClass == null)
                    return null;
                return InheritedClass.GlobalClassSpecifier;
            }
        }

        /// <summary>
        /// An array of types in the generics part of this class
        /// </summary>
        /// <value>The generic types</value>
        public AstClass[] Generic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this class is to be declared as partial.
        /// </summary>
        /// <value><c>true</c> if the declared class should be partial; otherwise, <c>false</c>.</value>
        public bool IsPartial { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this class is to be declared as static.
        /// </summary>
        /// <value><c>true</c> if the declared class should be static; otherwise, <c>false</c>.</value>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets the full name of the class including generics but without namespace or outer classes.
        /// </summary>
        /// <value>The full name of the class.</value>
        public string ClassSpecifierWithoutOwners {
            get {
                var str = ClassStemIdentifier;

                if (Generic != null) {
                    str += "<" + GetSafeGeneric(Generic,0);
                    for (int y=1;y<Generic.Length;y++) {
                        str += ","+GetSafeGeneric(Generic,y);
                    }
                    str += ">";
                }
                return str;
            }
        }

        private string GetSafeGeneric(AstClass[] arr, int index) {
            if (arr[index]==null) {
                return "(??na??)";
            }
            return Generic[index].GlobalClassSpecifier;
        }

        private string _NamespaceAlias = "global::";

        /// <summary>
        /// A namespace alias can be used to shorten the generated source code
        /// </summary>
        public string NamespaceAlias {
            get {
                if (MatchedClass != null)
                    return null;
                return _NamespaceAlias;
            }
            set {
                _NamespaceAlias = value;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        /// <exception cref="System.Exception"></exception>
        public override string ToString() {
            if (ClassStemIdentifier != null) {
                var str = "CLASS " + ClassStemIdentifier;
                    str += ":" + Inherits;
                return str;
            }
            return base.ToString();
        }


        /// <summary>
        /// In the below example, this property will contain "T,T2"
        /// <example>
        /// class MyStuff<T,T2> : Json<T> { ... }"/>
        /// </example>
        /// In the below example, this property will contain null
        /// <example>
        /// class MyStuff : Json<object> { ... }"/>
        /// </example>
        /// </summary>
        public virtual string Generics { get; set; }

        private string _Namespace;

        /// <summary>
        /// The namespace is calculated from the parent AstNodes unless
        /// there is a matched code behind class or external class with a
        /// given namespace.
        /// </summary>
        public virtual string Namespace {
            get {
                if (MatchedClass == null) {
                    if (Parent is AstJsonClass) {
                            return (Parent as AstJsonClass).Namespace;
                    }
                    return _Namespace;
                }
#if DEBUG
                if (_Namespace != null)
                    throw new Exception("Namespace conflict");
#endif
                return MatchedClass.Namespace;
            }
            set {
                _Namespace = value;
            }
        }

        private string _GlobalClassSpecifier = null;

        /// <summary>
        /// The global class specifier points out one exact type 
        /// (i.e. global::mynamespace.subnamespace.class.subclass)
        /// </summary>
        public virtual string GlobalClassSpecifier {
            get {
                if (_GlobalClassSpecifier != null) {
                    return _GlobalClassSpecifier;
                }
                if (Parent == null || !(Parent is AstClass)) {
                    var str = NamespaceAlias;
                    if (Namespace != null)
                        str += Namespace + ".";
                    if (MatchedClass != null) {
                        return MatchedClass.GlobalClassSpecifier;
                    }
                    //                    return "§[" + str + ClassSpecifierWithoutOwners + "]";
                    return str + ClassSpecifierWithoutOwners;
                }
                else {
                    return (Parent as AstClass).GlobalClassSpecifier + "." + ClassSpecifierWithoutOwners;
                }

            }
            set {
                _GlobalClassSpecifier = value;
            }
        }


        /// <summary>
        /// The global class specifier points out one exact type 
        /// (i.e. global::mynamespace.subnamespace.class.subclass)
        /// </summary>
        public virtual string GlobalClassSpecifierWithoutGenerics {
            get {
                var str = GlobalClassSpecifier;
                var index = str.IndexOf('<');
                if (index >= 0)
                    return str.Substring(0, index);
                return str;                
            }
        }
    }
}
