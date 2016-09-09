using System;
using System.Collections.Generic;
using Starcounter.Advanced.XSON;
using Starcounter.XSON.Interfaces;

namespace Starcounter.Internal.XSON.Modules {
    /// <summary>
    /// Represents this module
    /// </summary>
    public static class Starcounter_XSON {
        private static bool useCodeGeneratedSerializer = false;
        private static Dictionary<string, uint> jsonSerializerIndexes = new Dictionary<string, uint>();
        private static List<TypedJsonSerializer> jsonSerializers = new List<TypedJsonSerializer>();

        internal static uint StandardJsonSerializerId;
        internal static uint FTJSerializerId;

        /// <summary>
        /// 
        /// </summary>
        public static void Initialize() {
            StandardJsonSerializerId = RegisterJsonSerializer("json", new StandardJsonSerializer());
//            FTJSerializerId = RegisterJsonSerializer("ftj", new FasterThanJsonSerializer());
            JsonByExample.Initialize();
        }


        /// <summary>
        /// 
        /// </summary>
        public static bool UseCodegeneratedSerializer {
            get { return useCodeGeneratedSerializer; }
            set { useCodeGeneratedSerializer = value; }
        }

        /// <summary>
        /// If set to true the codegeneration for the serializer will not be done in a background
        /// and execution will wait until the generated serializer is ready to be used. This is 
        /// used by for example unittests, where you want to test the genererated code specifically.
        /// </summary>
        internal static bool DontCreateSerializerInBackground { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static uint GetJsonSerializerId(string name) {
            uint id;
            if (!jsonSerializerIndexes.TryGetValue(name, out id))
                throw new Exception("No typed json serializer with name '" + name + "' was found.");
            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializerId"></param>
        /// <returns></returns>
        public static TypedJsonSerializer GetJsonSerializer(uint serializerId) {
            if (serializerId >= jsonSerializers.Count)
                throw new Exception("Invalid serializerId.");
            return jsonSerializers[(int)serializerId];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static uint RegisterJsonSerializer(string name, TypedJsonSerializer serializer) {
            if (jsonSerializerIndexes.ContainsKey(name))
                throw new Exception("An serializer with the same name is already registered.");
            uint id = (uint)jsonSerializers.Count;
            jsonSerializers.Add(serializer);
            jsonSerializerIndexes.Add(name, id);
            return id;
        }

        public static void SetDefaultJsonSerializer(uint id) {
            StandardJsonSerializerId = id;
        }

        /// <summary>
        /// Contains all dependency injections into this module
        /// </summary>
        public static class Injections {
            /// <summary>
            /// In Starcounter, the user (i.e. programmer) can respond with an Obj on an Accept: text/html request.
            /// In this case, the HTML pertaining to the view of the view model described by the Obj should
            /// be retrieved. This cannot be done by the Obj itself as it does not know about the static web server
            /// or how to call any user handlers.
            /// </summary>
            public static IResponseConverter JsonMimeConverter = null;
        }

        /// <summary>
        /// Represents this module
        /// </summary>
        public static class JsonByExample {
            /// <summary>
            /// By default, Starcounter creates
            /// a JSON-by-example reader that allows you to convert a JSON file to a XOBJ template using the format
            /// string "json". You can inject other template formats here.
            /// </summary>
            internal static Dictionary<string, IXsonTemplateMarkupReader> MarkupReaders = new Dictionary<string, IXsonTemplateMarkupReader>();

            /// <summary>
            /// 
            /// </summary>
            internal static void Initialize() {
//                MarkupReaders.Add("json", new JsonByExampleTemplateReader());
            }

            ///// <summary>
            ///// Creates a json template based on the input json.
            ///// </summary>
            ///// <param name="script2">The json</param>
            ///// <param name="restrictToDesigntimeVariable">if set to <c>true</c> [restrict to designtime variable].</param>
            ///// <returns>an TObj instance</returns>
            //public static TypeTObj CreateFromJs<TypeObj, TypeTObj>(string script2, bool restrictToDesigntimeVariable)
            //    where TypeObj : Json, new()
            //    where TypeTObj : TObject, new() {
            //    return _CreateFromJs<TypeObj, TypeTObj>(script2, "unknown", restrictToDesigntimeVariable); //ignoreNonDesignTimeAssignments);
            //}

            ///// <summary>
            ///// 
            ///// </summary>
            ///// <typeparam name="TypeObj"></typeparam>
            ///// <typeparam name="TypeTObj"></typeparam>
            ///// <param name="source"></param>
            ///// <param name="sourceReference"></param>
            ///// <param name="ignoreNonDesignTimeAssigments"></param>
            ///// <returns></returns>
//            public static TypeTObj _CreateFromJs<TypeObj, TypeTObj>(string source,
//                                               string sourceReference,
//                                               bool ignoreNonDesignTimeAssigments)
//                where TypeObj : Json, new()
//                where TypeTObj : TObject, new() {

////                return JsonByExampleTemplateReader.CreateFromJs<TypeObj, TypeTObj>(source, sourceReference, ignoreNonDesignTimeAssigments);
//            }




            ///// <summary>
            ///// Reads the file and generates a typed json template.
            ///// </summary>
            ///// <param name="fileSpec">The file spec.</param>
            ///// <returns>a Schema instance</returns>
            //public static TObject ReadJsonTemplateFromFile(string fileSpec) {
            //    string content = ReadUtf8File(fileSpec);
            //    var t = _CreateFromJs<Json, TObject>(content, fileSpec, false);
            //    if (t.ClassName == null) {
            //        t.ClassName = Path.GetFileNameWithoutExtension(fileSpec);
            //    }
            //    return (TObject)t;
            //}

            ///// <summary>
            ///// Reads the UTF8 file.
            ///// </summary>
            ///// <param name="fileSpec">The file spec.</param>
            ///// <returns>System.String.</returns>
            //private static string ReadUtf8File(string fileSpec) {
            //    byte[] buffer = null;
            //    using (FileStream fileStream = new FileStream(
            //        fileSpec,
            //        FileMode.Open,
            //        FileAccess.Read,
            //        FileShare.ReadWrite)) {

            //        long len = fileStream.Length;
            //        buffer = new byte[len];
            //        fileStream.Read(buffer, 0, (int)len);
            //    }

            //    return Encoding.UTF8.GetString(buffer);
            //}
        }
    }
}
