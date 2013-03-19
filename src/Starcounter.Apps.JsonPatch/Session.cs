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
        /// Returns true if the dataobject have been sent to a client in a previous
        /// request.
        /// </summary>
        internal Boolean IsSentExternally { get; set; }

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
        }

        /// <summary>
        /// 
        /// </summary>
        public static Obj Data {
            get {
                if (current != null)
                    return current.root;
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
            IsSentExternally = false;

            // We don't want any changes logged during this request since
            // we will have to send the whole object anyway in the response.
            ChangeLog.CurrentOnThread = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal string GetDataLocation() {
            if (root == null)
                return null;
            return dataLocationUri + "12345"; // TODO: Proper id.
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        internal static void Start(Session session) {
            Debug.Assert(current == null);
            Session.current = session;
            ChangeLog.CurrentOnThread = session.changeLog;
        }

        /// <summary>
        /// 
        /// </summary>
        internal static void End() {
            var s = Current;
            if (s != null) {
                s.changeLog.Clear();
                Session.current = null;
                ChangeLog.CurrentOnThread = null;
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
        /// 
        /// </summary>
        public void Destroy() {
            if (root != null) {
                DisposeJsonRecursively(root);
            }
            root = null;
            changeLog = null;
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
