// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal;
using Starcounter.Templates;
using System.Text;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Json {

		/// <summary>
		/// Serializes JSON object to a string.
		/// </summary>
		/// <returns></returns>
		public string ToJson() {
			if (Template == null) {
				return "{}";
			}

            return ((TValue)Template).ToJson(this);
		}

        /// <summary>
        /// Serializes JSON object to a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToJsonUtf8() {
			if (Template == null) {
				var buffer = new byte[2];
				buffer[0] = (byte)'{';
				buffer[1] = (byte)'}';
                return buffer;
			}

            return ((TValue)Template).ToJsonUtf8(this);
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
        public int ToJsonUtf8(byte[] buf, int offset) {

            if (Template == null) {
                buf[offset] = (byte)'{';
                buf[offset + 1] = (byte)'}';
                return 2;
            }

            return ((TValue)Template).ToJsonUtf8(this, buf, offset);
        }

		/// <summary>
		/// 
		/// </summary>
        internal void CreateDynamicTemplate() {
            var t = new TObject();
            t.IsDynamic = true;
            Template = t; // IMPORTANT! It is important that the dynamic flag is set _before_ it is assigned to the Template property.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="jsonSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(IntPtr buffer, int jsonSize) {
            if (Template == null) {
                CreateDynamicTemplate();
            }
            if (jsonSize == 0) return 0;
			var template = Template as TContainer;
			return template.PopulateFromJson(this, buffer, jsonSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(byte[] buffer, int bufferSize) {
			if (Template == null) {
				CreateDynamicTemplate();
			}
			var template = Template as TContainer;
			return template.PopulateFromJson(this, buffer, bufferSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        public void PopulateFromJson(string json) {
			if (Template == null) {
				CreateDynamicTemplate();
			}
			if (string.IsNullOrEmpty(json))
				return;
			var template = Template as TContainer;
			template.PopulateFromJson(this, json);
        }		
    }
}
