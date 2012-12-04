 ﻿// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics; // TODO: Remove when HttpRequest can be sent in to the handler. And remove reference from Apps
using HttpStructs;

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

        private App rootApp;
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
            rootApp = null;
        }

        /// <summary>
        /// Attaches a root application to this session
        /// </summary>
        /// <param name="rootApp">The root app.</param>
        internal void AttachRootApp(App rootApp) {
            this.rootApp = rootApp;
            rootApp.ViewModelId = 1;
        }

        /// <summary>
        /// Gets an attached root application.
        /// </summary>
        /// <value></value>
        internal App GetRootApp(int index) {
            if (rootApp != null) {
                var trans = rootApp.Transaction;
                if (trans != null)
                    Transaction.SetCurrent(trans);
            }
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
            throw new NotImplementedException();
        }
    }
}
