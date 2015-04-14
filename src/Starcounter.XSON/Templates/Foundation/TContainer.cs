// ***********************************************************************
// <copyright file="TContainer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.DeserializerCompiler;
using Module = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// Base class for Obj and Arr templates.
    /// </summary>
    /// <remarks>
    /// Both arrays and objects can have children. Arrays has elements and objects has properties.
    /// In addition, the templates (TContainer) for this complex objects are frozen/sealed whenever there are
    /// instance Obj or Arr objects pertaining to them. This means that new templates need to be created to
    /// use alternate schemas.
    /// </remarks>
    public abstract class TContainer : TValue {
		private bool codeGenStarted = false;
		private TypedJsonSerializer codegenStandardSerializer;
		private TypedJsonSerializer codegenFTJSerializer;

		/// <summary>
		/// 
		/// </summary>
        public override bool IsPrimitive {
            get { return false; }
        }

		internal abstract Json GetValue(Json parent);

        /// <summary>
        /// Represents the contained properties (TObj) or the single contained type for typed arrays (TArr).
        /// </summary>
        /// <value>The child property or child element type template</value>
        public abstract IEnumerable<Template> Children { get; }

		public abstract void PopulateFromJson(Json json, string jsonStr);
		public abstract int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize);
		public abstract int PopulateFromJson(Json json, byte[] src, int srcSize);

//		public abstract int ToFasterThanJson(Json json, byte[] buffer, int offset);
//		public abstract int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public override string ToJson(Json json) {
            byte[] buffer = new byte[JsonSerializer.EstimateSizeBytes(json)];
            int count = ToJsonUtf8(json, buffer, 0);
            return Encoding.UTF8.GetString(buffer, 0, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public override byte[] ToJsonUtf8(Json json) {
            byte[] buffer = new byte[JsonSerializer.EstimateSizeBytes(json)];
            int count = ToJsonUtf8(json, buffer, 0);

            // Checking if we have to shrink the buffer.
            if (count != buffer.Length) {
                byte[] sizedBuffer = new byte[count];
                Buffer.BlockCopy(buffer, 0, sizedBuffer, 0, count);
                return sizedBuffer;
            }
            return buffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            return JsonSerializer.Serialize(json, buffer, offset);
        }

        public override int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize) {
            throw new NotImplementedException();
        }

		/// <summary>
		/// 
		/// </summary>
		internal TypedJsonSerializer FTJSerializer {
			get {
				if (Module.UseCodegeneratedSerializer) {
					if (codegenFTJSerializer != null)
						return codegenFTJSerializer;

					if (!codeGenStarted) {
						codeGenStarted = true;
						if (!Module.DontCreateSerializerInBackground)
							ThreadPool.QueueUserWorkItem(GenerateSerializer, false);
						else {
							GenerateSerializer(false);
							return codegenFTJSerializer;
						}
					}
				}
				return Module.GetJsonSerializer(Module.FTJSerializerId);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		internal TypedJsonSerializer JsonSerializer {
			get {
				if (Module.UseCodegeneratedSerializer) {
					if (codegenStandardSerializer != null)
						return codegenStandardSerializer;

					// This check might give the wrong answer if the same instance of this template
					// is used from different threads. However the worst thing that can happen
					// is that the serializer is generated more than once in the background, but
					// the fallback serializer will be used instead so it's better than locking.
					if (!codeGenStarted) {
						codeGenStarted = true;
						if (!Module.DontCreateSerializerInBackground)
							ThreadPool.QueueUserWorkItem(GenerateSerializer, true);
						else {
							GenerateSerializer(true);
							return codegenStandardSerializer;
						}
					}
				}
				return Module.GetJsonSerializer(Module.StandardJsonSerializerId);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="state"></param>
		private void GenerateSerializer(object state) {
			bool createStd = (bool)state;

			// it doesn't really matter if setting the variable in the template is synchronized 
			// or not since if the serializer is null a fallback serializer will be used instead.
			if (createStd)
				codegenStandardSerializer = SerializerCompiler.The.CreateStandardJsonSerializer((TObject)this);
			else
				codegenFTJSerializer = SerializerCompiler.The.CreateFTJSerializer((TObject)this);
			codeGenStarted = false;
		}


        internal void UpdateParentAndIndex(Json parent, Json newValue) {
            if (newValue != null) {
                if (newValue.Parent != parent)
                    newValue.Parent = parent;
                newValue._cacheIndexInArr = TemplateIndex;
            }

            var oldValue = (Json)GetUnboundValueAsObject(parent);
            if (oldValue != null) {
                oldValue.SetParent(null);
                oldValue._cacheIndexInArr = -1;
            }
        }
    }
}
