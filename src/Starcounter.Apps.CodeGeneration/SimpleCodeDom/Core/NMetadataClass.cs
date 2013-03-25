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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
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
                string type;

                // TODO: 
                // If we have a typed Json or Puppet class either we need to use the ObjMetadata 
                // class or have a separate JsonMetadata and PuppetMetadata class.
                if (NTemplateClass.Template is TObj) {
                    type = "Obj";
                } else {
                    type = NTemplateClass.NValueClass.ClassName;
                    type = UpperFirst(type);
                }

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
