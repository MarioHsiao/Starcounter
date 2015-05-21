﻿// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public partial class TObject : TContainer {
        // When a custom setter is used we need to add logic for setting parent.
        // The original setter is saved here, and the setter with added code (which uses)
        // this one is set as UnboundSetter (and maybe Setter)
        private Action<Json, Json> customSetter;

		public Action<Json, Json> Setter;
		public Func<Json, Json> Getter;
		internal Action<Json, object> BoundSetter;
		internal Func<Json, object> BoundGetter;
		internal Action<Json, Json> UnboundSetter;
		internal Func<Json, Json> UnboundGetter;

		private PropertyList _PropertyTemplates;

		private BindingStrategy bindChildren = BindingStrategy.Auto;
		public bool HasAtLeastOneBoundProperty = true; // TODO!
		
		/// <summary>
		/// Static constructor to automatically initialize XSON.
		/// </summary>
		static TObject() {
			HelperFunctions.PreLoadCustomDependencies();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TObj" /> class.
		/// </summary>
		public TObject() {
			_PropertyTemplates = new PropertyList(this);
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		internal override void SetDefaultValue(Json parent) {
			UnboundSetter(parent, (Json)CreateInstance(parent));
		}

		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			base.InvalidateBoundGetterAndSetter();
		}

		internal override bool GenerateBoundGetterAndSetter(Json parent) {
			TemplateDelegateGenerator.GenerateBoundDelegates(this, parent);
			return (BoundGetter != null);
		}

		internal override void GenerateUnboundGetterAndSetter() {
			if (UnboundGetter == null)
				TemplateDelegateGenerator.GenerateUnboundDelegates(this, false);
		}

		internal override void Checkpoint(Json parent) {
			var json = UnboundGetter(parent);
			if (json != null)
				json.CheckpointChangeLog();
			base.Checkpoint(parent);
		}

		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			Json value = UnboundGetter(parent);
			if (value != null)
				value.SetBoundValuesInTuple();
		}

		internal override Json GetValue(Json parent) {
			var json = UnboundGetter(parent);

            if (json != null && !json._checkBoundProperties && UseBinding(parent)) {
				json.CheckBoundObject(BoundGetter(parent));
			}

			return json;
		}

		internal void SetValue(Json parent, object value) {
			Json current = UnboundGetter(parent);
			if (current != null)
				current.AttachData(value);
		}

		internal override object GetUnboundValueAsObject(Json parent) {
			return UnboundGetter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal override object GetValueAsObject(Json parent) {
			return Getter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="value"></param>
		internal override void SetValueAsObject(Json parent, object value) {
			Setter(parent, (Json)value);
		}

		private Json BoundOrUnboundGet(Json parent) {
            if (UnboundGetter == null)
                return parent;

			Json value = UnboundGetter(parent);
            if (parent._checkBoundProperties && value != null && UseBinding(parent))
				value.CheckBoundObject(BoundGetter(parent));
			return value;
		}

		private void BoundOrUnboundSet(Json parent, Json value) {
            UpdateParentAndIndex(parent, value);

			if (value != null) {
				if (UseBinding(parent) && BoundSetter != null)
					BoundSetter(parent, value.Data);
			}
			UnboundSetter(parent, value);

			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);

			parent.CallHasChanged(this);
		}

		internal override void CopyValueDelegates(Template toTemplate) {
			var p = toTemplate as TObject;
			if (p != null) {
                p.customSetter = customSetter;
				p.UnboundGetter = UnboundGetter;
				p.UnboundSetter = UnboundSetter;
				p.hasCustomAccessors = hasCustomAccessors;
#if DEBUG
				p.DebugUnboundGetter = DebugUnboundGetter;
				p.DebugUnboundSetter = DebugUnboundSetter;
#endif
			}
		}

        private void SetParentAndUseCustomSetter(Json parent, Json value) {
            UpdateParentAndIndex(parent, value);
            customSetter(parent, value);
        }

		/// <summary>
		/// Sets the getter and setter delegates for unbound values to the submitted delegates.
		/// </summary>
		/// <param name="getter"></param>
		/// <param name="setter"></param>
		/// <param name="overwriteExisting">
		/// If false the new delegates are only set if the current delegates are null.
		/// </param>
		public void SetCustomAccessors(Func<Json, Json> getter, 
									   Action<Json, Json> setter,
									   bool overwriteExisting = true) {
            customSetter = setter;
			bool overwrite = (overwriteExisting || !hasCustomAccessors);

			if (overwrite || UnboundGetter == null) {
				UnboundGetter = getter;
#if DEBUG
				DebugUnboundGetter = "<custom>";
#endif
			}
			if (overwrite || UnboundSetter == null) {
                UnboundSetter = SetParentAndUseCustomSetter;
#if DEBUG
				DebugUnboundSetter = "<custom>";
#endif
			}

			hasCustomAccessors = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public BindingStrategy BindChildren {
			get { return bindChildren; }
			set {
				if (value == Templates.BindingStrategy.UseParent)
					throw new Exception("Cannot specify Bound.UseParent on this property.");
				bindChildren = value;
			}
		}

        public override Type MetadataType {
            get { return typeof(ObjMetadata<TObject, Json>); }
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public string Include { get; set; }

		/// <summary>
		/// Creates a new Message using the schema defined by this template
		/// </summary>
		/// <param name="parent">The parent for the new message (if any)</param>
		/// <returns>The new message</returns>
		public override object CreateInstance(Json parent = null) {
			if (jsonType != null) {
				var msg = (Json)Activator.CreateInstance(jsonType);
				msg.Template = this;
				msg.Parent = parent;
				return msg;
			}
            return base.CreateInstance(parent);
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

        internal static readonly Dictionary<Type, Func<TObject,string,TValue>> @switch = new Dictionary<Type, Func<TObject,string,TValue>> {
                    { typeof(byte), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt16), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int16), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt32), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int32), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(UInt64), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(Int64), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(float), (TObject t, string name) => { return t.Add<TLong>(name); }},
                    { typeof(double), (TObject t, string name) => { return t.Add<TDouble>(name); }},
                    { typeof(decimal), (TObject t, string name) => { return t.Add<TDecimal>(name); }},
                    { typeof(bool), (TObject t, string name) => { return t.Add<TBool>(name); }},
                    { typeof(string), (TObject t, string name) => { return t.Add<TString>(name); }}
            };

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of the new property</param>
        /// <returns>The new property template</returns>
        public TValue Add(Type type, string name) {


            Func<TObject, string, TValue> t;
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
                return this.Add<TObject>(name);
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
        public T Add<T>(string name, TObject type, string bind) where T : TObjArr, new() {
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
			var tv = property as TValue;
			if (tv != null)
				tv.GenerateUnboundGetterAndSetter();
        }

        protected override IReadOnlyList<IReadOnlyTree> _Children {
            get {
                return this.Properties;
            }
        }

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Object; }
        }
    }
}
