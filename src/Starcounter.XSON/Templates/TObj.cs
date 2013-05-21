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
using Starcounter.XSON;
using Starcounter.XSON.Serializers;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public abstract class TObj : TContainer {
        internal static TypedJsonSerializer FallbackSerializer = DefaultSerializer.Instance;

        private static object syncRoot = new object();
        private DataValueBinding<IBindable> dataBinding;
        private bool bindChildren;
        private TypedJsonSerializer codegenSerializer;
        private bool shouldUseCodegeneratedSerializer = true;
        private bool codeGenStarted = false;

        private void GenerateSerializer(object state){
            TypedJsonSerializer tjs;

            //lock (syncRoot) {
                tjs = Obj.Factory.CreateJsonSerializer(this);
            //}

            // it doesn't really matter if setting the variable in the template is synchronized 
            // or not since if the serializer is null a fallback serializer will be offset instead.
            this.codegenSerializer = tjs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TypedJsonSerializer Serializer {
            get {
                if (codegenSerializer != null) {
                    return codegenSerializer;
                } else {
                    if (UseCodegeneratedSerializer) {
                        // TODO:
                        // Is this lock needed? Want to make sure only one serializer is generated 
                        // per template instance, but maybe we can assume that it is only accessed 
                        // by one thread at a time. It doesn't really matter either if a serializer
                        // is generated more than once (other than unnecessary resource usage)
                        //lock (this) {
                        if (!codeGenStarted) {
                            codeGenStarted = true;
//                            ThreadPool.QueueUserWorkItem(this.GenerateSerializer);
                            GenerateSerializer(null);
                            return codegenSerializer;
                        }
                        //}
                    }
                    return FallbackSerializer;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseCodegeneratedSerializer {
            get {
                if (Obj.Factory == null)
                    return false;
                return GetRootTemplate().shouldUseCodegeneratedSerializer;
            }
            set {
                TObj root = GetRootTemplate();
                root.shouldUseCodegeneratedSerializer = value;
                if (!value) {
                    root.InvalidateSerializer();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void InvalidateSerializer() {
            //lock (this) {
                codeGenStarted = false;
                codegenSerializer = null;
            //}
        }

        /// <summary>
        /// Returns the topmost instance of the typedjson templatetree
        /// </summary>
        /// <returns></returns>
        private TObj GetRootTemplate() {
            TContainer current = this;
            while (current.Parent != null)
                current = current.Parent;
            return (TObj)current;
        }

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
        public string InstanceDataTypeName { get; set; }

        /// <summary>
        /// Creates a new template with the specified name and type and
        /// adds it to this apps propertylist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new template</param>
        /// <returns>A new instance of the specified template</returns>
        public T Add<T>(string name) where T : Template, new() {
            T t = new T() { TemplateName = name };
            Properties.Add(t);
            return t;
        }

        /// <summary>
        /// Creates a new template with the specified name and type and
        /// adds it to this apps propertylist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the new template</param>
        /// <param name="type"></param>
        /// <returns>A new instance of the specified template</returns>
        public T Add<T>(string name, TObj type) where T : TObjArr, new() {
            T t = new T() { TemplateName = name, App = type };
            Properties.Add(t);
            return t;
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
            T t = new T() { TemplateName = name, Bind = bind, Bound = true };
            Properties.Add(t);
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
            T t = new T() { TemplateName = name, App = type, Bind = bind, Bound = true };
            Properties.Add(t);
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
        /// If set to true all children will be automatically bound to the dataobject, 
        /// otherwise the children needs to set binding themselves.
        /// </summary>
        /// <remarks>
        /// If set to true and a child which should not be bound is added the name of the
        /// child should start with a '_' (underscore).
        /// </remarks>
        public bool BindChildren {
            get { return bindChildren; }
            set {
                if (Properties.Count > 0) {
                    throw new InvalidOperationException("Cannot change this property after children have been added.");
                }
                bindChildren = value;
            }
        }

        /// <summary>
        /// Callback when a child is added to this object properties.
        /// </summary>
        /// <param name="property"></param>
        internal void OnPropertyAdded(Template property) {
            TValue value;
            string propertyName;

            if (BindChildren) {
                value = property as TValue;
                if (value != null) {
                    if (!value.Bound) {
                        propertyName = value.PropertyName;
                        if (!string.IsNullOrEmpty(propertyName)
                            && !(propertyName[0] == '_')) {
                            value.Bind = propertyName;
                        }
                    } else if (value.Bind == null) {
                        value.Bound = false;
                    }
                }
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

        internal DataValueBinding<IBindable> GetBinding(IBindable data) {
            dataBinding = DataBindingFactory.VerifyOrCreateBinding<IBindable>(this, dataBinding, data.GetType(), Bind);
            return dataBinding;
        }
    }
}
