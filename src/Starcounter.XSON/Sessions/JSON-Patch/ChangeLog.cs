﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    public class ChangeLog {
        private List<Change> changes;
        private bool brandNew;
        private Json employer;
        private ViewModelVersion version;

        public ChangeLog(Json employer) {
            this.changes = new List<Change>();
            this.employer = employer;
            this.brandNew = true;
        }

        public ChangeLog(Json employer, ViewModelVersion version) : this(employer) {
            this.version = version;
        }

        [Conditional("DEBUG")]
        private void VerifyChange(Json json, TValue property) {

        }

        [Conditional("DEBUG")]
        private void VerifyChange(Change change) {

        }

        /// <summary>
        /// Adds an value update change.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        internal void UpdateValue(Json obj, TValue property) {
            VerifyChange(obj, property);

            changes.Add(Change.Update(obj, property));
            if (property != null)
                property.Checkpoint(obj);
        }

        /// <summary>
        /// Adds a list of changes to the log
        /// </summary>
        /// <param name="toAdd"></param>
        internal void Add(Change change) {
            VerifyChange(change);

            changes.Add(change);
        }

        internal List<Change> GetChanges() {
            return changes;
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        internal void Clear() {
            changes.Clear();
        }

        public ViewModelVersion Version {
            get { return version; }
        }

        /// <summary>
        /// Returns the number of changes in the log.
        /// </summary>
        /// <value></value>
        public Int32 Count { get { return changes.Count; } }

        /// <summary>
        /// Logs all changes since the last JSON-Patch update. This method generates the log
        /// for the dirty flags and the added/removed logs of the JSON tree in the session data.
        /// </summary>
        public void Generate() {
            if (version != null)
                version.LocalVersion++;

            if (brandNew) {
                changes.Add(Change.Add(employer));
                employer.CheckpointChangeLog();
                brandNew = false;
            } else {
                // TODO:
                // Since we dont want to have session here, this property should probably be moved 
                // somewhere else but since it currently always returns true we just ingore it for now.

//                if (DatabaseHasBeenUpdatedInCurrentTask) {
                    employer.LogValueChangesWithDatabase(this, true);
//                } else {
//                    employer.LogValueChangesWithoutDatabase(this, true);
//                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Checkpoint() {
            employer.CheckpointChangeLog();
            brandNew = false;
        }

        public bool BrandNew {
            get { return brandNew; }
        }

        internal void CleanupOldVersionLogs() {
            if (version != null)
                employer.CleanupOldVersionLogs(version, version.RemoteLocalVersion);
        }
    }

    public class ViewModelVersion {
        private List<Byte[]> patchQueue;
        private string localPropertyName;
        private string remotePropertyName;
        
        private long localVersion;
        private long remoteVersion;

        private long remoteLocalVersion;

        public ViewModelVersion() {
            localPropertyName = "_ver#s";
            remotePropertyName = "_ver#c$";
        }

        public ViewModelVersion(string localVersionPropertyName, string remoteVersionPropertyName) {
            localPropertyName = localVersionPropertyName;
            remotePropertyName = remoteVersionPropertyName;
        }

        public string LocalVersionPropertyName { 
            get { return localPropertyName; } 
        }

        public string RemoteVersionPropertyName { 
            get { return remotePropertyName; } 
        }

        public long LocalVersion {
            get { return localVersion; }
            set { localVersion = value; }
        }

        public long RemoteVersion {
            get { return remoteVersion; }
            set { remoteVersion = value; }
        }

        public long RemoteLocalVersion {
            get { return remoteLocalVersion; }
            set { remoteLocalVersion = value; }
        }

        internal void EnqueuePatch(byte[] patchArray, int index) {
            if (patchQueue == null)
                patchQueue = new List<byte[]>(8);

            for (int i = patchQueue.Count; i < index + 1; i++)
                patchQueue.Add(null);

            patchQueue[index] = patchArray;
        }

        internal byte[] GetNextEnqueuedPatch() {
            byte[] ret = null;
            if (patchQueue != null && patchQueue.Count > 0) {
                ret = patchQueue[0];
                patchQueue.RemoveAt(0);
            }
            return ret;
        }
    }

    internal struct ArrayVersionLog {
        internal long Version;
        internal List<Change> Changes;

        internal ArrayVersionLog(long version, List<Change> changes) {
            Version = version;
            Changes = changes;
        }
    }
}