// ***********************************************************************
// <copyright file="TDecimal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Templates {

    /// <summary>
    /// </summary>
    public class TDecimal : PrimitiveProperty<decimal>
    {
        /// <summary>
        /// </summary>
        decimal _DefaultValue = 0;

        public override Type MetadataType {
            get { return typeof(DecimalMetadata<Json>); }
        }


        public override void ProcessInput(Json obj, byte[] rawValue)
        {
            // TODO:
            // Superslow way of parsing the decimal value. Needs to be rewritten.
            decimal value;
            decimal.TryParse(Encoding.UTF8.GetString(rawValue), out value);
            obj.ProcessInput<decimal>(this, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (decimal)value;
            }
        }
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
