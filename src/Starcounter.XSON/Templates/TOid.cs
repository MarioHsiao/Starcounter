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
    public class TOid : PrimitiveProperty<UInt64> {
        public override Type MetadataType {
            get { throw new NotImplementedException(); } // TODO!
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
