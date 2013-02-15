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
        /// The classes
        /// </summary>
        public static Dictionary<Template, NMetadataClass> Classes = new Dictionary<Template, NMetadataClass>();

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NMetadataClass.</returns>
        public static NMetadataClass Find( Template template ) {
            template = NTemplateClass.GetPrototype(template);
            return NMetadataClass.Classes[template];
        }

        /// <summary>
        /// Initializes static members of the <see cref="NMetadataClass" /> class.
        /// </summary>
        static NMetadataClass() {
            Classes[NTemplateClass.TString] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TString] };
            Classes[NTemplateClass.TLong] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TLong] };
            Classes[NTemplateClass.TDecimal] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TDecimal] };
            Classes[NTemplateClass.TDouble] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TDouble] };
            Classes[NTemplateClass.TBool] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TBool] };
            Classes[NTemplateClass.ActionProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.ActionProperty] };
            Classes[NTemplateClass.TApp] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.TApp] };
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
