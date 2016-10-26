using System;
using Starcounter.Internal;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
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

        public virtual bool IsPrimitive {
            get {
                return false;
            }
        }

        private CodeBehindClassInfo _MatchedClass;

        /// <summary>
        /// If there is code-behind, the matched (user provided)
        /// class information that matches this system generated
        /// class goes here.
        /// </summary>
        public CodeBehindClassInfo CodebehindClass {
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

        /// <summary>
        /// Gets the name of the class without namespaces, owner classes or generics.
        /// </summary>
        /// <value>The name of the class.</value>
        public virtual string ClassStemIdentifier {
            get {
                if (_ClassStemIdentifier != null) {
                    return _ClassStemIdentifier;
//                    return "Gen(" + _ClassStemIdentifier + ")";
                }
                if (BuiltInType != null) {
                    return HelperFunctions.GetClassStemIdentifier(BuiltInType);
//                    return "Real(" + HelperFunctions.GetClassStemIdentifier(RealType) + ")";
                }
                return "UNKNOWN";
            }
            set {
                _ClassStemIdentifier = value;
            }
        }

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
                return str + GenericAsString();
            }
        }

        public string GenericAsString() {
            if (BuiltInType != null) {
                return GetGenericsSpecifier(BuiltInType);
            }
            else if (Generic != null) {
                var str = "<" + GetSafeGeneric(Generic, 0);
                for (int y = 1; y < Generic.Length; y++) {
                    str += "," + GetSafeGeneric(Generic, y);
                }
                str += ">";
                return str;
            }
            return "";
        }

        private static string GetGenericsSpecifier(Type type) {
            var ret = "";
            Type[] typeArguments = type.GetGenericArguments(); // GenericTypeArguments; // etGenericArguments();
            if (type.IsGenericType) {
                ret = ret + "<";
                var i = 0;
                foreach (Type tParam in typeArguments) {
                    if (i > 0)
                        ret = ret + ",";
                    ret = ret + HelperFunctions.GetClassDeclarationSyntax(tParam);
                    i++;
                }
                ret = ret + ">";
            }
            return ret;
        }

        private string GetSafeGeneric(AstClass[] arr, int index) {
            if (arr[index]==null) {
                return "(??na??)";
            }
            return Generic[index].GlobalClassSpecifier;
        }


        public AstClassAlias ClassAlias;

        private string _NamespaceAlias = "global::";

        /// <summary>
        /// A namespace alias can be used to shorten the generated source code
        /// </summary>
        public string NamespaceAlias {
            get {
                if (CodebehindClass != null)
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
            return this.GetType().Name + " " + this.GlobalClassSpecifier;
        }


        /// <summary>
        /// In the below example, this property will contain "T,T2"
        /// <example>
        /// class MyStuff<T,T2> : Json<T> { ... }"/>
        /// </example>
        /// In the below example, this property will contain null
        /// <example>
        /// class MyStuff : Json { ... }"/>
        /// </example>
        /// </summary>
        public virtual string Generics { get; set; }

        private string _Namespace;


        /// <summary>
        /// If not null, the namespace, identifier and classspecifier
        /// should be obtained from this actual type.
        /// </summary>
        public Type BuiltInType = null;

        /// <summary>
        /// The namespace is calculated from the parent AstNodes unless
        /// there is a matched code behind class or external class with a
        /// given namespace.
        /// </summary>
        public virtual string Namespace {
            get {
                if (_Namespace != null) {
                    return _Namespace;
                    //return "GEN(" + _Namespace + ")";
                }
                if (BuiltInType != null) {
                    //return "REAL(" + RealType.Namespace + ")";
                    return BuiltInType.Namespace;
                }
                if (CodebehindClass == null) {
                    if (Parent is AstJsonClass) {
                        return (Parent as AstJsonClass).Namespace;
                       // return "CODEBEHIND(" + (Parent as AstJsonClass).Namespace + ")";
                    }
                    return null;
                }
                return CodebehindClass.Namespace;
            }
            set {
                _Namespace = value;
            }
        }

        private string _GlobalClassSpecifier = null;
        protected string _ClassStemIdentifier = null;

        /// <summary>
        /// The global class specifier points out one exact type 
        /// (i.e. global::mynamespace.subnamespace.class.subclass)
        /// </summary>
        public virtual string GlobalClassSpecifier {
            get {
                if (ClassAlias != null && Generator.Root.AliasesActive) {
                    return ClassAlias.Alias;
                }
                return GetRawGlobalClassSpecifier();

            }
            set {
                _GlobalClassSpecifier = value;
            }
        }

        private string GetRawGlobalClassSpecifier() {
            if (_GlobalClassSpecifier != null) {
                //                    return "gen(" + _GlobalClassSpecifier + ")";
                return Clean(_GlobalClassSpecifier);
            }
            if (CodebehindClass != null) {
                return Clean(CodebehindClass.GlobalClassSpecifier);
            }
            if (Parent == null || !(Parent is AstClass)) {
                var str = NamespaceAlias;
                if (Namespace != null)
                    str += Namespace + ".";
                return Clean(str + ClassSpecifierWithoutOwners);
            }
            return (Parent as AstClass).GlobalClassSpecifier + "." + ClassSpecifierWithoutOwners;
        }

        private string Clean(string str) {
//            if (str.StartsWith("global::Starcounter.Templates.")) {
//                str = "st::" + str.Substring(30);
//            }
//            else if (str.StartsWith("global::Starcounter.")) {
//                str = "s::" + str.Substring(20);
//            }
//            if (Generator.Root!= null && str.StartsWith(Generator.Root.RootJsonClassAliasPrefix)) {
//                str = "uSr::" + str.Substring(Generator.Root.RootJsonClassAliasPrefix.Length - 1);
//            }
//            if (Generator.Root != null && str.Equals(Generator.Root.RootJsonClassAlias)) {
//                str = "uSr";
//            }
            return str;
        }


        /// <summary>
        /// The global class specifier points out one exact type 
        /// (i.e. global::mynamespace.subnamespace.class.subclass)
        /// </summary>
        public virtual string GlobalClassSpecifierWithoutGenerics {
            get {
                var str = GetRawGlobalClassSpecifier();
                //str = str.Substring(0,str.Length - 1);
                if (str[str.Length - 1] == '>') {
                    var nesting = 1;
                    for (int t = str.Length - 2; t >= 0; t--) {
                        switch (str[t]) {
                            case '>':
                                nesting++;
                                break;
                            case '<':
                                nesting--;
                                if (nesting == 0) {
                                    str = str.Substring(0, t);
                                    goto end;
                                }
                                break;
                        }
                    }
                end: { }
                }
                return str;                
            }
        }
        public bool UseInAliasName = true;

		public bool UseClassAlias = true;

        internal override string CalculateClassAliasIdentifier(int chars) {
            if (UseInAliasName) {
                var str = base.CalculateClassAliasIdentifier(Math.Max(chars/3, 1));
                var stem = ClassStemIdentifier;
                if (stem.Length > chars) {
                    stem = stem.Substring(0, chars);
                }
                stem = stem.Replace('.', '_');
                return str + stem;
            }
            return base.CalculateClassAliasIdentifier(chars);
        }

        public override bool MarkAsCodegen {
            get {
                if (Parent != null) {
                    if (Parent.MarkAsCodegen) {
                        return false;
                    }
                }
                return !IsPartial;
            }
        }
    }
}
