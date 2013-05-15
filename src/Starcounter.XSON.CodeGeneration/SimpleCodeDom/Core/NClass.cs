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

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// 
    /// </summary>
    public abstract class NClass : NBase {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NClass(DomGenerator gen)
            : base(gen) {
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
        public NClass Generic { get; set; }

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
                    str += "<" + Generic.FullClassName + ">";
                }
                if (Parent == null || !(Parent is NClass)) {
                    return str;
                }
                else {
                    return (Parent as NClass).FullClassName + "." + str;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        /// <exception cref="System.Exception"></exception>
        public override string ToString() {
            if (ClassName != null) {
                var str = "NCLASS " + ClassName;
                if (Inherits != null) {
                    str += ":" + Inherits;
                }
                return str;
            }
            throw new Exception();
        }




    }
}
