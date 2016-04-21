// ***********************************************************************
// <copyright file="TBool.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced.XSON;

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
        internal override Type DefaultInstanceType {
            get { return typeof(bool); }
        }

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Bool; }
        }

        protected override bool ValueEquals(bool value1, bool value2) {
            return value1 == value2;
        }
    }
}
