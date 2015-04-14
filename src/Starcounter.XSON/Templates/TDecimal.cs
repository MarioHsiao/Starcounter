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
        public override Type InstanceType {
            get { return typeof(decimal); }
        }

        public override string ToJson(Json json) {
            return Getter(json).ToString("0.0###########################", CultureInfo.InvariantCulture);
        }

        public override byte[] ToJsonUtf8(Json json) {
            byte[] buf = new byte[32];

            unsafe {
                fixed (byte* p = buf) {
                    JsonHelper.WriteDecimal((IntPtr)p, buf.Length, Getter(json));
                }
            }
            return buf;
        }

        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            if ((offset + 32) > buffer.Length)
                return -1;

            unsafe {
                fixed (byte* p = buffer) {
                    return JsonHelper.WriteDecimal((IntPtr)p, buffer.Length, Getter(json));
                }
            }
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            if (bufferSize < 32)
                return -1;

            return JsonHelper.WriteDecimal(ptr, bufferSize, Getter(json));
        }
    }
}
