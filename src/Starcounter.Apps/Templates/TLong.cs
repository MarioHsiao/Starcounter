// ***********************************************************************
// <copyright file="TLong.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TLong : TValue<long>
    {

        private long _DefaultValue = 0;

        internal override void ProcessInput(Obj obj, byte[] rawValue)
        {
            long v = (long)Utf8Helper.IntFastParseFromAscii(rawValue, 0, (uint)rawValue.Length);
            obj.ProcessInput<long>(this, v);
        }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public long DefaultValue {
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
                DefaultValue = (long)value;
            }
        }
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(long); }
        }
    }
}
