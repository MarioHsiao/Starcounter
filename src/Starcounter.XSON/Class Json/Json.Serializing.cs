// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON;
using Module = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Json {

		/// <summary>
		/// Serializes JSON object to a string.
		/// </summary>
		/// <returns></returns>
		public string ToJson(JsonSerializerSettings settings = null) {
            byte[] buffer;
            int exactSize;

            if (Template == null) {
                // TODO:
                // We probably should return null here instead of empty object since it can be anything.
                return "{}";
            }
            
            var serializer = ((TValue)Template).JsonSerializer;
            int estimatedSize = serializer.EstimateSizeBytes(this, settings);
            buffer = new byte[estimatedSize];
                
            unsafe
            {
                fixed (byte* pdest = buffer)
                {
                    exactSize = serializer.Serialize(this, (IntPtr)pdest, buffer.Length, settings);
                }
            }
            
            return Encoding.UTF8.GetString(buffer, 0, exactSize);
        }

        /// <summary>
        /// Serializes JSON object to a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToJsonUtf8(JsonSerializerSettings settings = null) {
            byte[] buffer;

            if (Template == null) {
                return new byte[] { (byte)'{', (byte)'}' };
            }

            var serializer = ((TValue)Template).JsonSerializer;
            int estimatedSize = serializer.EstimateSizeBytes(this, settings);
            buffer = new byte[estimatedSize];
            int exactSize;

            unsafe
            {
                fixed (byte* pdest = buffer)
                {
                    exactSize = serializer.Serialize(this, (IntPtr)pdest, buffer.Length, settings);
                }
            }

            if (exactSize != estimatedSize) {
                byte[] tmp = new byte[exactSize];
                Buffer.BlockCopy(buffer, 0, tmp, 0, exactSize);
                buffer = tmp;
            }
            return buffer;
        }

        /// <summary>
        /// Serializes this object and sets the out parameter to the buffer containing 
        /// the UTF8 encoded characters. Returns the size used in the buffer.
        /// </summary>
        /// <remarks>
        /// The actual returned buffer might be larger than the amount used.
        /// </remarks>
        /// <param name="buf"></param>
        /// <returns></returns>
        public int ToJsonUtf8(byte[] buf, int offset, JsonSerializerSettings settings = null) {
            unsafe {
                fixed (byte* pdest = &buf[offset]) {
                    return ToJsonUtf8((IntPtr)pdest, buf.Length - offset, settings);
                }
            }
        }

        public int ToJsonUtf8(IntPtr dest, int destSize, JsonSerializerSettings settings = null) {
            if (Template == null) {
                // TODO:
                // We probably should return null here instead of empty object since it can be anything.
                unsafe {
                    byte* pdest = (byte*)dest;
                    *pdest++ = (byte)'{';
                    *pdest = (byte)'}';
                }
                return 2;
            }
            return ((TValue)Template).JsonSerializer.Serialize(this, dest, destSize, settings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        public void PopulateFromJson(string json, JsonSerializerSettings settings = null) {
            if (Template == null) 
                CreateDynamicTemplate(null);
            
            if (string.IsNullOrEmpty(json))
                return;

            byte[] source = Encoding.UTF8.GetBytes(json);
            PopulateFromJson(source, source.Length, settings);
        }		

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(byte[] source, int sourceSize, JsonSerializerSettings settings = null) {
            if (Template == null) 
                CreateDynamicTemplate(null);
           
            unsafe {
                fixed (byte* psrc = source) {
                    return PopulateFromJson((IntPtr)psrc, sourceSize, settings);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="jsonSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(IntPtr source, int sourceSize, JsonSerializerSettings settings = null) {
            if (Template == null) {
                CreateDynamicTemplate(null);
            }
            if (sourceSize == 0) return 0;

            var serializer = ((TValue)Template).JsonSerializer;
			return serializer.Populate(this, source, sourceSize, settings);
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal TypedJsonSerializer JsonSerializer {
            get {
                TValue tv = Template as TValue;
                if (tv != null)
                    return tv.JsonSerializer;
                return Module.GetJsonSerializer(Module.StandardJsonSerializerId);
            }
        }
    }
}
