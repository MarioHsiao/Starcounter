// ***********************************************************************
// <copyright file="NValueClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// 
    /// </summary>
    public abstract class NValueClass : NClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NValueClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the N template class.
        /// </summary>
        /// <value>The N template class.</value>
        public NTemplateClass NTemplateClass { get; set; }



    }
}
