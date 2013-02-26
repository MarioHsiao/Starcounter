// ***********************************************************************
// <copyright file="TApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;
using Starcounter.Templates.Interfaces;
using Starcounter.Advanced;

namespace Starcounter.Templates {

    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public abstract class TObj : TContainer {
        private DataBinding<IBindable> dataBinding;

        /// <summary>
        /// Registers a template with the specified name.
        /// </summary>
        /// <typeparam name="TTemplate">The type of the template to register</typeparam>
        /// <param name="name">Name of the template</param>
        /// <param name="editable">if set to <c>true</c> the value should be editable</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <returns>A new instance of the specified template</returns>
        public TTemplate Register<TTemplate>(string name, string dotNetName, bool editable = false)
            where TTemplate : Template, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                PropertyName = dotNetName,
                Editable = editable
            };
        }

        /// <summary>
        /// Registers the specified name.
        /// </summary>
        /// <typeparam name="TTemplate">The type of the T template.</typeparam>
        /// <typeparam name="TValue">The type of the T value.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        /// <returns>``0.</returns>
        public TTemplate Register<TTemplate, TValue>(
            string name,
            string dotNetName,
            bool editable = false)
            where TTemplate : TValue<TValue>, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
            };
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
        internal Type _AppType;

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
        /// Callback from internal functions responsible for handle external inputs.
        /// </summary>
        /// <param name="obj">The parent obj.</param>
        /// <param name="value">The input value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal override void ProcessInput(Obj obj, byte[] value) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<Obj, IBindable> dataGetter) {
            dataBinding = new DataBinding<IBindable>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        /// <param name="dataSetter"></param>
        public void AddDataBinding(Func<Obj, IBindable> dataGetter, Action<Obj, IBindable> dataSetter) {
            dataBinding = new DataBinding<IBindable>(dataGetter, dataSetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public IBindable GetBoundValue(Obj app) {
            return dataBinding.GetValue(app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="entity"></param>
        public void SetBoundValue(Obj app, IBindable entity) {
            dataBinding.SetValue(app, entity);
        }
    }
}
