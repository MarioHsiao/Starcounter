// ***********************************************************************
// <copyright file="TContainer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.DeserializerCompiler;
using Module = Modules.Starcounter_XSON_JsonByExample;

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
		private static bool shouldUseCodegeneratedSerializer = false;
		private bool codeGenStarted = false;
		private bool _Sealed;
		private TypedJsonSerializer codegenStandardSerializer;
		private TypedJsonSerializer codegenFTJSerializer;

		/// <summary>
		/// 
		/// </summary>
        public override bool IsPrimitive {
            get { return false; }
        }

		/// <summary>
		/// Once a TContainer (Obj or Arr schema) is in use (have instances), this property will return
		/// true and you cannot modify the template.
		/// </summary>
		/// <remarks>
		/// Exception should be change to an SCERR???? error.
		/// </remarks>
		/// <value><c>true</c> if sealed; otherwise, <c>false</c>.</value>
		/// <exception cref="System.Exception">Once a TObj is sealed, you cannot unseal it</exception>
		public override bool Sealed {
			get {
				return _Sealed;
			}
			internal set {
				if (!value && _Sealed) {
					// TODO! SCERR!
					throw new Exception("Once a TContainer (Obj or Arr schema) is in use (have instances), you cannot modify it");
				}
				_Sealed = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public static bool UseCodegeneratedSerializer {
			get { return shouldUseCodegeneratedSerializer; }
			set { shouldUseCodegeneratedSerializer = value; }
		}

		internal abstract Json GetValue(Json parent);

		/// <summary>
		/// If set to true the codegeneration for the serializer will not be done in a background
		/// and execution will wait until the generated serializer is ready to be used. This is 
		/// used by for example unittests, where you want to test the genererated code specifically.
		/// </summary>
		internal static bool DontCreateSerializerInBackground { get; set; }

        /// <summary>
        /// Represents the contained properties (TObj) or the single contained type for typed arrays (TArr).
        /// </summary>
        /// <value>The child property or child element type template</value>
        public abstract IEnumerable<Template> Children { get; }

		public abstract string ToJson(Json json);
		public abstract byte[] ToJsonUtf8(Json json);

        public abstract int ToJsonUtf8(Json json, byte[] buffer, int offset);

		public abstract void PopulateFromJson(Json json, string jsonStr);
		public abstract int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize);
		public abstract int PopulateFromJson(Json json, byte[] src, int srcSize);

		public abstract int ToFasterThanJson(Json json, byte[] buffer, int offset);
		public abstract int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize);

		/// <summary>
		/// 
		/// </summary>
		internal TypedJsonSerializer FTJSerializer {
			get {
				if (UseCodegeneratedSerializer) {
					if (codegenFTJSerializer != null)
						return codegenFTJSerializer;

					if (!codeGenStarted) {
						codeGenStarted = true;
						if (!DontCreateSerializerInBackground)
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
				if (UseCodegeneratedSerializer) {
					if (codegenStandardSerializer != null)
						return codegenStandardSerializer;

					// This check might give the wrong answer if the same instance of this template
					// is used from different threads. However the worst thing that can happen
					// is that the serializer is generated more than once in the background, but
					// the fallback serializer will be used instead so it's better than locking.
					if (!codeGenStarted) {
						codeGenStarted = true;
						if (!DontCreateSerializerInBackground)
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

        internal void UpdateParentAndIndex(Json newValue, Json parent) {
            if (newValue != null) {
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
