// ***********************************************************************
// <copyright file="NValueClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// 
    /// </summary>
    public abstract class AstValueClass : AstClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstValueClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the N template class.
        /// </summary>
        /// <value>The N template class.</value>
        public AstTemplateClass NTemplateClass { get; set; }



    }
}
