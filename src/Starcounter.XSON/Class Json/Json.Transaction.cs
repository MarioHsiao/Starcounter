
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
        }

        public void AttachCurrentTransaction() {
            if (_DB != null && _DB.CurrentTransaction != null)
                _transaction = _DB.CurrentTransaction;
        }

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
