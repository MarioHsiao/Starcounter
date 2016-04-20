// ***********************************************************************
// <copyright file="TLong.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TLong : PrimitiveProperty<long> {
        public override Type MetadataType {
            get { return typeof(LongMetadata<Json>); }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        internal override Type DefaultInstanceType {
            get { return typeof(long); }
        }

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Long; }
        }

        protected override bool ValueEquals(long value1, long value2) {
            return (value1 == value2);
        }
    }
}
