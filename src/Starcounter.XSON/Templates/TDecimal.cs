﻿// ***********************************************************************
// <copyright file="TDecimal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Templates {

    /// <summary>
    /// </summary>
    public class TDecimal : PrimitiveProperty<decimal> {
        public override Type MetadataType {
            get { return typeof(DecimalMetadata<Json>); }
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal DefaultValue { get; set; }

        internal override void SetDefaultValue(Json parent) {
            UnboundSetter(parent, DefaultValue);
        }

        ///// <summary>
        ///// Contains the default value for the property represented by this
        ///// Template for each new App object.
        ///// </summary>
        ///// <value>The default value as object.</value>
        //public override object DefaultValueAsObject {
        //    get {
        //        return DefaultValue;
        //    }
        //    set {
        //        DefaultValue = (decimal)value;
        //    }
        //}
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(decimal); }
        }

		internal override string ValueToJsonString(Json parent) {
			return Getter(parent).ToString("0.0###########################");
		}
    }
}
