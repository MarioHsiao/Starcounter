﻿// ***********************************************************************
// <copyright file="AppTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Registers the specified name.
        /// </summary>
        /// <typeparam name="TTemplate">The type of the T template.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        /// <returns>``0.</returns>
        public TTemplate Register<TTemplate>(string name, bool editable = false)
            where TTemplate : Template, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable
            };
        }

        /// <summary>
        /// Registers the specified name.
        /// </summary>
        /// <typeparam name="TTemplate">The type of the T template.</typeparam>
        /// <typeparam name="TValue">The type of the T value.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        /// <returns>``0.</returns>
        public TTemplate Register<TTemplate, TValue>(
            string name,
            bool editable = false)
            where TTemplate : Property<TValue>, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TTemplate"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="name"></param>
        /// <param name="dataGetter"></param>
        /// <param name="editable"></param>
        /// <returns></returns>
        public TTemplate Register<TTemplate, TValue>(
            string name,
            Func<App, TValue> dataGetter,
            bool editable = false)
            where TTemplate : Property<TValue>, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
                GetBoundDataFunc = dataGetter,
                Bound = true
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TTemplate"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="name"></param>
        /// <param name="dataGetter"></param>
        /// <param name="dataSetter"></param>
        /// <param name="editable"></param>
        /// <returns></returns>
        public TTemplate Register<TTemplate, TValue>(
            string name,
            Func<App, TValue> dataGetter,
            Action<App, TValue> dataSetter,
            bool editable = false)
            where TTemplate : Property<TValue>, new() {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
                GetBoundDataFunc = dataGetter,
                SetBoundDataFunc = dataSetter,
                Bound = true
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
        /// <value>The namespace.</value>
        public string Namespace { get; set; }
        /// <summary>
        /// Gets or sets the include.
        /// </summary>
        /// <value>The include.</value>
        public string Include { get; set; }


        /// <summary>
        /// The _ property templates
        /// </summary>
        private PropertyList _PropertyTemplates;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppTemplate" /> class.
        /// </summary>
        public AppTemplate() {
            _PropertyTemplates = new PropertyList(this);
        }

        /// <summary>
        /// The _ app type
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
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns>``0.</returns>
        public T Add<T>(string name) where T : ITemplate, new() {
            T t = new T() { Name = name };
            Properties.Add(t);
            return t;
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <returns>``0.</returns>
        public T Add<T>(string name, IAppTemplate type) where T : IAppListTemplate, new() {
            T t = new T() { Name = name, Type = type };
            Properties.Add(t);
            return t;
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        public PropertyList Properties { get { return _PropertyTemplates; } }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerable<Template> Children {
            get { return (IEnumerable<Template>)Properties; }
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(AppNode parent) {
            return new App() { Template = this, Parent = parent };
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns>``0.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        T IAppTemplate.Add<T>(string name) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <returns>``0.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        T IAppTemplate.Add<T>(string name, IAppTemplate type) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        IPropertyTemplates IAppTemplate.Properties {
            get { return Properties; }
        }

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(App app, byte[] value) {
            throw new NotImplementedException();
        }
    }

}
