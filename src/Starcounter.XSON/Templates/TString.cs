// ***********************************************************************
// <copyright file="TString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced.XSON;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TString : PrimitiveProperty<string> {
        public TString() {
            DefaultValue = "";
        }

        public override Type MetadataType {
            get { return typeof(StringMetadata<Json>); }
        }
        
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        internal override Type DefaultInstanceType {
            get { return typeof(string); }
        }

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.String; }
        }

        protected override bool ValueEquals(string value1, string value2) {
            return string.Equals(value1, value2);
        }
    }
}
