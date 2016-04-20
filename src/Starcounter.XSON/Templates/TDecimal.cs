// ***********************************************************************
// <copyright file="TDecimal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Globalization;
using Starcounter.Advanced.XSON;

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
        internal override Type DefaultInstanceType {
            get { return typeof(decimal); }
        }

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Decimal; }
        }

        protected override bool ValueEquals(decimal value1, decimal value2) {
            return (value1 == value2);
        }
    }
}
