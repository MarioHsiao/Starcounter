﻿// ***********************************************************************
// <copyright file="TDouble.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Globalization;
using Starcounter.Advanced.XSON;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TDouble : PrimitiveProperty<double> {
        public override Type MetadataType {
            get { return typeof(DoubleMetadata<Json>); }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        internal override Type DefaultInstanceType {
            get { return typeof(double); }
        }

        public override string ToJson(Json json) {
            return Getter(json).ToString("0.0###########################", CultureInfo.InvariantCulture);
        }

        public override byte[] ToJsonUtf8(Json json) {
            byte[] buf = new byte[32];

            unsafe {
                fixed (byte* p = buf) {
                    JsonHelper.WriteDouble((IntPtr)p, buf.Length, Getter(json));
                }
            }
            return buf;
        }

        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            if ((offset + 32) > buffer.Length)
                return -1;

            unsafe {
                fixed (byte* p = buffer) {
                    return JsonHelper.WriteDouble((IntPtr)p, buffer.Length, Getter(json));
                }
            }
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            if (bufferSize < 32)
                return -1;

            return JsonHelper.WriteDouble(ptr, bufferSize, Getter(json));
        }

        public override int EstimateUtf8SizeInBytes(Json json) {
            return 32;
        }
    }
}
