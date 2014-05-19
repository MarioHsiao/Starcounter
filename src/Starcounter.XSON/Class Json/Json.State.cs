﻿using Starcounter.Advanced;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter {
    partial class Json {

        /// <summary>
        /// Backing field for the transaction applied to this instance (if any).
        /// </summary>
        private ITransaction _transaction;

        /// <summary>
        /// Cache element index if the parent of this Obj is an array (Arr).
        /// </summary>
        internal int _cacheIndexInArr;

        /// <summary>
        /// Tells if any property value has changed on this container (if it is an object) or
        /// any of its children or grandchildren (recursivly). If this flag is true, there can be
        /// no changes to the JSON tree (but there can be changes to bound data objects).
        /// </summary>
        internal bool _Dirty = false;

        //private bool __BrandNew_ = true;

        /// <summary>
        /// 
        /// </summary>
        internal bool _PendingEnumeration;

        /// <summary>
        /// For unbound values, we keep a list of flags to know which properties has changed.
        /// </summary>
        protected List<bool> _SetFlag;

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
        internal Session _Session;

        /// <summary>
        /// Keeps track on when we added/inserted or removed elements
        /// </summary>
        internal List<Change> ArrayAddsAndDeletes = null;

        /// <summary>
        /// Implementation field used to cache the Metadata property.
        /// </summary>
        private ObjMetadata<TObject, Json> _Metadata = null;

        /// <summary>
        /// The _ template
        /// </summary>
        internal Template _Template;

        /// <summary>
        /// An Json object or array can be bound to a data object. This makes the Json reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models are often associated with database objects. I.e. a person form might
        /// reflect a person database object.
        /// </summary>
        internal object _data;

        internal List<Json> _stepSiblings;

        internal Json _stepParent;

        internal String _appName;

        /// <summary>
        /// If set to true, additional features for keeping track of changes and getting a log of changes 
        /// are initialized. If not needed this should not be enabled since the performance will be much worse.
        /// </summary>
        internal bool _dirtyCheckEnabled;

        private bool _isArray;

        /// <summary>
        /// Default value for setting the dirtycheck on or off for new instances of Json.
        /// </summary>
        public static bool DirtyCheckEnabled = true;
    }
}
