 ﻿// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics; 
using Starcounter.Templates;
using Starcounter.Advanced;
using HttpStructs;
using Starcounter.Internal;
using System.Text;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Class Session
    /// </summary>
    public class Session : IAppsSession {
        [ThreadStatic]
        static Session current;

        [ThreadStatic]
        static Request request;

        internal Obj root;

        bool isInUse;

        ChangeLog changeLog;

        /// <summary>
        /// Cached pages dictionary.
        /// </summary>
        Dictionary<String, Obj> JsonNodeCacheDict;

        /// <summary>
        /// Tries to get cached JSON node.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        internal Obj GetCachedJsonNode(String uri)
        {
            Obj obj;
            if (!JsonNodeCacheDict.TryGetValue(uri, out obj))
                return null;

            Debug.Assert(null != obj);

            // Checking if its a root.
            if (root == obj)
                return root;

            // Checking if node has no parent, indicating that it was removed from tree.
            if (null != obj.Parent)
                return obj;

            return null;
        }

        /// <summary>
        /// Adds JSON node to cache.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="obj"></param>
        internal void AddJsonNodeToCache(String uri, Obj obj)
        {
            // Checking if cached state dictionary is already created.
            if (null == JsonNodeCacheDict)
                JsonNodeCacheDict = new Dictionary<String, Obj>();

            // Adding current URI to cache.
            JsonNodeCacheDict[uri] = obj;
        }

        /// <summary>
        /// Destroy session delegate.
        /// </summary>
        internal Action<Session> destroy_user_delegate_;

        /// <summary>
        /// Returns the current active session.
        /// </summary>
        /// <value></value>
        public static Session Current { get { return current; } }

        /// <summary>
        /// Returns the original request for session.
        /// </summary>
        /// <value></value>
        internal static Request InitialRequest {
            get { return request; }
            set { request = value; }
        }

        /// <summary>
        /// Getting internal session.
        /// </summary>
        public ScSessionClass InternalSession { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        internal Session() {
            changeLog = new ChangeLog();
        }

        /// <summary>
        /// Creates new empty session.
        /// </summary>
        /// <returns></returns>
        internal static Session CreateNewEmptySession()
        {
            Debug.Assert(current == null);

            current = new Session();
            ChangeLog.CurrentOnThread = current.changeLog;

            return current;
        }

        /// <summary>
        /// Sets session data.
        /// </summary>
        public static Obj Data {
            get {
                Session s = current;
                if (s != null && s.root != null) {
                    // TODO: 
                    // Better handling of transactions in jsonobjects.
                    ITransaction t = s.root.Transaction2;
                    if (t != null)
                        StarcounterBase._DB.SetCurrentTransaction(t);

                    return s.root;
                }
                return null;
            }
            set {
                if (current == null) {

                    // Creating new empty session.
                    current = new Session();

                    // Creating session on Request as well.
                    UInt32 errCode = request.GenerateNewSession(current);

                    if (errCode != 0)
                        throw ErrorCode.ToException(errCode);
                }
                current.SetData(value);
            }
        }

        /// <summary>
        /// Setting data object.
        /// </summary>
        /// <param name="data"></param>
        private void SetData(Obj data) {
            // TODO:
            // Do we allow setting a new dataobject if an old one exists?
            if (root != null) {
                DisposeJsonRecursively(current.root);
            }
            root = data;

            // We don't want any changes logged during this request since
            // we will have to send the whole object anyway in the response.
            ChangeLog.CurrentOnThread = null;
        }

        /// <summary>
        /// Internal session string.
        /// </summary>
        public String SessionIdString
        {
            get { return InternalSession.ToAsciiString(); }
        }

        /// <summary>
        /// Get complete resource locator.
        /// </summary>
        /// <returns></returns>
        internal string GetDataLocation()
        {
            if (root == null)
                return null;

            return ScSessionClass.DataLocationUriPrefix + SessionIdString;
        }

        /// <summary>
        /// Pushes data on existing session.
        /// </summary>
        /// <param name="data"></param>
        public void Push(String data)
        {
            Push(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Pushes data on existing session.
        /// </summary>
        /// <param name="data"></param>
        public void Push(Byte[] data)
        {
            // TODO
            if (data.Length > 3000)
                throw new ArgumentException("Current WebSockets implementation supports messages only up to 3000 bytes.");

            Request req = GatewayHandlers.GenerateNewRequest(
                InternalSession, MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS);

            req.SendResponse(data, 0, data.Length);
        }

        /// <summary>
        /// Start usage of given session.
        /// </summary>
        /// <param name="session"></param>
        internal static void Start(Session session)
        {
            Debug.Assert(current == null);

            // Session still can be null, e.g. did not pass the verification.
            if (session == null)
                return;

            Session.current = session;
            ChangeLog.CurrentOnThread = session.changeLog;
        }

        /// <summary>
        /// Finish usage of current session.
        /// </summary>
        internal static void End()
        {
            current.changeLog.Clear();
            Session.current = null;
            ChangeLog.CurrentOnThread = null;
            if (StarcounterBase._DB.GetCurrentTransaction() != null)
                StarcounterBase._DB.SetCurrentTransaction(null);
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
        public void SetDestroyCallback(Action<Session> destroy_user_delegate)
        {
            destroy_user_delegate_ = destroy_user_delegate;
        }

        /// <summary>
        /// Gets destroy callback if it was supplied before.
        /// </summary>
        /// <returns></returns>
        public Action<Session> GetDestroyCallback()
        {
            return destroy_user_delegate_;
        }

        /// <summary>
        /// Destroys the session.
        /// </summary>
        public void Destroy()
        {
            if (root != null) {
                DisposeJsonRecursively(root);
            }
            root = null;

            // Checking if destroy callback is supplied.
            if (null != destroy_user_delegate_)
            {
                destroy_user_delegate_(this);
                destroy_user_delegate_ = null;
            }

            changeLog = null;
            Session.current = null;
        }

        private void DisposeJsonRecursively(Obj json) {
            if (json == null)
                return;

            //if (json.TransactionOnThisApp != null) {
            //    json.TransactionOnThisApp.Dispose();
            //}

            if (json.Template == null)
                return;

            foreach (Template child in json.Template.Children) {
                if (child is TObj) {
                    DisposeJsonRecursively(json.Get((TObj)child));
                } else if (child is TObjArr) {
                    Arr listing = json.Get((TObjArr)child);
                    foreach (Obj listApp in listing) {
                        DisposeJsonRecursively(listApp);
                    }
                }
            }

        }
    }
}
