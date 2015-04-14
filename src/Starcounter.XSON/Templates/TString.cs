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

        internal override void SetDefaultValue(Json parent) {
            UnboundSetter(parent, DefaultValue);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(string); }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="addToChangeLog"></param>
		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			if (UseBinding(parent)) {
				string boundValue = BoundGetter(parent);
				string oldValue = UnboundGetter(parent);

				if ((boundValue == null && oldValue != null) 
					|| (boundValue != null && !boundValue.Equals(oldValue))) {
					UnboundSetter(parent, boundValue);
					if (addToChangeLog)
						parent.Session.UpdateValue(parent, this);
				}
			}	
		}

        public override string ToJson(Json json) {
            string value = Getter(json);
            if (!string.IsNullOrEmpty(value)) {
                byte[] buffer = new byte[value.Length * 4];
                unsafe {
                    fixed (byte* p = buffer) {
                        int size = JsonHelper.WriteString((IntPtr)p, buffer.Length, value);
                        return System.Text.Encoding.UTF8.GetString(buffer, 0, size);
                    }
                }
            }
            return "\"\"";
        }

        public override byte[] ToJsonUtf8(Json json) {
            string value = Getter(json);
            byte[] buf;
            int realSize;

            if (value != null) {
                buf = new byte[value.Length * 4];
                unsafe {
                    fixed (byte* p = buf) {
                        realSize = JsonHelper.WriteString((IntPtr)p, buf.Length, value);
                    }
                }

                if (buf.Length != realSize) {
                    byte[] tmp = new byte[realSize];
                    Buffer.BlockCopy(buf, 0, tmp, 0, realSize);
                    buf = tmp;
                }
            } else {
                // TODO:
                // Support real null.
                buf = new byte[2];
                buf[0] = (byte)'"';
                buf[1] = (byte)'"';
            }
            return buf;
        }

        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            unsafe {
                fixed (byte* p = &buffer[offset]) {
                    return JsonHelper.WriteString((IntPtr)p, buffer.Length - offset, Getter(json));
                }
            }
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            return JsonHelper.WriteString(ptr, bufferSize, Getter(json));
        }
    }
}
