// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public abstract class TObj : TContainer {
        private DataValueBinding<IBindable> dataBinding;
        private bool bindChildren;

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
            T t = new T() { Name = name };
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
            T t = new T() { Name = name, App = type };
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
            T t = new T() { Name = name, Bind = bind, Bound = true };
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
            T t = new T() { Name = name, App = type, Bind = bind, Bound = true };
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
