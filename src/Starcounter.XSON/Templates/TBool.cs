// ***********************************************************************
// <copyright file="TBool.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates {

    /// <summary>
    /// Defines a boolean property in an App object.
    /// </summary>
    public class TBool : PrimitiveProperty<bool> {
        public override Type MetadataType {
            get { return typeof(BoolMetadata<Json>); }
        }

        /// <summary>
        /// Will return the Boolean runtime type
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(bool); }
        }

		internal override string ValueToJsonString(Json parent) {
			bool v = Getter(parent);
			if (v) return "true";
			return "false";
		}
    }
}
