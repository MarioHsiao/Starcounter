 ﻿// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics; 
using Starcounter.Templates;
using Starcounter.Advanced;
using Starcounter.Internal;
using System.Text;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Class Session
    /// </summary>
    public partial class Session : IAppsSession {
        /// <summary>
        /// Current static JSON object.
        /// </summary>
        [ThreadStatic]
        internal static Session _Current;

        /// <summary>
        /// Current static Request object.
        /// </summary>
        [ThreadStatic]
        static Request _Request;

        /// <summary>
        /// Attached Json object.
        /// </summary>
        internal Json _Data;

        /// <summary>
        /// Indicates if session is being used.
        /// </summary>
        bool _IsInUse;

        /// <summary>
        /// Cached pages dictionary.
        /// </summary>
        Dictionary<String, Json> _JsonNodeCacheDict;

        /// <summary>
        /// Destroy session delegate.
        /// </summary>
        internal Action<Session> _SessionDestroyUserDelegate;

        /// <summary>
        /// Runs a task asynchronously on a given scheduler.
        /// </summary>
        public void RunAsync(Action action, Byte schedId = Byte.MaxValue)
        {
            InternalSession.RunAsync(action, schedId);
        }

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        public void RunSync(Action action, Byte schedId = Byte.MaxValue)
        {
            InternalSession.RunSync(action, schedId);
        }

        /// <summary>
        /// Running the given action on each active session.
        /// </summary>
        /// <param name="action">The user procedure to be performed on each session.</param>
        public static void ForEach(Action<Session> action) {
            ForEach(UInt64.MaxValue, action);
        }

        /// <summary>
        /// Running the given action on each active session.
        /// </summary>
        /// <param name="action">The user procedure to be performed on each session.</param>
        /// <param name="cargoId">Cargo ID filter.</param>
        public static void ForEach(UInt64 cargoId, Action<Session> action) {

            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                Byte schedId = i;

                ScSessionClass.DbSession.RunAsync(() => 
                {
                    // Saving current session since we are going to set other.
                    Session origCurrentSession = Session.Current;

                    try
                    {
                        SchedulerSessions ss = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(schedId);

                        LinkedListNode<UInt32> used_session_index_node = ss.UsedSessionIndexes.First;
                        while (used_session_index_node != null) {
                            LinkedListNode<UInt32> next_used_session_index_node = used_session_index_node.Next;

                            // Getting session instance.
                            ScSessionClass s = ss.GetAppsSessionIfAlive(used_session_index_node.Value);

                            // Checking if session is created at all.
                            if (s != null) {

                                // Checking if cargo ID is correct.
                                if ((cargoId == UInt64.MaxValue) || (cargoId == s.CargoId)) {

                                    Session session = (Session)s.apps_session_int_;

                                    // Setting new current session.
                                    Session.Current = session;

                                    // Running user delegate with session as parameter.
                                    action(session);
                                }
                            }

                            // Getting next used session.
                            used_session_index_node = next_used_session_index_node;
                        }
                    }
                    finally {
                        // Restoring original current session.
                        Session.Current = origCurrentSession;
                    }

                }, schedId);
            }
        }

        /// <summary>
        /// Tries to get cached JSON node.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        internal Json GetCachedJsonNode(String uri)
        {
            Json obj;

            if ((_JsonNodeCacheDict == null) || (!_JsonNodeCacheDict.TryGetValue(uri, out obj)))
                return null;

            Debug.Assert(null != obj);

            // Checking if its a root.
            if (_Data == obj)
                return _Data;

            // Checking if node has no parent, indicating that it was removed from tree.
            // We need to check all the way up to the root, since a parent might have been removed 
            // further up.
            if (obj.HasThisRoot(_Data))
                return obj;

            return null;
        }

        /// <summary>
        /// Adds JSON node to cache.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="obj"></param>
        internal void AddJsonNodeToCache(String uri, Json obj) {
            // Checking if cached state dictionary is already created.
            if (null == _JsonNodeCacheDict)
                _JsonNodeCacheDict = new Dictionary<String, Json>();

            // Adding current URI to cache.
            _JsonNodeCacheDict[uri] = obj;
        }

        /// <summary>
        /// Removes URI entry from cache.
        /// </summary>
        /// <param name="uri">URI entry.</param>
        /// <returns>True if URI entry is removed.</returns>
        internal Boolean RemoveUriFromCache(String uri) {
            // Checking if cached state dictionary is already created.
            if (null == _JsonNodeCacheDict)
                return false;

            // Adding current URI to cache.
            if (_JsonNodeCacheDict.ContainsKey(uri)) {
                _JsonNodeCacheDict[uri] = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates if user wants to use session cookie.
        /// </summary>
        public Boolean UseSessionCookie {
            get { return InternalSession.use_session_cookie_; }
            set { InternalSession.use_session_cookie_ = value; }
        }

        /// <summary>
        /// Returns the original request for session.
        /// </summary>
        /// <value></value>
        public static Request InitialRequest {
            get { return _Request; }
            set { _Request = value; }
        }

        /// <summary>
        /// Getting internal session.
        /// </summary>
        public ScSessionClass InternalSession { get; set; }

        /// <summary>
        /// Current static session object.
        /// </summary>
        public static Session Current
        {
            get {
                return _Current;
            }

            set {
                // Creating new empty session.
                _Current = value;

            }
        }

        /// <summary>
        /// Sets session data.
        /// </summary>
        public Json Data {
            get {
                return _Data;
            }

            set {
                _Data = value;
                if (value != null) {
                    value._Session = this;
                }

                // Setting current session.
                Current = this;
            }
        }

        /// <summary>
        /// Specific saved user object ID.
        /// </summary>
        public UInt64 CargoId
        {
            get
            {
                return InternalSession.CargoId;
            }

            set
            {
                InternalSession.CargoId = value;
            }
        }

        /// <summary>
        /// Getting session creation time. 
        /// </summary>
        public DateTime Created
        {
            get {
                return InternalSession.Created;
            }
        }

        /// <summary>
        /// Getting last active session time. 
        /// </summary>
        public DateTime LastActive
        {
            get {
                return InternalSession.LastActive;
            }
        }

        /// <summary>
        /// Session timeout.
        /// </summary>
        public UInt64 TimeoutMinutes
        {
            get {
                return InternalSession.TimeoutMinutes;
            }
            set {
                InternalSession.TimeoutMinutes = value;
            }
        }

        /// <summary>
        /// Internal session string.
        /// </summary>
        public String SessionIdString
        {
            get { return InternalSession.ToAsciiString(); }
        }

        // Last active WebSocket connection.
        public WebSocket ActiveWebsocket {
            get;
            internal set;
        }

        /// <summary>
        /// Start usage of given session.
        /// </summary>
        /// <param name="session"></param>
        internal static void Start(Session session)
        {
            Debug.Assert(_Current == null);

            // Session still can be null, e.g. did not pass the verification.
            if (session == null)
                return;

            Session._Current = session;
        }

        /// <summary>
        /// Finish usage of current session.
        /// </summary>
        internal static void End()
        {
			if (_Current != null) {
				_Current.Clear();
				Session._Current = null;
			}
        }

        /// <summary>
        /// Executes the specified action inside the Session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        internal static void Execute(Session session, Action action) {
            try {
                Start(session);
                action();
            } finally {
                End();
            }
        }

        /// <summary>
        /// Returns True if session is being used now.
        /// </summary>
        /// <returns></returns>
        public bool IsBeingUsed() {
            return _IsInUse;
        }

        /// <summary>
        /// Start using specific session.
        /// </summary>
        public void StartUsing() {
            _IsInUse = true;
        }

        /// <summary>
        /// Stop using specific session.
        /// </summary>
        public void StopUsing() {
            _IsInUse = false;
        }

        /// <summary>
        /// Checks if session is active.
        /// </summary>
        /// <returns></returns>
        public Boolean IsAlive()
        {
            return (InternalSession != null) && (InternalSession.IsAlive());
        }

        /// <summary>
        /// Set user destroy callback.  
        /// </summary>
        /// <param name="destroy_user_delegate"></param>
        public void SetSessionDestroyCallback(Action<Session> userDestroyMethod)
        {
            _SessionDestroyUserDelegate = userDestroyMethod;
        }

        /// <summary>
        /// Gets destroy callback if it was supplied before.
        /// </summary>
        /// <returns></returns>
        public Action<Session> GetDestroyCallback()
        {
            return _SessionDestroyUserDelegate;
        }

        /// <summary>
        /// Destroys the session.
        /// </summary>
        public void Destroy()
        {
            if (_Data != null) {
                DisposeJsonRecursively(_Data);
                _Data = null;
            }

            if (InternalSession != null) {
                
                // NOTE: Preventing recursive destroy call.
                InternalSession.apps_session_int_ = null;

                InternalSession.Destroy();
                InternalSession = null;
            }

            // Checking if destroy callback is supplied.
            if (null != _SessionDestroyUserDelegate)
            {
                _SessionDestroyUserDelegate(this);
                _SessionDestroyUserDelegate = null;
            }

            Session._Current = null;
        }

        /// <summary>
        /// Destroys Json tree recursively, including session.
        /// </summary>
        /// <param name="json"></param>
        private void DisposeJsonRecursively(Json json) {
            if (json == null)
                return;

            // Disposing transaction if it exists on this node.
            if (json.TransactionOnThisNode != null) {
                json.TransactionOnThisNode.Dispose();
            }

            if (json.Template == null || json.Template.IsPrimitive)
                return;

            foreach (Template child in ((TContainer)json.Template).Children) {
                if (child is TObject) {
                    DisposeJsonRecursively(((TObject)child).Getter(json));
                } else if (child is TObjArr) {
                    Json listing = ((TObjArr)child).Getter(json);
                    foreach (Json listApp in listing) {
                        DisposeJsonRecursively(listApp);
                    }
                }
            }
        }
    }
}
