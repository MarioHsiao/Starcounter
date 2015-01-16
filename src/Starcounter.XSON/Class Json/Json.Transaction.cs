
using Starcounter.Advanced;
using System;
namespace Starcounter {
    partial class Json {
        /// <summary>
        /// Gets the nearest transaction.
        /// </summary>
        public ITransaction Transaction {
            get {
                // Returning first available transaction climbing up the tree starting from this node.
                if (_transaction != null)
                    return _transaction;

                if (_parent != null)
                    return _parent.Transaction;

                return null;
            }
            private set {
                _transaction = value;
            }
        }

        //public void AttachCurrentScope() {
        //    if (_DB != null && _DB.Current != null) {
        //        if (Transaction == null || Transaction != _DB.Current) {
        //            _transaction = _DB.Current;
        //        }
        //    }
        //}

        /// <summary>
        /// Returns the transaction that is set on this app. Does NOT
        /// look in parents.
        /// </summary>
        internal ITransaction ThisTransaction {
            get { return _transaction; }
            set { _transaction = value; }
        }
    }
}
