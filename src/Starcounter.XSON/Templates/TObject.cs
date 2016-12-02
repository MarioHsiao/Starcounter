// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.XSON;
using Starcounter.Advanced.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public class TObject : TContainer {
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
        /// For dynamic Json objects, templates pertain to only a single object.
        /// </summary>
        internal Json SingleInstance = null;

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

		public void SetCustomBoundAccessors(Func<Json, object> boundGetter, Action<Json, object> boundSetter) {
            BindingStrategy = BindingStrategy.Bound;
            BoundGetter = boundGetter;
			BoundSetter = boundSetter;
			hasCustomBoundAccessors = true;
		}

		internal override void SetDefaultValue(Json parent, bool markAsReplaced = false) {
			UnboundSetter(parent, (Json)CreateInstance(parent));

            if (markAsReplaced && parent != null && parent.HasBeenSent) {
                parent.MarkAsDirty(TemplateIndex);
                parent.CallHasChanged(this);
            }
        }

		internal override void InvalidateBoundGetterAndSetter() {
            if (hasCustomBoundAccessors)
                return; // Never invalidate custom accessors.

            BoundGetter = null;
			BoundSetter = null;
			base.InvalidateBoundGetterAndSetter();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override bool HasBinding() {
            return (BoundGetter != null);
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
            if (value != null) {
                if (UseBinding(parent)) {
                    if (parent.AutoRefreshBoundProperties)
                        value.CheckBoundObject(BoundGetter(parent));
                    parent.MarkAsCached(TemplateIndex);
                }
                value.SetBoundValuesInTuple();
            }             
		}

		internal override Json GetValue(Json parent) {
			var json = UnboundGetter(parent);

            if (json != null && json.checkBoundProperties && UseBinding(parent)) {
				json.CheckBoundObject(BoundGetter(parent));
                parent.MarkAsCached(this.TemplateIndex);
			}

			return json;
		}

		internal void SetValue(Json parent, object value) {
			Json current = UnboundGetter(parent);
			if (current != null)
				current.AttachData(value, true);
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
            if (parent.checkBoundProperties && value != null && UseBinding(parent))
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
				parent.MarkAsDirty(TemplateIndex);

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
			bool overwrite = (overwriteExisting || !hasCustomAccessors);

			if (overwrite || UnboundGetter == null) {
				UnboundGetter = getter;
#if DEBUG
				DebugUnboundGetter = "<custom>";
#endif
			}
			if (overwrite || UnboundSetter == null) {
                customSetter = setter;
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

        /// <summary>
        /// Creates a new property (template) with the specified name and type.
        /// </summary>
        /// <param name="name">The name of the new property</param>
        /// <param name="type">The type of the new property</param>
        /// <returns>The new property template</returns>
        public TValue Add(Type type, string name) {
            return DynamicFunctions.AddTemplateFromType(type, this, name);
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
                t.Bind = bind;
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
                t.Bind = bind;
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
        
        /// <summary>
        /// Called when the user attempted to set a value on a dynamic obj without the object having
        /// the property defined (in its template). In the TDynamicObj template, this method will
        /// add the property to the template definition. The default behaviour in the implementation
        /// with a strict schema template, a exception will be called.
        /// </summary>
        /// <param name="property">The name of the missing property</param>
        /// <param name="Type">The type of the value being set</typeparam>
        internal void OnSetUndefinedProperty(string propertyName, Type type) {
            if (this.IsDynamic) {
                TValue property = this.Add(type, propertyName);
                this.SingleInstance.OnUndefinedPropertyAdded(property);
            } else {
                throw new Exception(String.Format("An attempt was made to set the property {0} to a {1} on an Obj object not having the property defined in the template.", propertyName, type.Name));
            }
        }
    }
}
