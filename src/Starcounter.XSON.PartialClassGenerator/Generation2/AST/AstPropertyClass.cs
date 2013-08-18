// ***********************************************************************
// <copyright file="NPropertyClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// The source code representation of the TApp class.
    /// </summary>
    public class AstPropertyClass : AstTemplateClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstPropertyClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string Inherits {
            get { throw new System.NotImplementedException(); }
        }

       // public Type Type { get; set; }

       // public override string ClassName {
       //     get { return Type.Name; }
       // }
    }
}
