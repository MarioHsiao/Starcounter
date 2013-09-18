using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.DeserializerCompiler;
using System.Threading;
using System;
using Module = Modules.Starcounter_XSON_JsonByExample;

namespace Starcounter.Templates {
    public abstract partial class TContainer {
		public abstract string ToJson(Json json);
		public abstract byte[] ToJsonUtf8(Json json);
		public abstract int ToJsonUtf8(Json json, out byte[] buffer);

		public abstract void PopulateFromJson(Json json, string jsonStr);
		public abstract int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize);
		public abstract int PopulateFromJson(Json json, byte[] src, int srcSize);
		
		public abstract int ToFasterThanJson(Json json, out byte[] buffer);
		public abstract int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize);

		private static bool shouldUseCodegeneratedSerializer = false;
		private bool codeGenStarted = false;
		private TypedJsonSerializer codegenStandardSerializer;
		private TypedJsonSerializer codegenFTJSerializer;

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
							return codegenStandardSerializer;
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

		private void GenerateSerializer(object state) {
			bool createStd = (bool)state;

			// it doesn't really matter if setting the variable in the template is synchronized 
			// or not since if the serializer is null a fallback serializer will be used instead.
			if (createStd)
				codegenStandardSerializer = SerializerCompiler.The.CreateStandardJsonSerializer((TObject)this);
			else
				codegenFTJSerializer = SerializerCompiler.The.CreateFTJSerializer(this);
			codeGenStarted = false;
		}

		/// <summary>
		/// 
		/// </summary>
		public static bool UseCodegeneratedSerializer {
			get { return shouldUseCodegeneratedSerializer; }
			set { shouldUseCodegeneratedSerializer = value; }
		}

		/// <summary>
		/// If set to true the codegeneration for the serializer will not be done in a background
		/// and execution will wait until the generated serializer is ready to be used. This is 
		/// used by for example unittests, where you want to test the genererated code specifically.
		/// </summary>
		internal static bool DontCreateSerializerInBackground { get; set; }
    }
}
