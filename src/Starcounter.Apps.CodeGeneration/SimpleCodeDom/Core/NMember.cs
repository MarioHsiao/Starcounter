// ***********************************************************************
// <copyright file="NMember.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Represents a property, a field or a function
    /// </summary>
    public class NProperty : NBase {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NProperty(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public NClass Type { get; set; }
        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        /// <value>The name of the member.</value>
        public string MemberName {
            get {
                return Template.PropertyName;
            }
        }

        /// <summary>
        /// Gets the function generic.
        /// </summary>
        /// <value>The function generic.</value>
        public NClass FunctionGeneric {
            get {
                if (Type is NArrXXXClass) {
                    return (Type as NArrXXXClass).NApp;
                }
                else if (Type is NAppClass) {
                    return Type;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public Template Template { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating if this property
        /// is bound to an underlying Entity.
        /// </summary>
        public bool Bound { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
            string str = MemberName;
//            if (FunctionGeneric != null) {
//                str += "<" + FunctionGeneric.FullClassName + ">";
//            }
            return "NMEMBER " + Type.FullClassName + " " + str;
        }
    }
}
