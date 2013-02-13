﻿// ***********************************************************************
// <copyright file="App.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// TODO!
// - Get rid of references to Transaction, SQL and Entity



using System;
using Starcounter.Templates.Interfaces;
using System.ComponentModel;
using Starcounter.Templates;
using Starcounter.Internal.REST;
using Starcounter.Internal;
using Starcounter.Apps;
using Starcounter.Advanced;

#if CLIENT
using Starcounter.Client.Template;

namespace Starcounter.Client {
#else

namespace Starcounter {
#endif

    /// <summary>
    /// An App is a live view model object controlled by your C# application code.
    /// It is mirrored between the server and the client in an MVVM or MVC application.
    /// App objects can be used to drive MVVM views or other such model driven clients.
    /// </summary>
    /// <remarks>An App object is modelled to simulate a JSON object. The App object can have properties such as strings, booleans
    /// arrays and other app objects. In this way you can build JSON like trees. These trees are then bound to your GUI.
    /// Whenever you change properties in the tree, such as changing values or adding and removing elements in the arrays,
    /// the UI gets updated. Likewise, when the user clicks or writes text inside your UI, the App view model tree gets updated.
    /// This is a very efficient way to connect a user interface to your application logic and will result in clean, simple
    /// and easy to understand and maintain code. This model view controller pattern (MVC) pattern is sometimes referred to as
    /// MVVM (model view view-model) or MDV (model driven views).    An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).
    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).</remarks>
    public abstract partial class Obj : Container
#if IAPP
, IApp
#endif
 {
        /// <summary>
        /// 
        /// </summary>
        private IBindable _Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        public Obj() : base() {
            _cacheIndexInList = -1;
            ViewModelId = -1;
        }

        /// <summary>
        /// Rest-server responsible for delivering static resources.
        /// </summary>
        public static HttpRestServer StaticResources;

        /// <summary>
        /// Triggers the type initialization.
        /// </summary>
        static internal void TriggerTypeInitialization() {
            // Calling a static method will trigger type initialization.
            // This is important to detect if the EXE module is running out of process.
            // (so that it can be stopped and restarted inside the database process).
            // Called when the When class is initialized.
        }

        /// <summary>
        /// Returns true if this app have been serialed and sent to the client.
        /// </summary>
        /// <value>The is serialized.</value>
        public Boolean IsSerialized { get; internal set; }

        /// <summary>
        /// Returns the id of this app or -1 if not used.
        /// </summary>
        internal int ViewModelId { get; set; }

        /// <summary>
        /// Cache field of index if this apps parent is a list.
        /// </summary>
        internal Int32 _cacheIndexInList;

        /// <summary>
        /// Fills the index path.
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal override void FillIndexPath(int[] path, int pos) {
            if (Parent != null) {
                if (Parent is Listing) {
                    if (_cacheIndexInList == -1) {
                        _cacheIndexInList = ((Listing)Parent).IndexOf(this);
                    }
                    path[pos] = _cacheIndexInList;
                } else {
                    path[pos] = Template.Index;
                }
                Parent.FillIndexPath(path, pos - 1);
            }
        }

        /// <summary>
        /// Gets or sets the underlying entity object.
        /// </summary>
        /// <value>The data.</value>
        public IBindable Data {
            get {
                return (IBindable)_Data;
            }
            set {
                InternalSetData(value);
            }
        }

        /// <summary>
        /// Sets the underlying dataobject and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data"></param>
        internal virtual void InternalSetData(IBindable data) {

            _Data = data;

            if (Template.Bound) {
                Template.SetBoundValue((App)this.Parent, data);
            }

            RefreshAllBoundValues();
            OnData();
        }

        /// <summary>
        /// Refreshes all databound values for this app.
        /// </summary>
        private void RefreshAllBoundValues() {
            Template child;
            for (Int32 i = 0; i < this.Template.Properties.Count; i++) {
                child = Template.Properties[i];
                if (child.Bound) {
                    Refresh(child);
                }
            }
        }

        /// <summary>
        /// Called after the data object is set.
        /// </summary>
        protected virtual void OnData() {
        }

//        /// <summary>
//        /// Calls the init.
//        /// </summary>
//        internal void CallInit() {
//            Init();
//        }

//        public void Input( Input input ) {
//        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal App GetNearestAppParent() {
            Container parent = Parent;
            while ((parent != null) && (!(parent is App))) {
                parent = parent.Parent;
            }
            return (App)parent;
        }

        /// <summary>
        /// Refreshes the specified template.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Refresh(Template model) {
            if (model is ObjArrProperty) {
                ObjArrProperty apa = (ObjArrProperty)model;
                this.SetValue(apa, apa.GetBoundValue(this));
            } else if (model is AppTemplate) {
                AppTemplate at = (AppTemplate)model;

                // TODO:
                IBindable v = at.GetBoundValue(this);
                if (v != null)
                    this.SetValue(at, v);
            } else {
                Property p = model as Property;
                if (p != null)
                    ChangeLog.UpdateValue(this, p);
            }
        }

        /// <summary>
        /// The template defining the properties of this App.
        /// </summary>
        /// <value>The template.</value>
        public new AppTemplate Template {
            get { return (AppTemplate)base.Template; }
            set { base.Template = value; }
        }

        /// <summary>
        /// Implementation field used to cache the Properties property.
        /// </summary>
        private AppMetadata _Metadata = null;

        /// <summary>
        /// Here you can set properties for each property in this App (such as Editable, Visible and Enabled).
        /// The changes only affect this instance.
        /// If you which to change properties for the template, use the Template property instead.
        /// </summary>
        /// <value>The metadata.</value>
        /// <remarks>It is much less expensive to set this kind of metadata for the
        /// entire template (for example to mark a property for all App instances as Editable).</remarks>
        public AppMetadata Metadata {
            get {
                return _Metadata;
            }
        }

//        /// <summary>
//        /// Use this method to override the default communication from the client.
//        /// </summary>
//        /// <remarks>
//        /// Requests can use the WebSockets or HTTP protocol
//        /// </remarks>
//        /// <param name="request">Can be used to retrieve the data of the request</param>
//        /// <returns>The raw response</returns>
//        public virtual byte[] HandleRawRequest(HttpRequest request) {
//            return null;
//        }

        /// <summary>
        /// If the view lives in this .NET application domain, this property can be used to reference it.
        /// For Starcounter serverside App objects, this property is often a string that is used to identifify
        /// a specific view. For web applications, the string is often a reference to the .html file.
        /// </summary>
        /// <value>The media.</value>
        public Media Media { get; set; }

        /// <summary>
        /// Gets or sets the view.
        /// </summary>
        /// <value>The view.</value>
        public string View { get; set; }

#if !CLIENT
        /// <summary>
        /// For convenience, the static SQL function can be called from either the App class,
        /// the Entity class or the Db class. The implementations are identical.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="pars">The pars.</param>
        /// <returns>SqlResult.</returns>
        public static SqlResult SQL(string str, params object[] pars) {
            return Db.SQL(str, pars);
        }

//        /// <summary>
//        /// SQLs the specified STR.
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="str">The STR.</param>
//        /// <param name="pars">The pars.</param>
//        /// <returns>SqlResult2{``0}.</returns>
//        public static SqlResult2<T> SQL<T>(string str, params object[] pars) where T : Entity {
//            return null;
//        }

        ///// <summary>
        ///// Transactions the specified action.
        ///// </summary>
        ///// <param name="action">The action.</param>
        //public static void Transaction(Action action) {
        //    Db.Transaction(action);
        //}

        /// <summary>
        /// Slows the SQL.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="pars">The pars.</param>
        /// <returns>SqlResult.</returns>
        public static SqlResult SlowSQL(string str, params object[] pars) {
            return Db.SlowSQL(str, pars);
        }

#endif

        /// <summary>
        /// Deletes this instance.
        /// </summary>
        public void Delete() { }

        /// <summary>
        /// Removes this App from its parent.
        /// </summary>
        public void Close() { }

        /// <summary>
        /// Shows this instance.
        /// </summary>
        public void Show() {
        }
    }
}
