// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Advanced.XSON;
using Modules;
using Starcounter.Internal.XSON.DeserializerCompiler;
using Starcounter.Internal.XSON;
using System.Collections;
using TJson = Starcounter.Templates.TObject;
using Module = Modules.Starcounter_XSON_JsonByExample;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public partial class TObject : TContainer {
        public override Type MetadataType {
            get { return typeof(ObjMetadata<TObject, Json>); }
        }
        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static TObject() {
            HelperFunctions.LoadNonGACDependencies();
//            XSON.CodeGeneration.Initializer.InitializeXSON();
            Starcounter_XSON_JsonByExample.Initialize();
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
            where TypeObj : Json, new()
                    where TypeTObj : TObject, new() {
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
        public static TObject CreateFromJson(string json) {
            return CreateFromMarkup<Json, Json.JsonByExample.Schema>("json", json, null);
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
        public TObject() {
            _PropertyTemplates = new PropertyList(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected Type _JsonType;

		/// <summary>
		/// Creates a new Message using the schema defined by this template
		/// </summary>
		/// <param name="parent">The parent for the new message (if any)</param>
		/// <returns>The new message</returns>
		public override object CreateInstance(Container parent) {
			if (_JsonType != null) {
				var msg = (Json)Activator.CreateInstance(_JsonType);
				msg.Template = this;
				msg.Parent = parent;
				return msg;
			}
			return new Json() { Template = this, Parent = parent };
		}

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (_JsonType == null) {
                    return typeof(Json);
                }
                return _JsonType;
            }
            set { _JsonType = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string InstanceDataTypeName {
            get { return instanceDataTypeName; }
            set {
                instanceDataTypeName = value;
                if (!string.IsNullOrEmpty(value))
					BindChildren = Bound.Yes;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal override object Wrap(object value) {
            if (value is IBindable) {
                return Wrap<Json>(value);
            }
            return value;
        }

        /// <summary>
        /// Returns a value as a JSON value.
        /// </summary>
        /// <typeparam name="ReturnType"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        internal ReturnType Wrap<ReturnType>(object value) where ReturnType : Json {
            if (value is ReturnType)
                return (ReturnType)value;
            var json = (Json)CreateInstance();
            json.Data = value;
            if (!(json is ReturnType)) {
                throw new Exception(
                    String.Format("Cannot convert {0} to {1}",
                        json, typeof(ReturnType)
                    ));
            }
            return (ReturnType)json;
        }

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new property</param>
        /// <returns>The new property template</returns>
        public T Add<T>(string name) where T : Template, new() {
            var @new = new T() { TemplateName = name };
            var t = Properties.GetTemplateByName(name);
            if (t == null) {
                Properties.Add(@new);
                return @new;
            } else {
                Properties.Replace(@new);
                Properties.Expose(@new);
            }

            return @new;
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
        public T Add<T>(string name, TObject type) where T : TObjArr, new() {
            T t = (T)Properties.GetTemplateByName(name);
            if (t == null) {
                t = new T() { TemplateName = name, ElementType = type };
                Properties.Add(t);
            } else {
                Properties.Expose(t);
            }

            return t;
        }


        


        private static readonly Dictionary<Type, Func<TObject,string,TValue>> @switch = new Dictionary<Type, Func<TObject,string,TValue>> {
                    { typeof(byte), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt16), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int16), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt32), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int32), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt64), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int64), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(float), (TObject t, string name) => { return t.Add<TLong>(name); }},
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
            if (typeof(IEnumerable<Json>).IsAssignableFrom(type)) {
                return this.Add<TArray<Json>>(name);
            }
//            if (typeof(IEnumerator<Obj>).IsAssignableFrom(type)) {
//                return this.Add<TArr<Obj, TDynamicObj>>(name);
//            }
            if (typeof(Json).IsAssignableFrom(type)) {
                return this.Add<TJson>(name);
            }
            if ((typeof(IEnumerable).IsAssignableFrom(type))) {
                return this.Add<TObjArr>(name);
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
                t = new T() { TemplateName = name, Bind = bind};
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
                t = new T() { TemplateName = name, ElementType = type, Bind = bind};
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
        }

        /// <summary>
        /// Callback from internal functions responsible for handle external inputs.
        /// </summary>
        /// <param name="obj">The parent obj.</param>
        /// <param name="value">The input value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(Json obj, byte[] value) {
            throw new NotImplementedException();
        }

        protected override IReadOnlyList<IReadOnlyTree> _Children {
            get {
                return this.Properties;
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public override string ToJson(Json json) {
			byte[] buffer;
			int count = ToJsonUtf8(json, out buffer);
			return Encoding.UTF8.GetString(buffer, 0, count);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public override byte[] ToJsonUtf8(Json json) {
			byte[] buffer;
			byte[] sizedBuffer;
			int count = ToJsonUtf8(json, out buffer);
			sizedBuffer = new byte[count];
			Buffer.BlockCopy(buffer, 0, sizedBuffer, 0, count);
			return sizedBuffer;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public override int ToJsonUtf8(Json json, out byte[] buffer) {
			return JsonSerializer.Serialize(json, out buffer);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public override int ToFasterThanJson(Json json, out byte[] buffer) {
			return FTJSerializer.Serialize(json, out buffer);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="jsonStr"></param>
		public override void PopulateFromJson(Json json, string jsonStr) {
			byte[] buffer = Encoding.UTF8.GetBytes(jsonStr);
			PopulateFromJson(json, buffer, buffer.Length);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="srcPtr"></param>
		/// <param name="srcSize"></param>
		/// <returns></returns>
		public override int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize) {
			return JsonSerializer.Populate(json, srcPtr, srcSize);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="src"></param>
		/// <param name="srcSize"></param>
		/// <returns></returns>
		public override int PopulateFromJson(Json json, byte[] src, int srcSize) {
			unsafe {
				fixed (byte* p = src) {
					return PopulateFromJson(json, (IntPtr)p, srcSize);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <param name="srcPtr"></param>
		/// <param name="srcSize"></param>
		/// <returns></returns>
		public override int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize) {
			return FTJSerializer.Populate(json, srcPtr, srcSize);
		}
    }
}
