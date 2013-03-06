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

        private Puppet[] rootApps;
        private VMID nextVMId;
        private bool isInUse;
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
            rootApps = new Puppet[16]; // TODO: Configurable.

            nextVMId.Index = 0;
            nextVMId.Count = 1;
        }

        /// <summary>
        /// Attaches a root application to this session
        /// </summary>
        /// <param name="rootApp">The root app.</param>
        internal void AttachRootApp(Puppet rootApp) {
            Puppet existingApp;

            existingApp = rootApps[nextVMId.Index];
            if (existingApp != null)
                DisposeAppRecursively(existingApp);
            
            rootApps[nextVMId.Index] = rootApp;
            rootApp.ViewModelId = nextVMId++;
        }

        /// <summary>
        /// Gets an attached root application.
        /// </summary>
        /// <value></value>
        internal Puppet GetRootApp(int id) {
            Puppet rootApp;
            VMID existingId;
            VMID vmId = id;

            if (vmId.Index < 0 || vmId.Index >= rootApps.Length)
                throw new ArgumentOutOfRangeException("vmID");

            rootApp = rootApps[vmId.Index];
            existingId = rootApp.ViewModelId;

            if (vmId.Count != existingId.Count)
                throw new Exception("The session for the viewmodel has timed out.");

            var trans = rootApp.Transaction;
            if (trans != null)
                Transaction.SetCurrent(trans);

            return rootApp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        internal void Start(HttpRequest request) {
            Debug.Assert(current == null);
            Session.current = this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void End() {
            this.changeLog.Clear();
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
        }

        private void DisposeAppRecursively(Obj obj) {
            Puppet app = (Puppet)obj;
            if (app == null)
                return;

            if (app.TransactionOnThisApp != null) {
                app.TransactionOnThisApp.Dispose();
            }

            if (app.Template == null)
                return;

            foreach (Template child in app.Template.Children) {
                if (child is TPuppet) {
                    DisposeAppRecursively(app.Get((TPuppet)child));
                } else if (child is TObjArr) {
                    Arr listing = app.Get((TObjArr)child);
                    foreach (Puppet listApp in listing) {
                        DisposeAppRecursively(listApp);
                    }
                }
            }

        }
    }

    internal struct VMID {
        internal ushort Index;
        internal ushort Count;

        public static implicit operator VMID(int value) {
            VMID id;
            id.Index = (ushort)value;
            id.Count = (ushort)(value >> 16);
            return id;
        }

        public static implicit operator int(VMID value) {
            int id;
            id = value.Count << 16;
            id |= value.Index;
            return id;
        }

        public static VMID operator ++(VMID value) {
            value.Count++;
            value.Index++;
            if (value.Index == 16)
                value.Index = 0;
            return value;
        }
    }
}
