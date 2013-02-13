// ***********************************************************************
// <copyright file="ListingProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

//    public class SetProperty<AppType, SchemaType> : AppListTemplate<AppType> where AppType : App, new() where SchemaType : AppTemplate {
//    }

    /// <summary>
    /// Class ListingProperty
    /// </summary>
    /// <typeparam name="AppType">The type of the app type.</typeparam>
    /// <typeparam name="AppTemplateType">The type of the app template type.</typeparam>
    public class ListingProperty<AppType,AppTemplateType> : ArrProperty
        where AppType : App, new()
        where AppTemplateType : AppTemplate
    {
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(Container parent) {
            return new Listing<AppType>((App)parent, this);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override System.Type InstanceType {
            get { return typeof(Listing<AppType>); }
        }

        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public new AppTemplateType App {
            get {
                return (AppTemplateType)(base.App);
            }
            set {
                base.App = value;
            }
        }

    }

    /// <summary>
    /// Class ListingProperty
    /// </summary>
    public class ArrProperty : ListTemplate
#if IAPP
        , IAppListTemplate
#endif
    {
        /// <summary>
        /// 
        /// </summary>
        internal AppTemplate[] _Single = new AppTemplate[0];

        private DataBinding<SqlResult> dataBinding;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<Obj, SqlResult> dataGetter) {
            dataBinding = new DataBinding<SqlResult>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public SqlResult GetBoundValue(Obj app) {
            return dataBinding.GetValue(app);
        }
        
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        IAppTemplate IAppListTemplate.Type {
            get {
                return App;
            }
            set {
                App = (AppTemplate)value;
            }
        }

        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public AppTemplate App {
            get {
                if (_Single.Length == 0)
                    return null;
                return (AppTemplate)_Single[0];
            }
            set {
                _Single = new AppTemplate[1];
                 _Single[0] = (AppTemplate)value;
            }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override object DefaultValueAsObject {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance( Container parent ) {
            return new Listing( (App)parent, this );
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override System.Type InstanceType {
            get { return typeof(Listing); }
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(App app, byte[] rawValue)
        {
            throw new System.NotImplementedException();
        }
    }

}
