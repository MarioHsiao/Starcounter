using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    public class ChangeLog {
        private List<Change> changes;
        private bool brandNew;
        private Json employer;
        private ViewModelVersion version;
        private bool isGeneratingChanges;

        public ChangeLog(Json employer) {
            this.changes = new List<Change>();
            this.employer = employer;
            this.brandNew = true;
            this.isGeneratingChanges = false;
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

        internal Json Employer {
            get { return employer; }
        }

        internal void ChangeEmployer(Json newEmployer) {
            this.employer.ChangeLog = null;
            this.employer = newEmployer;
            newEmployer.ChangeLog = this;
            this.brandNew = true;
        }

        /// <summary>
        /// Adds an value update change.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        internal void UpdateValue(Json obj, TValue property) {
            VerifyChange(obj, property);
            changes.Add(Change.Update(obj, property));
        }

        /// <summary>
        /// Adds a list of changes to the log
        /// </summary>
        /// <param name="toAdd"></param>
        internal void Add(Change change) {
            VerifyChange(change);
            changes.Add(change);
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
        /// Logs all changes since the last JSON-Patch update. This method generates the log
        /// for the dirty flags and the added/removed logs of the JSON tree in the session data.
        /// If called recursively, the method will return false and not collect any changes.
        /// </summary>
        public bool Generate(bool flushLog, out Change[] changeArr) {
            if (this.isGeneratingChanges) {
                changeArr = null;
                return false;
            }

            try {
                this.isGeneratingChanges = true;
                if (version != null)
                    version.LocalVersion++;
                    
                if (brandNew) {
                    changes.Add(Change.Add(employer));
                } else {
                    employer.GatherChanges(this, true);
                }

                changeArr = changes.ToArray();
                if (flushLog) 
                    this.Checkpoint();
                return true;
            } finally {
                this.isGeneratingChanges = false;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Checkpoint() {
            changes.Clear();
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
            internal set { localVersion = value; }
        }

        public long RemoteVersion {
            get { return remoteVersion; }
            internal set { remoteVersion = value; }
        }

        public long RemoteLocalVersion {
            get { return remoteLocalVersion; }
            internal set { remoteLocalVersion = value; }
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
