// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Advanced.XSON;
using Modules;
using Starcounter.Internal.XSON.DeserializerCompiler;
using Starcounter.Internal.XSON;
using System.Collections;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public partial class TObj : TContainer {

        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static TObj() {
            HelperFunctions.LoadNonGACDependencies();
//            XSON.CodeGeneration.Initializer.InitializeXSON();
            Starcounter_XSON_JsonByExample.Initialize();
        }

        /// <summary>
        /// By default, Starcounter creates
        /// a JSON-by-example reader that allows you to convert a JSON file to a XOBJ template using the format
        /// string "json". You can inject other template formats here.
        /// </summary>
        public static Dictionary<string, IXsonTemplateMarkupReader> MarkupReaders = new Dictionary<string, IXsonTemplateMarkupReader>();

        /// <summary>
        /// CreateFromMarkup
        /// </summary>
        /// <typeparam name="TypeObj"></typeparam>
        /// <typeparam name="TypeTObj"></typeparam>
        /// <param name="format"></param>
        /// <param name="markup"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static TypeTObj CreateFromMarkup<TypeObj,TypeTObj>(string format, string markup, string origin )
                    where TypeObj : Obj, new()
                    where TypeTObj : TObj, new() {
            IXsonTemplateMarkupReader reader;
            try {
                reader = TObj.MarkupReaders[format];
            }
            catch {
                throw new Exception(String.Format("Cannot create an XSON template. No markup compiler is registred for the format {0}.", format));
            }

            return reader.CompileMarkup<TypeObj,TypeTObj>(markup,origin);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static TJson CreateFromJson(string json) {
            return CreateFromMarkup<Json,TJson>("json", json, null);
        }

        internal static TypedJsonSerializer FallbackSerializer = DefaultSerializer.Instance;
        private static bool shouldUseCodegeneratedSerializer = true;

        internal DataValueBinding<IBindable> dataBinding;
        private bool bindChildren;
        private TypedJsonSerializer codegenSerializer;
        private bool codeGenStarted = false;
        private string instanceDataTypeName;

        internal void GenerateSerializer(object state){
            // it doesn't really matter if setting the variable in the template is synchronized 
            // or not since if the serializer is null a fallback serializer will be used instead.
            this.codegenSerializer = SerializerCompiler.The.CreateTypedJsonSerializer(this);   //Obj.Factory.CreateJsonSerializer(this);
        }

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

        /// <summary>
        /// The _ class name
        /// </summary>
        internal string _ClassName;

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName {
            get {
                return _ClassName;
            }
            set {
                _ClassName = value;
            }
        }



        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        /// <value></value>
        public string Namespace { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public string Include { get; set; }

        /// <summary>
        ///
        /// </summary>
        private PropertyList _PropertyTemplates;

        /// <summary>
        /// Initializes a new instance of the <see cref="TObj" /> class.
        /// </summary>
        public TObj() {
            _PropertyTemplates = new PropertyList(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected Type _AppType;

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (_AppType == null) {
                    return typeof(Obj);
                }
                return _AppType;
            }
            set { _AppType = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string InstanceDataTypeName {
            get { return instanceDataTypeName; }
            set {
                instanceDataTypeName = value;
                if (!string.IsNullOrEmpty(value))
                    BindChildren = true;
            }
        }

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new property</param>
        /// <returns>The new property template</returns>
        public T Add<T>(string name) where T : Template, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }

        /// <summary>
        /// Creates a new typed array property (template) with the specified name and type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of each element in the array</param>
        /// <returns>The new property template</returns>
        public T Add<T>(string name, TObj type) where T : TObjArr, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, App = type };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of the new property</param>
        /// <returns>The new property template</returns>
        public Template Add(Type type, string name) {

            var @switch = new Dictionary<Type, Func<Template>> {
                    { typeof(byte), () => { return this.Add<TLong>(name); }},
                    { typeof(UInt16), () => { return this.Add<TLong>(name); }},
                    { typeof(Int16), () => { return this.Add<TLong>(name); }},
                    { typeof(UInt32), () => { return this.Add<TLong>(name); }},
                    { typeof(Int32), () => { return this.Add<TLong>(name); }},
                    { typeof(UInt64), () => { return this.Add<TLong>(name); }},
                    { typeof(Int64), () => { return this.Add<TLong>(name); }},
                    { typeof(float), () => { return this.Add<TLong>(name); }},
                    { typeof(double), () => { return this.Add<TDouble>(name); }},
                    { typeof(decimal), () => { return this.Add<TDecimal>(name); }},
                    { typeof(bool), () => { return this.Add<TBool>(name); }},
                    { typeof(string), () => { return this.Add<TString>(name); }}
            };
            Func<Template> t;
            if (@switch.TryGetValue(type,out t)) {
                return t.Invoke();
            }
            if (typeof(IEnumerable<Obj>).IsAssignableFrom(type)) {
                return this.Add<TArr<Obj,TObj>>(name);
            }
//            if (typeof(IEnumerator<Obj>).IsAssignableFrom(type)) {
//                return this.Add<TArr<Obj, TDynamicObj>>(name);
//            }
            if (typeof(Obj).IsAssignableFrom(type)) {
                return this.Add<TObj>(name);
            }
            throw new Exception(String.Format("Cannot add the {0} property to the template as the type {1} is not supported for Json properties", name, type.Name));
        }

        /// <summary>
        /// Creates a new typed array property (template) with the specified name and type.
        /// </summary>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of the new property</param>
        /// <param name="elementType">The type of each element in the array</param>
        /// <returns>The new property template</returns>
        public Template Add(Type type, string name, Type elementType ) {

            throw new NotImplementedException();

            throw new Exception( String.Format("Cannot add the {0} property to the template as the type {1} is not supported for Json properties",name,type.Name));
        }

        /// <summary>
        /// Creates a new template with the specified name and type and
        /// adds it to this apps propertylist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new template</param>
        /// <param name="bind">The name of the property in the dataobject to bind to.</param>
        /// <returns>A new instance of the specified template</returns>
        public T Add<T>(string name, string bind) where T : TValue, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, Bind = bind, Bound = true };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }

        /// <summary>
        /// Creates a new template with the specified name and type and
        /// adds it to this apps propertylist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new template</param>
        /// <param name="type"></param>
        /// <param name="bind">The name of the property in the dataobject to bind to.</param>
        /// <returns>A new instance of the specified template</returns>
        public T Add<T>(string name, TObj type, string bind) where T : TObjArr, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, App = type, Bind = bind, Bound = true };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }

        /// <summary>
        /// Gets a list of all properties for this app.
        /// </summary>
        /// <value></value>
        public PropertyList Properties { get { return _PropertyTemplates; } }

        /// <summary>
        /// Gets an enumeration of all templates for this app.
        /// </summary>
        /// <value></value>
        public override IEnumerable<Template> Children {
            get { return (IEnumerable<Template>)Properties; }
        }


        /// <summary>
        /// Callback when a child is added to this object properties.
        /// </summary>
        /// <param name="property"></param>
        internal void OnPropertyAdded(Template property) {
            if (BindChildren) {
                CheckBindingForChild(property);
            }
        }

        /// <summary>
        /// Callback from internal functions responsible for handle external inputs.
        /// </summary>
        /// <param name="obj">The parent obj.</param>
        /// <param name="value">The input value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(Obj obj, byte[] value) {
            throw new NotImplementedException();
        }

    }
}
