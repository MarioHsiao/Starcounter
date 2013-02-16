// ***********************************************************************
// <copyright file="NTemplateClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// 
    /// </summary>
    public abstract class NTemplateClass : NClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NTemplateClass(DomGenerator gen)
            : base(gen) {
        }


        /// <summary>
        /// The template
        /// </summary>
        public Template Template;

        /// <summary>
        /// The _ N value class
        /// </summary>
        private NValueClass _NValueClass;

        /// <summary>
        /// Gets or sets the N value class.
        /// </summary>
        /// <value>The N value class.</value>
        public NValueClass NValueClass {
            get {
                if (_NValueClass != null)
                    return _NValueClass;
                return Generator.ValueClasses[this.Template];
            }
            set { _NValueClass = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public NProperty NValueProperty;

        /// <summary>
        /// Gets or sets the N metadata class.
        /// </summary>
        /// <value>The N metadata class.</value>
        public NMetadataClass NMetadataClass { get; set; }
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                return Template.GetType().Name;
            }
        }
    }
}
