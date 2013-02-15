﻿// ***********************************************************************
// <copyright file="NObjMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Each App can have a metadata class. See ObjMetadata.
    /// </summary>
    public class NObjMetadata : NMetadataClass {

        public NObjMetadata(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// The template
        /// </summary>
        public TPuppet Template;

        /// <summary>
        /// The instances
        /// </summary>
        public static Dictionary<TPuppet, NClass> Instances = new Dictionary<TPuppet, NClass>();

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                return NTemplateClass.NValueClass.ClassName + "Metadata";
            }
        }

        /// <summary>
        /// The _ inherits
        /// </summary>
        public string _Inherits;

        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get { return _Inherits; }
        }

    }
}
