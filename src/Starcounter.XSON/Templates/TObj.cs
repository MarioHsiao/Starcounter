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

        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static TObj() {
            HelperFunctions.LoadNonGACDependencies();
            XSON.CodeGeneration.Initializer.InitializeXSON();
        }

        internal static TypedJsonSerializer FallbackSerializer = DefaultSerializer.Instance;
        private static bool shouldUseCodegeneratedSerializer = true;

        private DataValueBinding<IBindable> dataBinding;
        private bool bindChildren;
        private TypedJsonSerializer codegenSerializer;
        private bool codeGenStarted = false;
        private string instanceDataTypeName;

        internal void GenerateSerializer(object state){
            // it doesn't really matter if setting the variable in the template is synchronized 
            // or not since if the serializer is null a fallback serializer will be used instead.
            this.codegenSerializer = Obj.Factory.CreateJsonSerializer(this);
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
                if (Obj.Factory == null)
                    return false;
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
                bindChildren = value;
                if (Properties.Count > 0) {
                    if (value == true) {
                        foreach (var child in Properties) {
                            CheckBindingForChild(child);
                        }
                    }
                }
            }
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
        /// 
        /// </summary>
        /// <param name="child"></param>
        private void CheckBindingForChild(Template child) {
            TValue value;
            string propertyName;

            value = child as TValue;
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
