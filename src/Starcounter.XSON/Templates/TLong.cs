﻿// ***********************************************************************
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
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(long); }
        }
    }
}
