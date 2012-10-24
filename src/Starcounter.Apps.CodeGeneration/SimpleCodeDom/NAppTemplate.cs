// ***********************************************************************
// <copyright file="NAppTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of the AppTemplate class.
    /// </summary>
    public class NAppTemplateClass : NTemplateClass {
       // public NAppClass AppClassNode;

//        public static Dictionary<AppTemplate, NClass> Instances = new Dictionary<AppTemplate, NClass>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NAppTemplateClass" /> class.
        /// </summary>
        public NAppTemplateClass() : base()
        {
            Constructor = new NConstructor() { Parent = this };
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                if (NValueClass == null)
                    return "Unknown";
                return NValueClass.ClassName + "Template";
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

        /// <summary>
        /// The constructor
        /// </summary>
        public NConstructor Constructor;
    }
}
