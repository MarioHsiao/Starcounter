// ***********************************************************************
// <copyright file="NMetadataClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Class NMetadataClass
    /// </summary>
    public class NMetadataClass : NClass {

        public NMetadataClass(DomGenerator gen)
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

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                var type = NTemplateClass.NValueClass.ClassName;
                //if (type.Equals("long"))
                //    type = "Int";
                //else
                    type = UpperFirst(type);
                return type + "Metadata";
            }
        }

        /// <summary>
        /// Uppers the first.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        public static string UpperFirst( string str ) {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        /// <summary>
        /// The N template class
        /// </summary>
        public NTemplateClass NTemplateClass;
    }
}
