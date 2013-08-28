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
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public partial class Schema<JsonType> : TContainer 
        where JsonType : Json<object>, new()
    {

        public override Type MetadataType {
            get { return typeof(ObjMetadata<Schema<Json<object>>, JsonType>); }
        }
        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static Schema() {
            HelperFunctions.LoadNonGACDependencies();
//            XSON.CodeGeneration.Initializer.InitializeXSON();
            Starcounter_XSON_JsonByExample.Initialize();
        }
        /// <summary>
        /// Allows implicit casting from Schema&ltJson&ltMyClass&gt&gt to Schema&ltJson&ltobject&gt&gt
        /// </summary>
        /// <remarks>
        /// This method does nothing but is needed for C# to compile.
        /// The method does nothing but return the same object as it is supplied
        /// with. The C# compiler fails to recognize the "class" constrain in the
        /// where statement of the Json&ltDataType&gt class declaration means that all instances of
        /// this class are also already Schema&ltJson&ltobject&gt&gt instances.
        /// </remarks>
        /// <param name="self">The object to cast</param>
        /// <returns>The same object (self) is returned without further action</returns>
        public static implicit operator Schema<Json<object>>(Schema<JsonType> self) {
            return self as Schema<Json<object>>;
        }

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
            where TypeObj : Json<object>, new()
                    where TypeTObj : Schema<TypeObj>, new() {
            IXsonTemplateMarkupReader reader;
            try {
                reader = Modules.Starcounter_XSON_JsonByExample.MarkupReaders[format];
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
        public static Schema<Json<object>> CreateFromJson(string json) {
            return CreateFromMarkup<Json<object>, Json<object>.JsonByExample.Schema<Json<object>>>("json", json, null);
        }

        private string instanceDataTypeName;


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
        public Schema() {
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
                    return typeof(Json<object>);
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
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new property</param>
        /// <returns>The new property template</returns>
        public TValue AddExperimental<T>(string name) {
            return this.Add(typeof(T), name);
        }




        /// <summary>
        /// Creates a new typed array property (template) with the specified name and type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of each element in the array</param>
        /// <returns>The new property template</returns>
        public T Add<T>(string name, Schema<Json<object>> type) where T : TObjArr, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, ElementType = type };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }


        


        private static readonly Dictionary<Type, Func<Schema<Json<object>>,string,TValue>> @switch = new Dictionary<Type, Func<Schema<Json<object>>,string,TValue>> {
                    { typeof(byte), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt16), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int16), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt32), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int32), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt64), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int64), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(float), (Schema<Json<object>> t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(double), (TJson t, string name) => { return t.Add<TDouble>(name); }},
                    { typeof(decimal), (TJson t, string name) => { return t.Add<TDecimal>(name); }},
                    { typeof(bool), (TJson t, string name) => { return t.Add<TBool>(name); }},
                    { typeof(string), (TJson t, string name) => { return t.Add<TString>(name); }}
            };

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of the new property</param>
        /// <returns>The new property template</returns>
        public TValue Add(Type type, string name) {


            Func<TJson, string, TValue> t;
            if (@switch.TryGetValue(type,out t)) {
                return t.Invoke(this,name);
            }
            if (typeof(IEnumerable<Json<object>>).IsAssignableFrom(type)) {
                return this.Add<ArrSchema<Json<object>>>(name);
            }
//            if (typeof(IEnumerator<Obj>).IsAssignableFrom(type)) {
//                return this.Add<TArr<Obj, TDynamicObj>>(name);
//            }
            if (typeof(Json<object>).IsAssignableFrom(type)) {
                return this.Add<TJson>(name);
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
                t = new T() { TemplateName = name, Bind = bind, Bound = Bound.Yes };
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
        public T Add<T>(string name, TJson type, string bind) where T : TObjArr, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, ElementType = type, Bind = bind, Bound = Bound.Yes };
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
        public override void ProcessInput(Json<object> obj, byte[] value) {
            throw new NotImplementedException();
        }

        protected override IReadOnlyList<IReadOnlyTree> _Children {
            get {
                return this.Properties;
            }
        }

    }
}
