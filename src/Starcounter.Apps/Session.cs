 ﻿// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics; // TODO: Remove when HttpRequest can be sent in to the handler. And remove reference from Apps
using HttpStructs;
using Starcounter.Templates;

namespace Starcounter.Apps {
    // TODO:
    // HttpRequest should not be stored inside the session. Better to be able to
    // specify that you want it in the handler and remove it here.

    /// <summary>
    /// Class Session
    /// </summary>
    public class Session : IAppsSession {
        [ThreadStatic]
        private static Session current;

        private App[] rootApps;
        private int nextVMId;
        private bool isInUse;
        private HttpRequest request; // TODO: Remove when it can be sent in to the handler.
        internal ChangeLog changeLog;

        /// <summary>
        /// Returns the current active session.
        /// </summary>
        /// <value></value>
        public static Session Current { get { return current; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        internal Session() {
            changeLog = new ChangeLog();
            rootApps = new App[16]; // TODO: Configurable.
            nextVMId = 0;
        }

        /// <summary>
        /// Attaches a root application to this session
        /// </summary>
        /// <param name="rootApp">The root app.</param>
        internal void AttachRootApp(App rootApp) {
            App existingApp;

            if (nextVMId == rootApps.Length)
                nextVMId = 0;

            existingApp = rootApps[nextVMId];
            if (existingApp != null)
                DisposeAppRecursively(existingApp);
            
            rootApps[nextVMId] = rootApp;
            rootApp.ViewModelId = nextVMId++;
        }

        /// <summary>
        /// Gets an attached root application.
        /// </summary>
        /// <value></value>
        internal App GetRootApp(int index) {
            App rootApp;

            if (index < 0 || index >= rootApps.Length)
                throw new ArgumentOutOfRangeException("index");

            rootApp = rootApps[index];
            var trans = rootApp.Transaction;
            if (trans != null)
                Transaction.SetCurrent(trans);

            return rootApp;
        }

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        /// <value>The HTTP request.</value>
        public HttpRequest HttpRequest { // TODO: Remove when it can be sent in to the handler.
            get { return request; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        internal void Start(HttpRequest request) {
            Debug.Assert(current == null);

            this.request = request;
            Session.current = this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void End() {
            this.changeLog.Clear();
            this.request = null;
            Session.current = null;
        }

        /// <summary>
        /// Executes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="action">The action.</param>
        internal void Execute(HttpRequest request, Action action) {
            try {
                Start(request);
                action();
            } finally {
                End();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsBeingUsed() {
            return isInUse;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartUsing() {
            isInUse = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopUsing() {
            isInUse = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Destroy() {
            for (int i = 0; i < rootApps.Length; i++) {
                DisposeAppRecursively(rootApps[i]);
            }
            rootApps = null;
            changeLog = null;
            request = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        private void DisposeAppRecursively(App app) {
            if (app == null)
                return;

            if (app.TransactionOnThisApp != null) {
                app.TransactionOnThisApp.Dispose();
            }

            if (app.Template == null)
                return;

            foreach (Template child in app.Template.Children) {
                if (child is AppTemplate) {
                    DisposeAppRecursively(app.GetValue((AppTemplate)child));
                } else if (child is ListingProperty) {
                    Listing listing = app.GetValue((ListingProperty)child);
                    foreach (App listApp in listing) {
                        DisposeAppRecursively(listApp);
                    }
                }
            }

        }

    }
}
