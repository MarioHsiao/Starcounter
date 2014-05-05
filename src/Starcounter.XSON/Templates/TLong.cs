// ***********************************************************************
// <copyright file="TLong.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
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
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public long DefaultValue { get; set; }

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
        //        DefaultValue = (long)value;
        //    }
        //}
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(long); }
        }
    }
}
