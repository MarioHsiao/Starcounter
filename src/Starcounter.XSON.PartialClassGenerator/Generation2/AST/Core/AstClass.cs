// ***********************************************************************
// <copyright file="NClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// 
    /// </summary>
    public abstract class AstClass : AstBase {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        public override string Name {
            get { return ClassName; }
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public abstract string ClassName { get; }
        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        public abstract string Inherits { get; }
        /// <summary>
        /// Gets or sets the generic.
        /// </summary>
        /// <value>The generic.</value>
        public AstClass Generic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is partial.
        /// </summary>
        /// <value><c>true</c> if this instance is partial; otherwise, <c>false</c>.</value>
        public bool IsPartial { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is static.
        /// </summary>
        /// <value><c>true</c> if this instance is static; otherwise, <c>false</c>.</value>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets the full name of the class.
        /// </summary>
        /// <value>The full name of the class.</value>
        public virtual string FullClassName {
            get {
                var str = ClassName;
                if (Generic != null) {
                    str += "<" + Generic.GlobalClassSpecifier + ">";
                }
                if (Generics != null) {
                    if (Generics != null)
                        str += "<" + Generics + ">";
                }
                if (Parent == null || !(Parent is AstClass)) {
                    return str;
                }
                else {
                    return (Parent as AstClass).FullClassName + "." + str;
                }
            }
        }

        public string NamespaceAlias = "global::";

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        /// <exception cref="System.Exception"></exception>
        public override string ToString() {
            if (ClassName != null) {
                var str = "CLASS " + ClassName;
                if (Generics != null)
                    str += "<" + Generics + ">";
                //if (Inherits != null) {
                    str += ":" + Inherits;
                //}
                return str;
            }
            throw new Exception();
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
        public string Generics;

        /// <summary>
        /// class.subclass
        /// </summary>
        public string ClassSpecifierWithoutNamespace {
            get {
                var str = FullClassName;
                return str;
            }
        }


        /// <summary>
        /// class.subclass
        /// </summary>
        public string ClassDeclaration {
            get {
                var str = ClassName;
                if (Generics != null)
                    str += "<" + Generics + ">";
                return str;
            }
        }

        private string _Namespace;

        public virtual string Namespace {
            set {
                _Namespace = value;
            }
            get {
                return _Namespace;
            }
        }

        /// <summary>
        /// global::mynamespace.subnamespace.class.subclass
        /// </summary>
        public virtual string GlobalClassSpecifier {
            get {
                var str = NamespaceAlias;
                if (Namespace != null)
                    str += Namespace + ".";
                return str + ClassSpecifierWithoutNamespace;
            }
        }
    }
}
