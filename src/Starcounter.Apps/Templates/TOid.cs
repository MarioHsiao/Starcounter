// ***********************************************************************
// <copyright file="TOid.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TOid : TValue<UInt64> {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public UInt64 DefaultValue { get; set; }

        internal override void ProcessInput(Obj obj, byte[] rawValue) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        public override object DefaultValueAsObject {
            get { return DefaultValue; }
            set { DefaultValue = (UInt64)value; }
        }
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(UInt64); }
        }
    }
}
