// ***********************************************************************
// <copyright file="Obj.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter {
    /// <summary>
    /// A Json object represents tree of values that can be serialized to or from a 
    /// JSON string.
    /// 
    /// A value can be a primitive such as numbers, strings and booleans or they can be other Json objects.or
    /// arrays of values.
    /// 
    /// The objects mimics the types of trees inducable from the JSON string format.
    /// The difference from the Json induced object tree in Javascript is
    /// foremost that Obj supports multiple numeric types, time and higher precision numerics.
    /// 
    /// While JSON is a text based notation format, the Json class is a materialized
    /// tree of arrays, objects and primitive values than can be serialized 
    /// to and from the corresponding JSON text.
    /// 
    /// The types supported are:
    ///
    /// Object			    (can contain properties of any supported type)
    /// List			    (typed array/list/vector of any supported type),
    /// null            
    /// Time 			    (datetime)
    /// Boolean
    /// String 			    (variable length Unicode string),
    /// Integer 		    (variable length up to 64 bit, signed)
    /// Unsigned Integer	(variable length up to 64 bit, unsigned)
    /// Decimal			    (base-10 floating point up to 64 bit),
    /// Float			    (base-2 floating point up to 64 bit)
    /// </summary>
    /// <remarks>
    /// The current implementation has a few shortcommings. Currently Json only supports arrays of objects.
    /// Also, all objects in the array must use the same Schema.
    /// 
    /// In the release version of Starcounter, Obj objects trees will be optimized for storage in "blobs" rather than on
    /// the garbage collected heap. This is such that stateful sessions can employ them without causing unnecessary system
    /// stress.
    ///
    /// A Json object can be data bound to a database object such as its bound properties
    /// merely reflect the values of the database objects.
    /// </remarks>
    public partial class Json {
        /// <summary>
        /// Base classes to be derived by Json-by-example classes.
        /// </summary>
        public static class JsonByExample {
            /// <summary>
            /// 
            /// </summary>
            public class Schema : TObject {
            }

            /// <summary>
            /// Used by to support inheritance when using Json-by-example compiler
            /// </summary>
            /// <typeparam name="TJson">The Json instance type described by this schema</typeparam>
            /// <typeparam name="TTemplate">The schema for the Json.</typeparam>
            public class Metadata<TJson, TTemplate> : ValueMetadata<TJson, TTemplate>
                where TTemplate : TValue
                where TJson : Json {

                public Metadata(TJson app, TTemplate template) : base(app, template) { }
            }
        }

        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static Json() {
            // TODO! Whay is this and why is it needed?
            HelperFunctions.PreLoadCustomDependencies();
            //    XSON.CodeGeneration.Initializer.InitializeXSON();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Json" /> class.
        /// </summary>
        public Json()
            : base() {
            cacheIndexInArr = -1;
            transaction = TransactionHandle.Invalid;
            AttachCurrentTransaction();
            trackChanges = false;
            checkBoundProperties = true;
            appName = StarcounterEnvironment.AppName;
            if (template == null) {
                Template = GetDefaultTemplate();
            }
        }

        /// <summary>
        /// Creates an instance <see cref="Json" /> class using the specified string to build a <see cref="Template" /> 
        /// and set values.
        /// </summary>
        /// <param name="jsonStr">The string containing proper JSON</param>
        public Json(string jsonStr) : this() {
            this.Template = TObject.CreateFromJson(jsonStr);
            this.PopulateFromJson(jsonStr);
        }

        /// <summary>
        /// Returns true if this instance is backed by a codegenerated template.
        /// </summary>
        public virtual bool IsCodegenerated { get { return false; } }
        
        /// <summary>
        /// The QUICKTUPLE implementation keeps the property values of an App in a simple array of 
        /// boxed CLR values. This implementation should never be used on the server side as the
        /// strain on the garbage collector and the memory consumption would be to great. Instead, the
        /// server side represetation should use the default session blob model.
        /// </summary>
        protected void _InitializeValues() {
            InitializeCache();
        }

        /// <summary>
        /// Gets the ChangeLog object from the closest parent that have one attached.
        /// </summary>
        /// <param name="lookInStepSiblings"></param>
        /// <returns></returns>
        private ChangeLog GetChangeLog(bool lookInStepSiblings) {
            var log = changeLog;

            if (log != null)
                return log;

            if (Parent != null)
                log = Parent.GetChangeLog(true);

            if (log == null && lookInStepSiblings && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    log = stepSibling.GetChangeLog(false);
                    if (log != null)
                        break;
                }
            }
            return log;
        }

        /// <summary>
        /// 
        /// </summary>
        public ChangeLog ChangeLog {
            get {
                return GetChangeLog(true);
            }
            set {
                changeLog = value;
            }
        }

        /// <summary>
        /// Json objects can be stored on the server between requests as session data.
        /// </summary>
        public Session Session {
            get {
                return GetSession(true);
            }
            set {
                if (Parent != null)
                    throw ErrorCode.ToException(Error.SCERRSESSIONJSONNOTROOT);

                if (this.session != null) {
                    // This instance is already attached to a session. We need to remove the old
                    // before setting the new.
                    this.session.Data = null;
                }

                if (value != null)
                    value.Data = this;
                this.session = value;
            }
        }

        private Session GetSession(bool lookInStepSiblings) {
            Session session = this.session;

            if (session != null)
                return session;

            if (Parent != null)
                session = Parent.GetSession(true);

            if (session == null && lookInStepSiblings && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    session = stepSibling.GetSession(false);
                    if (session != null)
                        break;
                }
            }
            return session;
        }

        internal void OnSessionSet() {
            OnAddedToViewmodel(true);
        }

        /// <summary>
        /// The schema element of this app instance
        /// </summary>
        /// <value>The template.</value>
        /// <exception cref="System.Exception">Template is already set for App. Cannot change template once it is set</exception>
        public Template Template {
            set {
                this.template = value;
                this.isArray = (this.template is TObjArr);

                if (this.template == null)
                    return;

                if (this.template is TObject && ((TObject)this.template).IsDynamic) {
                    TObject t = (TObject)this.template;
                    if (t.SingleInstance != null && t.SingleInstance != this) {
                        throw new Exception(String.Format("You cannot assign a Template ({0}) for a dynamic Json object (i.e. an Expando like object) to a new Json object ({0})", value, this));
                    }
                    ((TObject)this.template).SingleInstance = (Json)this;
                } else {
                    this.template.Sealed = true;

                    if (this.template.IsPrimitive) {
                        this.template.TemplateIndex = 0;
                        ((TValue)this.template).GenerateUnboundGetterAndSetter();
                    }
                }
#if QUICKTUPLE
                _InitializeValues();
#endif
            }
            get {
                return this.template;
            }
        }

        protected virtual void ChildArrayHasAddedAnElement(TObjArr property, int elementIndex) {
        }

        protected virtual void ChildArrayHasRemovedAnElement(TObjArr property, int elementIndex) {
        }

        protected virtual void ChildArrayHasReplacedAnElement(TObjArr property, int elementIndex) {
        }

        /// <summary>
        /// Returns True if current Obj is within the given tree.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal bool HasThisRoot(Json treeRoot) {
            Json r = this;
            while (r.Parent != null)
                r = r.Parent;
            Json root = (Json)r;

            if (treeRoot == root)
                return true;

            return false;
        }

        /// <summary>
        /// Returns the Json root.
        /// </summary>
        public Json Root {
            get {
                Json r = this;
                while (r.Parent != null)
                    r = r.Parent;
                return r;
            }
        }

        protected virtual Template GetDefaultTemplate() {
            return null;
        }

        /// <summary>
        /// Refreshes the specified property of this Obj.
        /// </summary>
        /// <param name="property">The property</param>
        public void Refresh(Template property) {
            if (property is TObjArr) {
                TObjArr tarr = (TObjArr)property;
                if (tarr.UseBinding(this)) {
                    var jsonArr = tarr.UnboundGetter(this);
                    jsonArr.CheckBoundArray(tarr.BoundGetter(this));
                }
            } else if (property is TObject) {
                var at = (TObject)property;
                if (at.UseBinding(this)) {
                    CheckBoundObject(at.BoundGetter(this));
                }
            } else {
                TValue p = property as TValue;
                if (p != null) {
                    HasChanged(p);
                }
            }
            if (this.trackChanges)
                MarkAsReplaced(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="property">The property that has changed in this Obj</param>
        protected virtual void HasChanged(TValue property) {
        }

        /// <summary>
        /// Here you can set properties for each property in this Obj (such as Editable, Visible and Enabled).
        /// The changes only affect this instance.
        /// If you which to change properties for the template, use the Template property instead.
        /// </summary>
        /// <value>The metadata.</value>
        /// <remarks>It is much less expensive to set this kind of metadata for the
        /// entire template (for example to mark a property for all Obj instances as Editable).</remarks>
        // TODO:
        // Metadata has never been used. It should either be fixed or removed.
        internal ObjMetadata<TObject, Json> Metadata { get { return metadata; } }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        /// <exception cref="System.Exception">Cannot change parent in Apps</exception>
        public Json Parent {
            get {
                return this.parent;
            }
            set {
                if (this.parent != null && this.parent != value) {
                    throw new Exception("Cannot change parent of objects in Typed JSON trees");
                }
                SetParent(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        internal void SetParent(Json value) {
            // When parent is set it means that either the json have been attached
            // to the tree for the first time or that it have been moved.
            // If session is set we need to call the remove and add methods to make sure
            // all stateful info is correct.

            // Since we change parents we need to retrieve session twice.
            if (isAddedToViewmodel && this.parent != null) {
                if (Session != null)
                    OnRemovedFromViewmodel(true);
            }

            this.parent = value;
            if (this.parent != null) {
                if (Session != null)
                    OnAddedToViewmodel(true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsArray { get { return this.isArray; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPrimitive {
            get {
                if (Template == null) {
                    return false;
                }
                return Template.IsPrimitive;
            }
        }

		public object this[int index] {
			get {
				if (this.IsArray) {
					// TODO: 
					// Should be delegate on property as well.
					return _GetAt(index);
				} else {
					TValue property = (TValue)((TObject)Template).Properties[index];
					return property.GetValueAsObject(this);
				}
			}
			set {
				if (this.IsArray) {
                    Replace(value, index);
				} else {
					TValue property = (TValue)((TObject)Template).Properties[index];
					property.SetValueAsObject(this, value);
				}
			}
		}

		public object this[string key] {
			get {
				var template = (TObject)Template;
				var prop = template.Properties[key];
				if (prop == null) {
					return null;
				}
				return this[prop.TemplateIndex];
			}
			set {
				if (Template == null)
					CreateDynamicTemplate();

				var template = (TObject)Template;
				var prop = template.Properties[key];
				if (prop == null) {
					Type type;
					if (value == null) {
						type = typeof(Json);
					} else {
						type = value.GetType();
					}
					template.OnSetUndefinedProperty(key, type);
					this[key] = value;
					return;
				}
				this[prop.TemplateIndex] = value;
			}
		}

        internal void UpdateParentAndCachedIndex(int templateIndex, Json newValue) {
            var tobj = (TObject)Template;
            var prop = tobj.Properties[templateIndex] as TContainer;
            if (prop != null) 
                prop.UpdateParentAndIndex(this, newValue);
        }
    }
}
