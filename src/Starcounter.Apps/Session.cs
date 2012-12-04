// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpStructs; // TODO: Remove when HttpRequest can be sent in to the handler. And remove reference from Apps

namespace Starcounter.Apps {
    // TODO:
    // HttpRequest should not be stored inside the session. Better to be able to
    // specify that you want it in the handler and remove it here.

    /// <summary>
    /// Class Session
    /// </summary>
    public class Session {
        private const int maxRootApps = 32;

        [ThreadStatic]
        private static Session current;

        private App[] rootApps;
        private int rootAppCount;
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
            rootAppCount = 0;
            rootApps = new App[maxRootApps];
        }

        /// <summary>
        /// Attaches a root application to this session
        /// </summary>
        /// <param name="rootApp">The root app.</param>
        internal int AttachRootApp(App rootApp) {
            int index;

            if (rootAppCount >= maxRootApps) {
                throw new Exception("TODO: Errorcode, maximum number of rootapps reached.");
            }

            index = rootAppCount++;
            rootApps[index] = rootApp;
            return index;
        }

        /// <summary>
        /// Gets an attached root application.
        /// </summary>
        /// <value></value>
        internal App GetRootApp(int index) {
            App rootApp;

            if (index > rootAppCount) {
                throw new ArgumentOutOfRangeException("index");
            }

            // TODO:
            // Proper implementation of App transactions.
            rootApp = rootApps[index];
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
        /// Executes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="action">The action.</param>
        internal void Execute(HttpRequest request, Action action) {
            try {
                this.request = request;
                Session.current = this;
                action();
            } finally {
                changeLog.Clear();
                current = null;
                request = null;
            }
        }

    }
}
