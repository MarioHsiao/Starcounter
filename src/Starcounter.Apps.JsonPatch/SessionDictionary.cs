// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpStructs;

namespace Starcounter.Internal.Application {
    /// <summary>
    /// Class SessionDictionary
    /// </summary>
    public class SessionDictionary {

        /// <summary>
        /// The hardcoded
        /// </summary>
        private static Session Hardcoded;
        //        private Dictionary<SessionID, Session> Sessions = new Dictionary<SessionID,Session>();

        //public App GetRootApp(SessionID session)
        //{
        //    return RootApps[session];
        //}

        /// <summary>
        /// Creates the session.
        /// </summary>
        /// <returns>Session.</returns>
        internal Session CreateSession() {
            //            var sid = SessionID.CreateSession();
            var session = new Session(1);
            Hardcoded = session;
            //            Sessions[sid] = session;
            return session;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Session.</returns>
        internal Session GetSession(Int32 id) {
            //Session session;
            //Sessions.TryGetValue(sid, out session);
            //return session;
            if (Hardcoded == null) {
                CreateSession();
            }

            return Hardcoded;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="sessionString">The session string.</param>
        /// <returns>Session.</returns>
        public Session GetSession(string sessionString) {
            // TODO:
            // How do we convert string to SessionID???
            //SessionID sid = SessionID.NullSession;
            //return GetSession(sid);
            return Hardcoded;
        }
    }

    /// <summary>
    /// Class Session
    /// </summary>
    public class Session {
        /// <summary>
        /// The _current
        /// </summary>
        [ThreadStatic]
        private static Session _current;

        /// <summary>
        /// The _change log
        /// </summary>
        internal ChangeLog _changeLog;
        /// <summary>
        /// The _session ID
        /// </summary>
        private Int32 _sessionID;
        /// <summary>
        /// The _root app
        /// </summary>
        private App _rootApp;

        /// <summary>
        /// The _request
        /// </summary>
        private HttpRequest _request;

        /// <summary>
        /// Gets the current.
        /// </summary>
        /// <value>The current.</value>
        public static Session Current { get { return _current; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        internal Session()
            : this(1) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        /// <param name="sid">The sid.</param>
        internal Session(Int32 sid) {
            _changeLog = new ChangeLog();
            _sessionID = sid;
        }

        /// <summary>
        /// Attaches the root app.
        /// </summary>
        /// <param name="rootApp">The root app.</param>
        internal void AttachRootApp(App rootApp) {
            _rootApp = rootApp;
        }

        /// <summary>
        /// Gets the root app.
        /// </summary>
        /// <value>The root app.</value>
        public App RootApp {
            get { return _rootApp; }
        }

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        /// <value>The HTTP request.</value>
        public HttpRequest HttpRequest {
            get { return _request; }
        }

        /// <summary>
        /// Executes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="action">The action.</param>
        internal void Execute(HttpRequest request, Action action) {
            LongRunningTransaction transaction = null;

            try {
                _request = request;
                _current = this;
                ChangeLog.BeginRequest(_changeLog);

                transaction = (RootApp != null) ? RootApp.GetAttachedTransaction() : null;
                if (transaction != null && transaction != LongRunningTransaction.Current)
                    transaction.SetTransactionAsCurrent();

                action();

            } finally {
                ChangeLog.EndRequest();
                _current = null;
                _request = null;
                if (transaction != null)
                    transaction.ReleaseCurrentTransaction();
            }
        }
    }
}
