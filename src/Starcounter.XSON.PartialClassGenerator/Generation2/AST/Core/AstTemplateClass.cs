// ***********************************************************************
// <copyright file="NTemplateClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// 
    /// </summary>
    public abstract class AstTemplateClass : AstClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstTemplateClass(DomGenerator gen)
            : base(gen) {
        }

        public override string Name {
            get { return Template.PropertyName; }
        }

        /// <summary>
        /// The template
        /// </summary>
        public Template Template;

        /// <summary>
        /// The _ N value class
        /// </summary>
        private AstValueClass _NValueClass;

        /// <summary>
        /// Gets or sets the N value class.
        /// </summary>
        /// <value>The N value class.</value>
        public AstValueClass NValueClass {
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
        public AstProperty NValueProperty;

        /// <summary>
        /// Gets or sets the N metadata class.
        /// </summary>
        /// <value>The N metadata class.</value>
        public AstMetadataClass NMetadataClass { get; set; }
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
