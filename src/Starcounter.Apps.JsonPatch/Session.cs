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

namespace Starcounter {
    /// <summary>
    /// Class Session
    /// </summary>
    public class Session : IAppsSession {
        [ThreadStatic]
        private static Session current;

        private static string dataLocationUri = "/__" + Db.Environment.DatabaseName.ToLower() + "/";

        internal Obj root;

        private bool isInUse;

        private ChangeLog changeLog;

        /// <summary>
        /// Returns the current active session.
        /// </summary>
        /// <value></value>
        public static Session Current { get { return current; } }

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
        public static Session CreateNewEmptySession()
        {
            Debug.Assert(current == null);

            current = new Session();
            ChangeLog.CurrentOnThread = current.changeLog;

            return current;
        }

        /// <summary>
        /// 
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
                    current = new Session();
                }
                current.SetData(value);
            }
        }

        private void SetData(Obj data) {
            // TODO:
            // Do we allow setting a new dataobject if an old one exists?
            if (root != null) {
                DisposeJsonRecursively(current.root);
            }
            root = data;

            // We don't want any changes logged during this request since
            // we will have to send the whole object anyway in the response.
            root.LogChanges = false;
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
        /// 
        /// </summary>
        /// <returns></returns>
        internal string GetDataLocation() 
        {
            if (root == null)
                return null;

            return dataLocationUri + SessionIdString;
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
            Request req = GatewayHandlers.GenerateNewRequest(
                InternalSession, MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS);

            req.SendResponse(data, 0, data.Length);
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        internal static void End()
        {
            if (current != null)
            {
                current.changeLog.Clear();
                Session.current = null;
                ChangeLog.CurrentOnThread = null;
                if (StarcounterBase._DB.GetCurrentTransaction() != null)
                    StarcounterBase._DB.SetCurrentTransaction(null);
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
        /// Checks if session is active.
        /// </summary>
        /// <returns></returns>
        public Boolean IsAlive()
        {
            return (InternalSession != null) && (InternalSession.IsAlive());
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
