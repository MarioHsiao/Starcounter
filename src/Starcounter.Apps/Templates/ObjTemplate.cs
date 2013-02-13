// ***********************************************************************
// <copyright file="AppTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;
using Starcounter.Templates.Interfaces;

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public class AppTemplate : ParentTemplate
#if IAPP
, IAppTemplate
#endif
 {
        private DataBinding<Entity> dataBinding;

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
            where TTemplate : Property<TValue>, new() {
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
        /// Initializes a new instance of the <see cref="AppTemplate" /> class.
        /// </summary>
        public AppTemplate() {
            _PropertyTemplates = new PropertyList(this);
        }

        /// <summary>
        /// 
        /// </summary>
        private Type _AppType;

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (_AppType == null) {
                    return typeof(App);
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
        public T Add<T>(string name) where T : ITemplate, new() {
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
        public T Add<T>(string name, IAppTemplate type) where T : IAppListTemplate, new() {
            T t = new T() { Name = name, Type = type };
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
        /// Creates a new App-instance based on this template.
        /// </summary>
        /// <param name="parent">The parent for the new app</param>
        /// <returns></returns>
        public override object CreateInstance(AppNode parent) {
            return new App() { Template = this, Parent = parent };
        }

        /// <summary>
        /// Creates a new Template with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of template to create</typeparam>
        /// <param name="name">The name of the template.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        T IAppTemplate.Add<T>(string name) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new Template with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of template to create.</typeparam>
        /// <param name="name">The name of the template.</param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        T IAppTemplate.Add<T>(string name, IAppTemplate type) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all templates for this app.
        /// </summary>
        /// <value>The properties.</value>
        IPropertyTemplates IAppTemplate.Properties {
            get { return Properties; }
        }

        /// <summary>
        /// Callback from internal functions responsible for handle external inputs.
        /// </summary>
        /// <param name="app">The parent app.</param>
        /// <param name="value">The input value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(App app, byte[] value) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<App, Entity> dataGetter) {
            dataBinding = new DataBinding<Entity>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        /// <param name="dataSetter"></param>
        public void AddDataBinding(Func<App, Entity> dataGetter, Action<App, Entity> dataSetter) {
            dataBinding = new DataBinding<Entity>(dataGetter, dataSetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public Entity GetBoundValue(App app) {
            return dataBinding.GetValue(app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="entity"></param>
        public void SetBoundValue(App app, Entity entity) {
            dataBinding.SetValue(app, entity);
        }
    }

}
