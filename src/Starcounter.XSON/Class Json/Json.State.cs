using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Logging;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter {
    partial class Json {
        [Flags]
        protected enum PropertyState : byte {
            Default = 0,
            /// <summary>
            /// The property is considered dirty and value should be sent to client.
            /// </summary>
            Dirty = 1,
            /// <summary>
            /// Set for bound properties. The bound value is cached and should be read from
            /// the cache. Used to avoid changes in bound values while gathering changes and 
            /// serializing since these parts are done in steps.
            /// </summary>
            Cached = 2
        }

        private static LogSource logSource = new LogSource("Starcounter.XSON");

        /// <summary>
        /// Backing field for the transaction applied to this instance (if any).
        /// </summary>
        private TransactionHandle transaction;

        /// <summary>
        /// Cache element index if the parent of this Obj is an array (Arr).
        /// </summary>
        internal int cacheIndexInArr;

        /// <summary>
        /// Tells if any property value has changed on this container (if it is an object) or
        /// any of its children or grandchildren (recursivly). If this flag is true, there can be
        /// no changes to the JSON tree (but there can be changes to bound data objects).
        /// </summary>
        internal bool dirty = false;
        
        /// <summary>
        /// 
        /// </summary>
        internal bool pendingEnumeration;

        /// <summary>
        /// A list containing state for each property. Will only be initialized if changes are tracked.
        /// </summary>
        protected List<PropertyState> stateFlags;

        /// <summary>
        /// If this is a dynamic or a non-codegenerated jsonobject the list will contain the values 
        /// for each property.
        /// If this is an array, the list will contain the rows.
        /// If none of the above the list is not used and will never be initialized.
        /// </summary>
        private IList valueList;

        /// <summary>
        /// Json instances (objects or arrays) can be values in a hosting object property or in a 
        /// hosting array element. Whereas Javascript objects can refered to by a property without
        /// the object pointing back to the referrer, Nested objects/elements in JSON trees always
        /// have a single parent. Our implementation provides the service of finding the declaring
        /// (parent) object of this object.
        /// </summary>
        private Json parent;

        /// <summary>
        /// Json objects can be stored on the server between requests as session data.
        /// </summary>
        internal Session session;

        /// <summary>
        /// Keeps track on when we added/inserted or removed elements
        /// </summary>
        internal List<Change> arrayAddsAndDeletes = null;

        /// <summary>
        /// 
        /// </summary>
        internal List<ArrayVersionLog> versionLog = null;

        /// <summary>
        /// 
        /// </summary>
        private ChangeLog changeLog;

        /// <summary>
        /// Implementation field used to cache the Metadata property.
        /// </summary>
        private ObjMetadata<TObject, Json> metadata = null;

        /// <summary>
        /// The template this object is based on.
        /// </summary>
        private Template template;

        /// <summary>
        /// An Json object or array can be bound to a data object. This makes the Json reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models are often associated with database objects. I.e. a person form might
        /// reflect a person database object.
        /// </summary>
        internal object data;

        /// <summary>
        /// List containing all siblings that exists on this level.
        /// </summary>
        private SiblingList siblings;

        internal String appName;

        internal Boolean wrapInAppName;
        
        /// <summary>
        /// If set to true, additional features for keeping track of changes and getting a log of changes 
        /// are initialized. If not needed this should not be enabled since the performance will be much worse.
        /// </summary>
        private bool trackChanges;

        /// <summary>
        /// If set to false, bound properties will not be updated automatically.
        /// </summary>
        internal bool checkBoundProperties;
 
        private bool isArray;

        /// <summary>
        /// If this json is a part of a stateful viewmodel (i.e. puppet) this field contains
        /// the version the json was added to the viewmodel.
        /// </summary>
        private long addedInVersion;

        //TODO:
        // Needed when creating patches and serializing and namespaces are used. Sometimes the namespace (= appname)
        // should not be written.
        // See if there are better ways of solving this problem.
        internal bool calledFromStepSibling;

        private bool isAddedToViewmodel;

#if JSONINSTANCECOUNTER
        private static long globalInstanceCounter = 0;
        internal long instanceNo = -1;
        
        private void AssignInstanceNumber() {
            instanceNo = System.Threading.Interlocked.Increment(ref globalInstanceCounter);
        }
#endif 
    }   
}
