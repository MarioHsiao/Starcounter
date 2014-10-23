
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

                Json parentWithTrans = GetNearestObjParentWithTransaction();
                if (parentWithTrans != null)
                    return parentWithTrans.Transaction;

                return null;
            }
            set {
                if (_transaction != null) {
                    throw new Exception("An transaction is already set for this object. Changing transaction_ is not allowed.");
                }
                _transaction = value;
            }
        }

        /// <summary>
        /// Returns the transaction that is set on this app. Does NOT
        /// look in parents.
        /// </summary>
        internal ITransaction ThisTransaction {
            get { return _transaction; }
            set { _transaction = value; }
        }

        /// <summary>
        /// Returns the nearest parent that has a transaction.
        /// </summary>
        /// <returns>An Obj or null if this is the root Obj.</returns>
        Json GetNearestObjParentWithTransaction() {
            Json parent = Parent;
            while (parent != null) {
                Json objParent = parent as Json;

                if ((null != objParent) && (null != objParent.Transaction))
                    return objParent;

                parent = parent.Parent;
            }

            return (Json)parent;
        }
    }
}
