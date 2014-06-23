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

        public double DefaultValue { get; set; }

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
        //        DefaultValue = (double)value;
        //    }
        //}

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
