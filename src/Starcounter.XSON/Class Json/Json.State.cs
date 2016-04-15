using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter {
    partial class Json {
        [Flags]
        protected enum PropertyState : byte {
            Default = 0,
            Dirty = 1,
            Cached = 2
        }

        /// <summary>
        /// Backing field for the transaction applied to this instance (if any).
        /// </summary>
//        private ITransaction _transaction;
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

        //private bool __BrandNew_ = true;

        /// <summary>
        /// 
        /// </summary>
        internal bool pendingEnumeration;

        /// <summary>
        /// For unbound values, we keep a list of flags to know which properties has changed.
        /// </summary>
        protected List<PropertyState> stateFlags;

        /// <summary>
        /// </summary>
        private IList _list;

        /// <summary>
        /// Json instances (objects or arrays) can be values in a hosting object property or in a 
        /// hosting array element. Whereas Javascript objects can refered to by a property without
        /// the object pointing back to the referrer, Nested objects/elements in JSON trees always
        /// have a single parent. Our implementation provides the service of finding the declaring
        /// (parent) object of this object.
        /// </summary>
        private Json _parent;

        /// <summary>
        /// Json objects can be stored on the server between requests as session data.
        /// </summary>
        internal Session session;

        /// <summary>
        /// Keeps track on when we added/inserted or removed elements
        /// </summary>
        internal List<Change> ArrayAddsAndDeletes = null;

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
        /// List containing all stepsiblings that exists on this level.
        /// </summary>
        private SiblingList _stepSiblings;

        internal String _appName;

        internal Boolean _wrapInAppName;
        
        /// <summary>
        /// If set to true, additional features for keeping track of changes and getting a log of changes 
        /// are initialized. If not needed this should not be enabled since the performance will be much worse.
        /// </summary>
        private bool _trackChanges;

        /// <summary>
        /// If set to false, bound properties will not be updated automatically.
        /// </summary>
        internal bool _checkBoundProperties;
 
        private bool _isArray;

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
    }   
}
