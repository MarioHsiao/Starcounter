
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.DeserializerCompiler;
using System.Threading;
namespace Starcounter.Templates {

    partial class Template {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TypedJsonSerializer Serializer {
            get {
                if (UseCodegeneratedSerializer) {
                    if (codegenSerializer != null)
                        return codegenSerializer;

                    // This check might give the wrong answer if the same instance of this template
                    // is used from different threads. However the worst thing that can happen
                    // is that the serializer is generated more than once in the background, but
                    // the fallback serializer will be used instead so it's better than locking.
                    if (!codeGenStarted) {
                        codeGenStarted = true;
                        if (!DontCreateSerializerInBackground)
                            ThreadPool.QueueUserWorkItem(GenerateSerializer);
                        else {
                            GenerateSerializer(null);
                            return codegenSerializer;
                        }
                    }
                }
                return FallbackSerializer;
            }
        }

        internal static TypedJsonSerializer FallbackSerializer = DefaultSerializer.Instance;


        private static bool shouldUseCodegeneratedSerializer = false;

        private TypedJsonSerializer codegenSerializer;
        private bool codeGenStarted = false;
        internal void GenerateSerializer(object state) {
            // it doesn't really matter if setting the variable in the template is synchronized 
            // or not since if the serializer is null a fallback serializer will be used instead.
            this.codegenSerializer = SerializerCompiler.The.CreateTypedJsonSerializer(this);   //Obj.Factory.CreateJsonSerializer(this);
        }


        /// <summary>
        /// 
        /// </summary>
        public static bool UseCodegeneratedSerializer {
            get {
                //if (Obj.Factory == null)
                //    return false;
                return shouldUseCodegeneratedSerializer;
            }
            set {
                shouldUseCodegeneratedSerializer = value;
            }
        }

        /// <summary>
        /// If set to true the codegeneration for the serializer will not be done in a background
        /// and execution will wait until the generated serializer is ready to be used. This is 
        /// used by for example unittests, where you want to test the genererated code specifically.
        /// </summary>
        internal static bool DontCreateSerializerInBackground { get; set; }

    }
}
