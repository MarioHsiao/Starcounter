// ***********************************************************************
// <copyright file="NTApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of the TApp class.
    /// </summary>
    public class NTAppClass : NTemplateClass {
       // public NAppClass AppClassNode;

//        public static Dictionary<TApp, NClass> Instances = new Dictionary<TApp, NClass>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NTAppClass" /> class.
        /// </summary>
        public NTAppClass( DomGenerator gen ) : base( gen )
        {
            Constructor = new NConstructor( gen ) { Parent = this };
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                if (NValueClass == null)
                    return "Unknown";
                return "T" + NValueClass.ClassName; // +"Template";
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

        /// <summary>
        /// If set to true all properties in this appclass will be automatically 
        /// bound, if not specified otherwise on the property, to the underlying dataobject in the app.
        /// </summary>
        public bool AutoBindProperties { get; set; }
    }
}
