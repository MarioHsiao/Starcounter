// ***********************************************************************
// <copyright file="ArrProperty.cs" company="Starcounter AB">
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
    /// Class ArrProperty
    /// </summary>
    /// <typeparam name="OT">The type of the app type.</typeparam>
    /// <typeparam name="OTT">The type of the app template type.</typeparam>
    public class ArrProperty<OT,OTT> : ObjArrProperty
        where OT : App, new()
        where OTT : AppTemplate
    {
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(Container parent) {
            return new Listing<OT>((App)parent, this);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override System.Type InstanceType {
            get { return typeof(Listing<OT>); }
        }

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(App app, byte[] rawValue) {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public override ObjTemplate App {
            get {
                if (_Single.Length == 0)
                    return null;
                return (ObjTemplate)_Single[0];
            }
            set {
                _Single = new OTT[1];
                _Single[0] = (OTT)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal OTT[] _Single = new OTT[0];


        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }


    }

    /// <summary>
    /// Class ArrProperty
    /// </summary>
    public abstract class ObjArrProperty : ListTemplate
#if IAPP
        , IAppListTemplate
#endif
    {

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
        /// Gets or sets the type (the template) that should be the template for all elements
        /// in this array.
        /// </summary>
        /// <value>The obj template adhering to each element in this array</value>
        public abstract ObjTemplate App { get; set; }

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

    }

}
