// ***********************************************************************
// <copyright file="TDecimal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Globalization;

namespace Starcounter.Templates {

    /// <summary>
    /// </summary>
    public class TDecimal : PrimitiveProperty<decimal> {
        public override Type MetadataType {
            get { return typeof(DecimalMetadata<Json>); }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(decimal); }
        }

		internal override string ValueToJsonString(Json parent) {
            return Getter(parent).ToString("0.0###########################", CultureInfo.InvariantCulture);
		}
    }
}
