// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Obj {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToJsonUtf8() {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            return Template.Serializer.ToJsonUtf8(this);
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
        public int ToJsonUtf8(out byte[] buffer) {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            return Template.Serializer.ToJsonUtf8(this, out buffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToJson() {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            return Template.Serializer.ToJson(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="jsonSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(IntPtr buffer, int jsonSize) {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            if (jsonSize == 0) return 0;
            return Template.Serializer.PopulateFromJson(this, buffer, jsonSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(byte[] buffer, int bufferSize) {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            if (bufferSize == 0) return 0;
            return Template.Serializer.PopulateFromJson(this, buffer, bufferSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        public void PopulateFromJson(string json) {
            if (Template == null)
                throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
            if (string.IsNullOrEmpty(json)) return;
            Template.Serializer.PopulateFromJson(this, json);
        }
    }
}
