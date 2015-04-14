// ***********************************************************************
// <copyright file="TLong.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced.XSON;
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

        public override string ToJson(Json json) {
            return Getter(json).ToString();
        }

        public override byte[] ToJsonUtf8(Json json) {
            long value = Getter(json);
            byte[] buf = new byte[GetSizeOfIntAsUtf8(value)];
            Utf8Helper.WriteIntAsUtf8Man(buf, 0, value);
            return buf;
        }

        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            long value = Getter(json);
            int neededSize = GetSizeOfIntAsUtf8(value);

            if ((neededSize + offset) > buffer.Length)
                return -1;

            return (int)Utf8Helper.WriteIntAsUtf8Man(buffer, (uint)offset, value);
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            long value = Getter(json);
            int neededSize = GetSizeOfIntAsUtf8(value);

            if (neededSize > bufferSize)
                return -1;

            unsafe {
                return (int)Utf8Helper.WriteIntAsUtf8((byte*)ptr, value);
            }
        }

        private static int GetSizeOfIntAsUtf8(long value) {
            int size = 0;
            do {
                value = value / 10;
                size++;
            } while (value > 0);

            return size;
        }
    }
}
