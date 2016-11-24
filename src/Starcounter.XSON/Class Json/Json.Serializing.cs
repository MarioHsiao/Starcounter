// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON;
using Starcounter.XSON.Interfaces;
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
            return JsonSerializer.Serialize(this, settings);
        }

        /// <summary>
        /// Serializes JSON object to a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToJsonUtf8(JsonSerializerSettings settings = null) {
            MemoryStream stream = new MemoryStream();
            JsonSerializer.Serialize(this, stream, settings);
            return stream.ToArray();
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
            MemoryStream stream = new MemoryStream(buf, offset, buf.Length - offset);
            JsonSerializer.Serialize(this, stream, settings);
            return (int)stream.Position - offset;
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

            JsonSerializer.Deserialize(this, json, settings);
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

            var stream = new MemoryStream(source, 0, sourceSize);
            JsonSerializer.Deserialize(this, stream, settings);
            return (int)stream.Position;
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal ITypedJsonSerializer JsonSerializer {
            get {
                TValue tv = Template as TValue;
                if (tv != null)
                    return tv.JsonSerializer;
                return Module.GetJsonSerializer(Module.StandardJsonSerializerId);
            }
        }
    }
}
