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

        public override string ToJson(Json json) {
            bool v = Getter(json);
            if (v) return "true";
            return "false";
        }

        public override byte[] ToJsonUtf8(Json json) {
            byte[] buf;
            bool value = Getter(json);

            buf = (value == true) ? new byte[4] : new byte[5];
            unsafe {
                fixed (byte* p = buf) {
                    JsonHelper.WriteBool((IntPtr)p, buf.Length, value);
                }
            }

            return buf;
        }

        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            bool value = Getter(json);
            int neededSize = 4;

            if (value == false)
                neededSize++;

            if ((neededSize + offset) > buffer.Length)
                return -1;

            unsafe {
                fixed (byte* p = &buffer[offset]) {
                    return JsonHelper.WriteBool((IntPtr)p, buffer.Length - offset, value);
                }
            }
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            bool value = Getter(json);
            int neededSize = 4;

            if (value == false)
                neededSize++;

            if (neededSize > bufferSize)
                return -1;

            unsafe {
                return JsonHelper.WriteBool((IntPtr)ptr, bufferSize, value);
            }
        }

        public override int EstimateUtf8SizeInBytes(Json json) {
            return 5;
        }
    }
}
