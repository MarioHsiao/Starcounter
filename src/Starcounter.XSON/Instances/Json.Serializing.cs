// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal;
using Starcounter.Templates;
using System.Text;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Json {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] ToJsonUtf8() {
            // TODO! Move all this code to polymorphic template implementations
            if (Template == null) {
                return new byte[0];
            }
            if (!(Template is TContainer)) {
                throw new NotImplementedException("Cannot currently serialize JSON for single value JSON");
            }
            var template = Template as TContainer;
            return template.Serializer.ToJsonUtf8(this);
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
        public override int ToJsonUtf8(out byte[] buffer) {
            // TODO! Move all this code to polymorphic template implementations
            if (Template == null) {
             //   throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
//                CreateDynamicTemplate();
//                var ret = this.ToJsonUtf8(out buffer);
//                Template = null;
//                return ret;
                buffer = new byte[0];
                return 0;               
            }
            if (!(Template is TContainer)) {
                throw new NotImplementedException("Cannot currently serialize JSON for single value JSON");
            }
            var template = Template as TContainer;
            return template.Serializer.ToJsonUtf8(this, out buffer);
        }

        internal void CreateDynamicTemplate() {
            var t = new Schema();
            t.IsDynamic = true;
            Template = t; // IMPORTANT! It is important that the dynamic flag is set _before_ it is assigned to the Template property.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToJson() {
            if (Template == null) {
                return "";
            }
            // TODO! Move all this code to polymorphic template implementations
            if (!(Template is TContainer)) {
                throw new NotImplementedException("Cannot currently serialize JSON for single value JSON");
            }
            return ((TContainer)Template).Serializer.ToJson(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="jsonSize"></param>
        /// <returns></returns>
        public int PopulateFromJson(IntPtr buffer, int jsonSize) {
            if (Template == null) {
             //   throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
                CreateDynamicTemplate();
            }
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
            if (Template == null) {
             //   throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
                CreateDynamicTemplate();
            }
            if (bufferSize == 0) return 0;
            return Template.Serializer.PopulateFromJson(this, buffer, bufferSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        public void PopulateFromJson(string json) {
            if (Template == null) {
             //   throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
                CreateDynamicTemplate();
            }
            if (string.IsNullOrEmpty(json)) return;
            Template.Serializer.PopulateFromJson(this, json);
        }
    }
}
