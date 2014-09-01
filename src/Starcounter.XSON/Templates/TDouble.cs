// ***********************************************************************
// <copyright file="TDouble.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Globalization;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TDouble : PrimitiveProperty<double> {
        public override Type MetadataType {
            get { return typeof(DoubleMetadata<Json>); }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(double); }
        }

		internal override string ValueToJsonString(Json parent) {
			return Getter(parent).ToString("0.0###########################", CultureInfo.InvariantCulture);
		}
    }
}
