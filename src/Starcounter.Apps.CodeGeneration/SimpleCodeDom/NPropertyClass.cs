// ***********************************************************************
// <copyright file="NPropertyClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of the AppTemplate class.
    /// </summary>
    public class NPropertyClass : NTemplateClass {
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
