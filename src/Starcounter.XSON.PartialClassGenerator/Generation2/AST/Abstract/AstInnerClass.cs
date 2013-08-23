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
    /// There are two types of Json inner classes that are used
    /// by the code generator. One is the base or derived
    /// template classes (AstTemplateClass) and one is the base
    /// or dervied meta data bases (AstMetadataClass)
    /// </summary>
    public abstract class AstInnerClass : AstClass {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="generator">The dom generator instance</param>
        public AstInnerClass(Gen2DomGenerator generator)
            : base(generator) {
        }

        private AstInstanceClass _NValueClass;

        /// <summary>
        /// The corresponding Json instance type of this 
        /// metadata class. I.e. if this is a JsonMetadata class,
        /// the InstanceClass is a Json class.
        /// </summary>
        public AstInstanceClass NValueClass {
            get {
                return _NValueClass;
            }
            set { _NValueClass = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public AstProperty NValueProperty;


//        public override string Namespace {
//            get {
//                return NValueClass.Namespace;
//            }
//        }

        public bool IsCodegenerated = false;

//        /// <summary>
//        /// Gets the name of the class.
//        /// </summary>
//        /// <value>The name of the class.</value>

    }
}
